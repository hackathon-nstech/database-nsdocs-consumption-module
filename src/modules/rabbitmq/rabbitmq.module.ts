import { Module } from '@nestjs/common'
import { RabbitMQModule } from '@golevelup/nestjs-rabbitmq'
import { RabbitmqService } from './rabbitmq.service'
import { DatabaseModule } from 'src/infra/prisma/database.module'
import { ConfigModule, ConfigService } from '@nestjs/config'

@Module({
	imports: [
		RabbitMQModule.forRootAsync({
			imports: [ConfigModule],
			inject: [ConfigService],
			useFactory: (configService: ConfigService) => {
				const rabbitMqUrl =
					configService.get<string>('RABBITMQ_URL') || 'amqp://localhost'
				console.log(`Configuring RabbitMQ connection with URL: ${rabbitMqUrl}`)

				return {
					exchanges: [
						{
							name: 'my_exchange',
							type: 'direct',
						},
					],
					uri: rabbitMqUrl,
					connectionInitOptions: { wait: false },
				}
			},
		}),
		DatabaseModule,
	],
	providers: [RabbitmqService],
	exports: [RabbitmqService],
})
export class RabbitmqModule {}
