import { Module } from '@nestjs/common';
import { AppController } from './app.controller';
import { AppService } from './app.service';
import { DocumentsModule } from '../documents/documents.module';
import { ProceduresModule } from '../procedures/procedures.module';
import { ConfigModule } from '@nestjs/config';

@Module({
  imports: [
    ConfigModule.forRoot({
      isGlobal: true, // Makes ConfigService available globally
    }),
    DocumentsModule,
    ProceduresModule,
  ],
  controllers: [AppController],
  providers: [AppService],
})
export class AppModule {}
