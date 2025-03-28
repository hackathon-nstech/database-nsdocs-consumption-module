import { PrismaService } from '../../infra/prisma/prisma.service';
import { Prisma } from '@prisma/client';

export class UpdateConsumption {
  constructor(private readonly prisma: PrismaService) {}

  async execute(
    id_company: number,
    request_date: Date,
    documentChanges: {
      origin?: 'file' | 'email' | 'ws';
      document_type?: 'cfe' | 'cte' | 'cteos' | 'mdfe' | 'nfce' | 'nfe' | 'nfse';
      status?: 'ok' | 'pending' | 'error' | 'non-existing';
    },
  ): Promise<void> {
    const updateData: Prisma.ConsumptionUpdateManyMutationInput = {};

    // if (documentChanges.origin !== undefined) {
    //   updateData.origin = documentChanges.origin;
    // }
    // if (documentChanges.document_type !== undefined) {
    //   updateData.document_type = documentChanges.document_type;
    // }
    // if (documentChanges.status !== undefined) {
    //   updateData.status = documentChanges.status === 'non-existing' ? 'non_existing' : documentChanges.status;
    // }

    await this.prisma.consumption.updateMany({
      where: {
        id_company        
      },
      data: updateData,
    });
  }
}
