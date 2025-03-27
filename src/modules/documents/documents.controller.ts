import { Controller, Get, Post, Body, Param, Patch, Delete } from '@nestjs/common'
import { DocumentsService } from './documents.service'
import { Documents } from '@prisma/client'

@Controller('documents')
export class DocumentsController {
  constructor(private readonly documentsService: DocumentsService) {}

  @Get()
  async findAll(): Promise<Documents[]> {
    return this.documentsService.findAll()
  }

  @Get(':id')
  async findOne(@Param('id') id: string): Promise<Documents | null> {
    return this.documentsService.findOne(BigInt(id))
  }

  @Post()
  async create(@Body() data: Partial<Documents>): Promise<Documents> {
    const validData: Omit<Documents, 'id'> = {
      id_company: data.id_company ?? 0, // Provide a default value
      access_key: data.access_key ?? '',
      request_date: data.request_date ?? new Date(),
      updated_date: data.updated_date ?? new Date(),
      origin_id: data.origin_id ?? 0,
      document_type_id: data.document_type_id ?? 0,
      status_id: data.status_id ?? 0,
    }
    return this.documentsService.create(validData)
  }

  @Patch(':id')
  async update(@Param('id') id: string, @Body() data: Partial<Documents>): Promise<Documents> {
    return this.documentsService.update(BigInt(id), data)
  }

  @Delete(':id')
  async delete(@Param('id') id: string): Promise<Documents> {
    return this.documentsService.delete(BigInt(id))
  }
}
