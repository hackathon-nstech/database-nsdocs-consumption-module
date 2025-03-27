import { Injectable } from '@nestjs/common'
import { PrismaService } from '../../infra/prisma/prisma.service'
import { Documents } from '@prisma/client'

@Injectable()
export class DocumentsService {
  constructor(private readonly prisma: PrismaService) {}

  async findAll(): Promise<Documents[]> {
    return this.prisma.documents.findMany()
  }

  async findOne(id: bigint): Promise<Documents | null> {
    return this.prisma.documents.findUnique({ where: { id } })
  }

  async create(data: Omit<Documents, 'id'>): Promise<Documents> {
    return this.prisma.documents.create({ data })
  }

  async update(id: bigint, data: Partial<Documents>): Promise<Documents> {
    return this.prisma.documents.update({ where: { id }, data })
  }

  async delete(id: bigint): Promise<Documents> {
    return this.prisma.documents.delete({ where: { id } })
  }
}
