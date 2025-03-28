import { BadRequestException, Body, Injectable, Logger, Param } from '@nestjs/common';
import { PrismaService } from './prisma.service';
import { Documents } from '@prisma/client'; // Ensure Prisma types are imported
import { UpdateCompanyConsumptionUseCase } from 'src/modules/documents/usecases/update-consumption.usecase';

@Injectable()
export class DatabaseDocumentsService {
  private readonly logger = new Logger(DatabaseDocumentsService.name)
  
  constructor(private readonly prisma: PrismaService) {}

  async isValidRefCode(id: number, entity: string): Promise<boolean> {
    const count = await this.prisma.nsRefCodes.count({
      where: {
        id,
        entity,
      },
    });
    return count > 0;
  }

  async findByAccessKeyAndCompany(accessKey: string, idCompany: number): Promise<Documents | null> {
    return this.prisma.documents.findFirst({
      where: {
        access_key: accessKey,
        id_company: idCompany,
      },
    });
  }

  async create(@Body() data: Partial<Documents>): Promise<Documents> {
      this.logger.log('Received request to create a document')
      this.logger.debug(`Request body: ${JSON.stringify(data)}`)
  
      // Validate IDs
      if (data.origin_id && !(await this.isValidRefCode(data.origin_id, 'origin'))) {
        throw new BadRequestException('Invalid origin_id')
      }
      if (data.document_type_id && !(await this.isValidRefCode(data.document_type_id, 'document_type'))) {
        throw new BadRequestException('Invalid document_type_id')
      }
      if (data.status_id && !(await this.isValidRefCode(data.status_id, 'status'))) {
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
      const existingDocument = await this.findByAccessKeyAndCompany(validData.access_key, validData.id_company);
      if (existingDocument) {
        throw new BadRequestException('A document with the same access_key and id_company already exists');
      }
  
      const createdDocument = await this.prisma.documents.create({ data: validData });
      const updateCompanyConsumption = new UpdateCompanyConsumptionUseCase(this.prisma);
      await updateCompanyConsumption.execute(createdDocument, 1);
      
      this.logger.log(`Document created with ID: ${createdDocument.id}`)
  
      return createdDocument
  }

  async update(@Param('id') id: string, @Body() data: Partial<Documents>): Promise<Documents> {
    this.logger.log(`Updating document with ID: ${id}`)
    this.logger.debug(`Update data: ${JSON.stringify(data)}`)

    const existingDocument = await this.prisma.documents.findUnique({ where: { id: BigInt(id) } });
    if (!existingDocument) {
      throw new Error('Document not found');
    }

    // Validate IDs
    if (data.origin_id && !(await this.isValidRefCode(data.origin_id, 'origin'))) {
      throw new BadRequestException('Invalid origin_id')
    }
    if (data.document_type_id && !(await this.isValidRefCode(data.document_type_id, 'document_type'))) {
      throw new BadRequestException('Invalid document_type_id')
    }
    if (data.status_id && !(await this.isValidRefCode(data.status_id, 'status'))) {
      throw new BadRequestException('Invalid status_id')
    }

    // Validate unique key for update
    if (data.access_key && data.id_company) {
      const existingDocument = await this.findByAccessKeyAndCompany(data.access_key, data.id_company);
      if (existingDocument && existingDocument.id !== BigInt(id)) {
        throw new BadRequestException('A document with the same access_key and id_company already exists');
      }
    }

    if (data?.id) {
      delete data.id // Remove id from the update data
    }
    const updatedDocument = await this.prisma.documents.update({
      where: { id: BigInt(id) },
      data,
    });
    this.logger.log(`Document with ID: ${id} updated successfully`)

    const updateCompanyConsumption = new UpdateCompanyConsumptionUseCase(this.prisma);
      
    // Verifica se algum dos campos relevantes foi alterado
    if (
      data?.origin_id !== existingDocument?.origin_id ||
      data?.id_company !== existingDocument?.id_company ||
      data?.status_id !== existingDocument?.status_id
    ) {
      // Atualiza o consumo para os valores antigos
      await updateCompanyConsumption.execute(existingDocument, -1);

      // Atualiza o consumo para os valores novos
      await updateCompanyConsumption.execute(updatedDocument, 1);
    }

    return updatedDocument;
  }
}
