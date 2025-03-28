import { Injectable } from '@nestjs/common';
import { PrismaService } from './prisma.service';
import { UpdateCompanyConsumption } from '../../modules/documents/update-company-consumption.class';
import { UpdateConsumption } from '../../modules/documents/update-consumption.class';
import { Prisma } from '@prisma/client'; // Ensure Prisma types are imported

@Injectable()
export class DatabaseProceduresService {
  constructor(private readonly prisma: PrismaService) {}

  async triggerUpdateCompanyConsumption(): Promise<void> {
    const updater = new UpdateCompanyConsumption();
    await updater.execute();
  }

  async triggerAfterInsert(
    id_company: number,
    request_date: Date,
    origin: 'file' | 'email' | 'ws',
    document_type: 'cfe' | 'cte' | 'cteos' | 'mdfe' | 'nfce' | 'nfe' | 'nfse',
    status: 'ok' | 'pending' | 'error' | 'non-existing',
  ): Promise<void> {
    const updater = new UpdateCompanyConsumption();
    await updater.execute();
  }

  async triggerAfterUpdate(
    newData: {
      id_company: number;
      request_date: Date;
      origin: 'file' | 'email' | 'ws';
      document_type: 'cfe' | 'cte' | 'cteos' | 'mdfe' | 'nfce' | 'nfe' | 'nfse';
      status: 'ok' | 'pending' | 'error' | 'non-existing';
    },
    oldData: {
      id_company: number;
      request_date: Date;
      origin: 'file' | 'email' | 'ws';
      document_type: 'cfe' | 'cte' | 'cteos' | 'mdfe' | 'nfce' | 'nfe' | 'nfse';
      status: 'ok' | 'pending' | 'error' | 'non-existing';
    },
  ): Promise<void> {
    const updater = new UpdateCompanyConsumption();
    await updater.execute();
  }

  async triggerAfterDelete(
    id_company: number,
    request_date: Date,
    origin: 'file' | 'email' | 'ws',
    document_type: 'cfe' | 'cte' | 'cteos' | 'mdfe' | 'nfce' | 'nfe' | 'nfse',
    status: 'ok' | 'pending' | 'error' | 'non-existing',
  ): Promise<void> {
    const updater = new UpdateCompanyConsumption();
    await updater.execute();
  }

  async updateCompanyConsumption(
    id_company: number,
    request_date: Date,
    documentChanges: {
      origin?: 'file' | 'email' | 'ws';
      document_type?: 'cfe' | 'cte' | 'cteos' | 'mdfe' | 'nfce' | 'nfe' | 'nfse';
      status?: 'ok' | 'pending' | 'error' | 'non-existing';
    },
  ): Promise<void> {
    const updater = new UpdateConsumption(this.prisma);
    await updater.execute(id_company, request_date, documentChanges);
  }
}
