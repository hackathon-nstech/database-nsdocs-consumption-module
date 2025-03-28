import { Controller, Get, Post, Body, Param, Patch, Delete, Logger } from '@nestjs/common'
import { DocumentsService } from './documents.service'
import { Documents } from '@prisma/client'

@Controller('documents')
export class DocumentsController {
  private readonly logger = new Logger(DocumentsController.name)

  constructor(private readonly documentsService: DocumentsService) {}

  async updateConsumption(): Promise<void> {
    this.logger.log('Updating company consumption...')
    await this.documentsService.updateCompanyConsumption()
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

    const createdDocument = await this.documentsService.create(validData)
    this.logger.log(`Document created with ID: ${createdDocument.id}`)

    return createdDocument
  }

  @Patch(':id')
  async update(@Param('id') id: string, @Body() data: Partial<Documents>): Promise<Documents> {
    this.logger.log(`Updating document with ID: ${id}`)
    this.logger.debug(`Update data: ${JSON.stringify(data)}`)
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
