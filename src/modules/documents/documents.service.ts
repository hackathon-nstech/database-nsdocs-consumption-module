import { Injectable, Logger } from '@nestjs/common';
import { PrismaService } from '../../infra/prisma/prisma.service';
import { Documents } from '@prisma/client';
import { UpdateCompanyConsumptionUseCase } from './usecases/update-consumption.usecase';

@Injectable()
export class DocumentsService {
  private readonly logger = new Logger(DocumentsService.name);

  constructor(
    private readonly prisma: PrismaService
  ) {}

  async findAll(): Promise<Documents[]> {
    return this.prisma.documents.findMany({
      include: {
        document_type: true,
        status: true,
        origin: true,
      },
    });
  }

  async findOne(id: bigint): Promise<Documents | null> {
    return this.prisma.documents.findUnique({
      where: { id },
      include: {
        document_type: true,
        status: true,
        origin: true,
      },
    });
  }

  async create(data: Omit<Documents, 'id'>): Promise<Documents> {
    return this.prisma.$transaction(async (prisma) => {
      const document = await prisma.documents.create({ data });

      const updateCompanyConsumption = new UpdateCompanyConsumptionUseCase(prisma);
      await updateCompanyConsumption.execute(document, 1);

      return document;
    });
  }

  async update(id: bigint, data: Partial<Documents>): Promise<Documents> {
    return this.prisma.$transaction(async (prisma) => {
      const existingDocument = await this.prisma.documents.findUnique({ where: { id } });
      if (!existingDocument) {
        throw new Error('Document not found');
      }

      const updatedDocument = await this.prisma.documents.update({ where: { id }, data });
      const updateCompanyConsumption = new UpdateCompanyConsumptionUseCase(prisma);
      
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
    });
  }

  async delete(id: bigint): Promise<Documents> {
    return this.prisma.$transaction(async (prisma) => {
      const document = await this.prisma.documents.findUnique({ where: { id } });
      if (document) {
        await this.prisma.documents.delete({ where: { id } });
        const updateCompanyConsumption = new UpdateCompanyConsumptionUseCase(prisma);
        await updateCompanyConsumption.execute(document, -1); // Decrementa a contagem
      }
      return document!;
    });
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

  async findByAccessKeyAndCompany(accessKey: string, idCompany: number): Promise<Documents | null> {
    return this.prisma.documents.findFirst({
      where: {
        access_key: accessKey,
        id_company: idCompany,
      },
    });
  }
}
