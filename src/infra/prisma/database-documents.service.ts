import { BadRequestException, Body, Injectable, Logger } from '@nestjs/common';
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
}
