import { Injectable, Logger } from '@nestjs/common';
import { AmqpConnection, RabbitSubscribe } from '@golevelup/nestjs-rabbitmq';
import { DatabaseDocumentsService } from 'src/infra/prisma/database-documents.service';

@Injectable()
export class RabbitmqService {
  private readonly logger = new Logger(RabbitmqService.name);

  constructor(
    private readonly amqpConnection: AmqpConnection,
    private readonly databaseDocumentService: DatabaseDocumentsService) {}

  // Publisher: Send a message to a queue
  async publishMessage(queue: string, message: any) {
    try {
      await this.amqpConnection.publish('my_exchange', queue, message);
      this.logger.log(`Message sent to queue "${queue}": ${JSON.stringify(message)}`);
    } catch (error) {
      this.logger.error('Error publishing message:', error);
    }
  }

  // Subscriber: Listen for messages from a queue
  @RabbitSubscribe({
    exchange: 'my_exchange',
    routingKey: 'document_posted',
    queue: 'documents_queue',
  })
  handleMessage(message: any) {
    this.logger.log(`Received message: ${JSON.stringify(message)}`);
    // Process the message here
    
    this.databaseDocumentService.create(message).then(result => {
      this.logger.log(`Document created successfully: ${JSON.stringify(result)}`);
    }).catch(error => {
      this.logger.error('Error creating document:', error);
    });
  }
}