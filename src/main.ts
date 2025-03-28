import { NestFactory } from '@nestjs/core'
import { MicroserviceOptions, Transport } from '@nestjs/microservices'
import { ConfigService } from '@nestjs/config'
import { AppModule } from './modules/app/app.module'

declare global {
	interface BigInt {
		toJSON(): string
	}
}

BigInt.prototype.toJSON = function () {
	return this.toString()
}

async function bootstrap() {
	const app = await NestFactory.create(AppModule)

	// Get the ConfigService
	const configService = app.get(ConfigService)

	// Maximum number of reconnection attempts
	const maxReconnectAttempts =
		configService.get<number>('MAX_RECONNECT_ATTEMPTS') || 5
	const reconnectDelay = configService.get<number>('RECONNECT_DELAY') || 5000 // 5 seconds delay between attempts

	let reconnectAttempts = 0
	let microserviceConnected = false

	// Function to connect to RabbitMQ with retry logic
	const connectToRabbitMQ = async () => {
		try {
			const rabbitmqUrl = configService.get<string>('RABBITMQ_URL')
			console.log(`Attempting to connect to RabbitMQ at: ${rabbitmqUrl}`)
			
			// Connect to RabbitMQ as a microservice
			app.connectMicroservice<MicroserviceOptions>({
				transport: Transport.RMQ,
				options: {
					urls: [rabbitmqUrl || 'amqp://localhost'],
					queue:
						configService.get<string>('DOCUMENTS_QUEUE') || 'documents_queue',
					queueOptions: {
						durable: true,
					},
					noAck: false, // Enable manual acknowledgment
					//reconnectAttempts: 5, // Built-in reconnect attempts for initial connection
					//reconnectDelay: 1000, // Reconnect delay in ms
				},
			})

			// Start all microservices
			await app.startAllMicroservices()

			microserviceConnected = true
			console.log('RabbitMQ subscriber is active and listening for messages')
		} catch (error) {
			console.error(
				`Failed to connect to RabbitMQ: ${error && error['message']}`,
			)

			reconnectAttempts++
			if (reconnectAttempts < maxReconnectAttempts) {
				console.log(
					`Connection to transport failed. Trying to reconnect... Attempt ${reconnectAttempts}/${maxReconnectAttempts}`,
				)
				setTimeout(connectToRabbitMQ, reconnectDelay)
			} else {
				console.error(
					`Failed to connect to RabbitMQ after ${maxReconnectAttempts} attempts. Giving up.`,
				)
				// Optionally, you might want to exit the process or continue without RabbitMQ
				// process.exit(1);
			}
		}
	}

	// Initial connection attempt
	await connectToRabbitMQ()

	await app.listen(process.env.PORT ?? 3000)
}
bootstrap()
