import { Module } from '@nestjs/common';
import { ProceduresController } from './procedures.controller';
import { DatabaseProceduresService } from '../../infra/prisma/database-procedures.service';
import { PrismaService } from '../../infra/prisma/prisma.service';
import { DatabaseDocumentsService } from 'src/infra/prisma/database-documents.service';

@Module({
  controllers: [ProceduresController],
  providers: [DatabaseProceduresService, DatabaseDocumentsService, PrismaService],
  exports: [DatabaseProceduresService, DatabaseDocumentsService], // Exportando o serviço para outros módulos
})
export class ProceduresModule {}
