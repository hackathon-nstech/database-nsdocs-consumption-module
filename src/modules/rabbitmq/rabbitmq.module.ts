import { Module } from '@nestjs/common';
import { RabbitMQModule } from '@golevelup/nestjs-rabbitmq';
import { RabbitmqService } from './rabbitmq.service';
import { DatabaseModule } from 'src/infra/prisma/database.module';

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
    DatabaseModule
  ],
  providers: [RabbitmqService],
  exports: [RabbitmqService],
})
export class RabbitmqModule {}