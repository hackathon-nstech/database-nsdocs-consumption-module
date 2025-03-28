import { Module } from '@nestjs/common';
import { DocumentsService } from './documents.service';
import { DocumentsController } from './documents.controller';
import { PrismaService } from '../../infra/prisma/prisma.service';
import { ProceduresModule } from '../procedures/procedures.module';
import { RabbitmqModule } from '../rabbitmq/rabbitmq.module';

@Module({
  imports: [ProceduresModule, RabbitmqModule], // Importando o módulo que fornece o DatabaseProceduresService
  controllers: [DocumentsController],
  providers: [DocumentsService, PrismaService],
})
export class DocumentsModule {}
