import { Module } from '@nestjs/common';
import { DocumentsService } from './documents.service';
import { DocumentsController } from './documents.controller';
import { PrismaService } from '../../infra/prisma/prisma.service';
import { ProceduresModule } from '../procedures/procedures.module';

@Module({
  imports: [ProceduresModule], // Importando o módulo que fornece o DatabaseProceduresService
  controllers: [DocumentsController],
  providers: [DocumentsService, PrismaService],
})
export class DocumentsModule {}
