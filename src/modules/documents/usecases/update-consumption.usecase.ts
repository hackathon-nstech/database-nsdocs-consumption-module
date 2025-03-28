import { Injectable } from '@nestjs/common';
import { Documents, Prisma } from '@prisma/client';

@Injectable()
export class UpdateCompanyConsumptionUseCase {
  constructor(private readonly prisma: Prisma.TransactionClient) {}

  async execute(
    data: Documents,
    quantity: number,
  ): Promise<void> {
    const { id_company, request_date, origin_id, document_type_id, status_id } = data;

    const startOfDayDate = new Date(
      `${request_date.toISOString().split('T')[0]}T00:00:00.000Z`
    );
    const endOfDayDate = new Date(
      `${request_date.toISOString().split('T')[0]}T23:59:59.999Z`
    );

    // Check if a consumption record already exists
    const existingConsumption = await this.prisma.consumption.findFirst({
      where: {
        id_company,
        origin_id,
        document_type_id,
        status_id,
        AND: [
          {
            consumption_date: {
              gte: startOfDayDate,
            },
          },
          {
            consumption_date: {
              lte: endOfDayDate,
            },
          },
        ],
      },
    });


    if (!existingConsumption) {
      // Insert a new consumption record if it doesn't exist
      await this.prisma.consumption.create({
        data: {
          id_company,
          consumption_date: startOfDayDate,
          origin_id,
          document_type_id,
          status_id,
          quantity: Math.max(quantity, 0),
          total: quantity > 0 ? 1 : 0,
        },
      });
    } else {
      // Update the existing consumption record
      await this.prisma.consumption.update({
        where: { id: existingConsumption.id },
        data: {
          quantity: Math.max(existingConsumption.quantity + quantity, 0),
          total: existingConsumption.total + (quantity > 0 ? 1 : 0),
        },
      });
    }
  }
}