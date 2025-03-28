import { Module } from '@nestjs/common';
import { ProceduresController } from './procedures.controller';
import { DatabaseProceduresService } from '../../infra/prisma/database-procedures.service';
import { PrismaService } from '../../infra/prisma/prisma.service';

@Module({
  controllers: [ProceduresController],
  providers: [DatabaseProceduresService, PrismaService],
  exports: [DatabaseProceduresService], // Exportando o serviço para outros módulos
})
export class ProceduresModule {}
