import { Injectable, Logger } from '@nestjs/common';
import { PrismaService } from '../../infra/prisma/prisma.service';
import { DatabaseProceduresService } from '../../infra/prisma/database-procedures.service';
import { Documents } from '@prisma/client';
import { UpdateCompanyConsumption } from './update-company-consumption.class'; // Corrigido para o caminho correto

@Injectable()
export class DocumentsService {
  private readonly logger = new Logger(DocumentsService.name);

  constructor(
    private readonly prisma: PrismaService,
    private readonly proceduresService: DatabaseProceduresService,
  ) {}

  async findAll(): Promise<Documents[]> {
    return this.prisma.documents.findMany();
  }

  async findOne(id: bigint): Promise<Documents | null> {
    return this.prisma.documents.findUnique({ where: { id } });
  }

  async create(data: Omit<Documents, 'id'>): Promise<Documents> {
    const document = await this.prisma.documents.create({ data });
    // await this.proceduresService.triggerAfterInsert(
    //   document.id_company,
    //   document.request_date,
    //   document.origin,
    //   document.document_type,
    //   document.status === 'non_existing' ? 'non-existing' : document.status,
    // );
    return document;
  }

  async update(id: bigint, data: Partial<Documents>): Promise<Documents> {
    const oldDocument = await this.prisma.documents.findUnique({ where: { id } });
    const updatedDocument = await this.prisma.documents.update({ where: { id }, data });

    // if (oldDocument) {
    //   await this.proceduresService.triggerAfterUpdate(
    //     {
    //       id_company: updatedDocument.id_company,
    //       request_date: updatedDocument.request_date,
    //       origin: updatedDocument.origin,
    //       document_type: updatedDocument.document_type,
    //       status: updatedDocument.status === 'non_existing' ? 'non-existing' : updatedDocument.status,
    //     },
    //     {
    //       id_company: oldDocument.id_company,
    //       request_date: oldDocument.request_date,
    //       origin: oldDocument.origin,
    //       document_type: oldDocument.document_type,
    //       status: oldDocument.status === 'non_existing' ? 'non-existing' : oldDocument.status,
    //     },
    //   );
    // }
    return updatedDocument;
  }

  async delete(id: bigint): Promise<Documents> {
    const document = await this.prisma.documents.findUnique({ where: { id } });
    if (document) {
      await this.prisma.documents.delete({ where: { id } });
      // await this.proceduresService.triggerAfterDelete(
      //   document.id_company,
      //   document.request_date,
      //   document.origin,
      //   document.document_type,
      //   document.status === 'non_existing' ? 'non-existing' : document.status,
      // );
    }
    return document!;
  }

  async updateCompanyConsumption(): Promise<void> {
    const updater = new UpdateCompanyConsumption();
    await updater.execute(); // Substitui a chamada ao procedimento armazenado
  }

  async isValidRefCode(id: number, entity: string): Promise<boolean> {
    const count = await this.prisma.nsRefCodes.count({
      where: {
        id,
        entity,
      },
    });
    return count > 0;
  }
}
