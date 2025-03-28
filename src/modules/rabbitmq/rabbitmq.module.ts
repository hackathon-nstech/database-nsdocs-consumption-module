import { Module } from '@nestjs/common';
import { RabbitMQModule } from '@golevelup/nestjs-rabbitmq';
import { RabbitmqService } from './rabbitmq.service';
import { DatabaseDocumentsService } from 'src/infra/prisma/database-documents.service';
import { ProceduresModule } from '../procedures/procedures.module';

@Module({
  imports: [
    RabbitMQModule.forRoot({
      exchanges: [
        {
          name: 'my_exchange',
          type: 'direct',
        },
      ],
      uri: 'amqp://localhost', // RabbitMQ connection URI
      connectionInitOptions: { wait: false },
    }),
    ProceduresModule
  ],
  providers: [RabbitmqService],
  exports: [RabbitmqService],
})
export class RabbitmqModule {}