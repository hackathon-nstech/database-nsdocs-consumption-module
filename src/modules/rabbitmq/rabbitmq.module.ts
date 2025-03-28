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
<<<<<<< HEAD
    DatabaseModule
=======
>>>>>>> e10607d5b7dd011bd43d560cf3420e076747c42a
  ],
  providers: [RabbitmqService],
  exports: [RabbitmqService],
})
export class RabbitmqModule {}