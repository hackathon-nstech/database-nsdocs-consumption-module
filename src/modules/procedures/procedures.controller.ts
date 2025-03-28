import { Controller, Post, Body } from '@nestjs/common';
import { DatabaseProceduresService } from '../../infra/prisma/database-procedures.service';

@Controller('procedures')
export class ProceduresController {
  constructor(private readonly proceduresService: DatabaseProceduresService) {}

  @Post('update-company-consumption')
  async updateCompanyConsumption(@Body() body: any): Promise<void> {
    const { id_company, quantity, consumption_date, origin, document_type, status } = body;
    await this.proceduresService.updateCompanyConsumption(
      id_company,
      new Date(consumption_date),
      { origin, document_type, status },
    );
  }

  @Post('trigger-after-insert')
  async triggerAfterInsert(@Body() body: any): Promise<void> {
    const { id_company, request_date, origin, document_type, status } = body;
    await this.proceduresService.triggerAfterInsert(
      id_company,
      new Date(request_date),
      origin,
      document_type,
      status,
    );
  }

  @Post('trigger-after-update')
  async triggerAfterUpdate(@Body() body: any): Promise<void> {
    const { newData, oldData } = body;
    await this.proceduresService.triggerAfterUpdate(newData, oldData);
  }

  @Post('trigger-after-delete')
  async triggerAfterDelete(@Body() body: any): Promise<void> {
    const { id_company, request_date, origin, document_type, status } = body;
    await this.proceduresService.triggerAfterDelete(
      id_company,
      new Date(request_date),
      origin,
      document_type,
      status,
    );
  }
}
