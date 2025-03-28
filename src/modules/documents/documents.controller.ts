import { Controller, Get, Post, Body, Param, Patch, Delete, Logger, BadRequestException } from '@nestjs/common'
import { DocumentsService } from './documents.service'
import { Documents } from '@prisma/client'
import { RabbitmqService } from '../rabbitmq/rabbitmq.service'

@Controller('documents')
export class DocumentsController {
  private readonly logger = new Logger(DocumentsController.name)

  constructor(
    private readonly documentsService: DocumentsService,
    private readonly rabbitmqService: RabbitmqService,
  ) {}

  async updateConsumption(): Promise<void> {
    this.logger.log('Updating company consumption...')
    this.logger.log('Company consumption updated successfully.')
  }

  @Get()
  async findAll(): Promise<Documents[]> {
    this.logger.log('Fetching all documents')
    const documents = await this.documentsService.findAll()
    this.logger.debug(`Fetched ${documents.length} documents`)
    return documents
  }

  @Get(':id')
  async findOne(@Param('id') id: string): Promise<Documents | null> {
    this.logger.log(`Fetching document with ID: ${id}`)
    const document = await this.documentsService.findOne(BigInt(id))
    if (document) {
      this.logger.debug(`Document found: ${JSON.stringify(document)}`)
    } else {
      this.logger.warn(`Document with ID: ${id} not found`)
    }
    return document
  }

  @Post()
  async create(@Body() data: Partial<Documents>): Promise<Documents> {
    this.logger.log('Received request to create a document')
    this.logger.debug(`Request body: ${JSON.stringify(data)}`)

    // Validate IDs
    if (data.origin_id && !(await this.documentsService.isValidRefCode(data.origin_id, 'origin'))) {
      throw new BadRequestException('Invalid origin_id')
    }
    if (data.document_type_id && !(await this.documentsService.isValidRefCode(data.document_type_id, 'document_type'))) {
      throw new BadRequestException('Invalid document_type_id')
    }
    if (data.status_id && !(await this.documentsService.isValidRefCode(data.status_id, 'status'))) {
      throw new BadRequestException('Invalid status_id')
    }

    const validData: Omit<Documents, 'id'> = {
      id_company: data.id_company ?? 0, // Provide a default value
      access_key: data.access_key ?? '',
      request_date: data.request_date ?? new Date(),
      updated_date: data.updated_date ?? new Date(),
      origin_id: data.origin_id ?? 0,
      document_type_id: data.document_type_id ?? 0,
      status_id: data.status_id ?? 0,
    }

    this.logger.debug(`Validated data: ${JSON.stringify(validData)}`)

    // Validate unique key for create
    const existingDocument = await this.documentsService.findByAccessKeyAndCompany(validData.access_key, validData.id_company);
    if (existingDocument) {
      throw new BadRequestException('A document with the same access_key and id_company already exists');
    }

    const createdDocument = await this.documentsService.create(validData)
    this.logger.log(`Document created with ID: ${createdDocument.id}`)

    return createdDocument
  }

  @Post('async')
  async createAsync(@Body() data: Partial<Documents>): Promise<{ success: boolean, message: string }> {
    this.logger.log('Received request to create a document')
    this.logger.debug(`Request body: ${JSON.stringify(data)}`)

    try {
      // You can add validation or transformation of the data here if needed
      await this.rabbitmqService.publishMessage("document_posted", data);
      return { 
        success: true,
        message: 'Document event sent to message queue successfully'
      };
    } catch (error) {
      return {
        success: false,
        message: `Failed to post document event: ${error && error["message"]}`
      };
    }
  }

  @Patch(':id')
  async update(@Param('id') id: string, @Body() data: Partial<Documents>): Promise<Documents> {
    this.logger.log(`Updating document with ID: ${id}`)
    this.logger.debug(`Update data: ${JSON.stringify(data)}`)

    // Validate IDs
    if (data.origin_id && !(await this.documentsService.isValidRefCode(data.origin_id, 'origin'))) {
      throw new BadRequestException('Invalid origin_id')
    }
    if (data.document_type_id && !(await this.documentsService.isValidRefCode(data.document_type_id, 'document_type'))) {
      throw new BadRequestException('Invalid document_type_id')
    }
    if (data.status_id && !(await this.documentsService.isValidRefCode(data.status_id, 'status'))) {
      throw new BadRequestException('Invalid status_id')
    }

    // Validate unique key for update
    if (data.access_key && data.id_company) {
      const existingDocument = await this.documentsService.findByAccessKeyAndCompany(data.access_key, data.id_company);
      if (existingDocument && existingDocument.id !== BigInt(id)) {
        throw new BadRequestException('A document with the same access_key and id_company already exists');
      }
    }

    if (data?.id) {
      delete data.id // Remove id from the update data
    }
    const updatedDocument = await this.documentsService.update(BigInt(id), data)
    this.logger.log(`Document with ID: ${id} updated successfully`)
    return updatedDocument
  }

  @Delete(':id')
  async delete(@Param('id') id: string): Promise<Documents> {
    this.logger.log(`Deleting document with ID: ${id}`)
    const deletedDocument = await this.documentsService.delete(BigInt(id))
    this.logger.log(`Document with ID: ${id} deleted successfully`)
    return deletedDocument
  }
}
