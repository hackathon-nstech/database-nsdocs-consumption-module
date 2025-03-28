import { Module } from '@nestjs/common';
import { DatabaseDocumentsService } from './database-documents.service';
import { PrismaService } from './prisma.service';

@Module({
  imports: [], // Importando o módulo que fornece o DatabaseProceduresService
  controllers: [],
  providers: [DatabaseDocumentsService, PrismaService],
  exports: [DatabaseDocumentsService]
})
export class DatabaseModule {}
