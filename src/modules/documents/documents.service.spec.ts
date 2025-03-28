import { Test, TestingModule } from '@nestjs/testing';
import { DocumentsService } from './documents.service';
import { PrismaService } from '../../infra/prisma/prisma.service';
import { DatabaseProceduresService } from '../../infra/prisma/database-procedures.service';
import { Documents } from '@prisma/client';

// Mock data
const mockDocuments: Documents[] = [
  {
    id: BigInt(1),
    id_company: 1,
    access_key: 'key1',
    request_date: new Date(),
    updated_date: new Date(),
    origin_id: 1,
    document_type_id: 1,
    status_id: 1
  },
  {
    id: BigInt(2),
    id_company: 2,
    access_key: 'key2',
    request_date: new Date(),
    updated_date: new Date(),
    origin_id: 2,
    document_type_id: 2,
    status_id: 2
  },
];

// Mock PrismaService
const mockPrismaService = {
  documents: {
    findMany: jest.fn(),
    findUnique: jest.fn(),
    findFirst: jest.fn(),
    create: jest.fn(),
    update: jest.fn(),
    delete: jest.fn(),
  },
  nsRefCodes: {
    count: jest.fn(),
  },
};

// Mock DatabaseProceduresService
const mockDatabaseProceduresService = {
  triggerAfterInsert: jest.fn(),
  triggerAfterUpdate: jest.fn(),
  triggerAfterDelete: jest.fn(),
};

describe('DocumentsService', () => {
  let service: DocumentsService;

  beforeEach(async () => {
    const module: TestingModule = await Test.createTestingModule({
      providers: [
        DocumentsService,
        {
          provide: PrismaService,
          useValue: mockPrismaService,
        },
        {
          provide: DatabaseProceduresService,
          useValue: mockDatabaseProceduresService,
        },
      ],
    }).compile();

    service = module.get<DocumentsService>(DocumentsService);
    
    // Reset all mocks before each test
    jest.clearAllMocks();
  });

  it('should be defined', () => {
    expect(service).toBeDefined();
  });

  describe('findAll', () => {
    it('should return an array of documents', async () => {
      mockPrismaService.documents.findMany.mockResolvedValue(mockDocuments);
      
      const result = await service.findAll();
      
      expect(result).toEqual(mockDocuments);
      expect(mockPrismaService.documents.findMany).toHaveBeenCalledWith({
        include: {
          document_type: true,
          status: true,
          origin: true,
        },
      });
    });
  });

  describe('findOne', () => {
    it('should return a single document by id', async () => {
      mockPrismaService.documents.findUnique.mockResolvedValue(mockDocuments[0]);
      
      const result = await service.findOne(BigInt(1));
      
      expect(result).toEqual(mockDocuments[0]);
      expect(mockPrismaService.documents.findUnique).toHaveBeenCalledWith({
        where: { id: BigInt(1) },
        include: {
          document_type: true,
          status: true,
          origin: true,
        },
      });
    });

    it('should return null if document not found', async () => {
      mockPrismaService.documents.findUnique.mockResolvedValue(null);
      
      const result = await service.findOne(BigInt(999));
      
      expect(result).toBeNull();
    });
  });

  describe('create', () => {
    it('should create and return a document', async () => {
      const createDocumentData = {
        id_company: 3,
        access_key: 'key3',
        request_date: new Date(),
        updated_date: new Date(),
        origin_id: 1,
        document_type_id: 1,
        status_id: 1
      };
      
      const createdDocument = {
        id: BigInt(3),
        ...createDocumentData,
      };
      
      mockPrismaService.documents.create.mockResolvedValue(createdDocument);
      
      const result = await service.create(createDocumentData);
      
      expect(result).toEqual(createdDocument);
      expect(mockPrismaService.documents.create).toHaveBeenCalledWith({
        data: createDocumentData,
      });
    });
  });

  describe('update', () => {
    it('should update and return a document', async () => {
      const documentId = BigInt(1);
      const updateData = {
        access_key: 'updated_key',
      };
      
      const oldDocument = mockDocuments[0];
      const updatedDocument = { ...mockDocuments[0], ...updateData };
      
      mockPrismaService.documents.findUnique.mockResolvedValue(oldDocument);
      mockPrismaService.documents.update.mockResolvedValue(updatedDocument);
      
      const result = await service.update(documentId, updateData);
      
      expect(result).toEqual(updatedDocument);
      expect(mockPrismaService.documents.findUnique).toHaveBeenCalledWith({
        where: { id: documentId },
      });
      expect(mockPrismaService.documents.update).toHaveBeenCalledWith({
        where: { id: documentId },
        data: updateData,
      });
    });
  });

  describe('delete', () => {
    it('should delete and return a document', async () => {
      const documentId = BigInt(1);
      const document = mockDocuments[0];
      
      mockPrismaService.documents.findUnique.mockResolvedValue(document);
      mockPrismaService.documents.delete.mockResolvedValue(document);
      
      const result = await service.delete(documentId);
      
      expect(result).toEqual(document);
      expect(mockPrismaService.documents.findUnique).toHaveBeenCalledWith({
        where: { id: documentId },
      });
      expect(mockPrismaService.documents.delete).toHaveBeenCalledWith({
        where: { id: documentId },
      });
    });

    it('should return undefined if document not found', async () => {
      const documentId = BigInt(999);
      
      mockPrismaService.documents.findUnique.mockResolvedValue(null);
      
      const result = await service.delete(documentId);
      
      expect(result).toBeNull();
      expect(mockPrismaService.documents.delete).not.toHaveBeenCalled();
    });
  });

  describe('isValidRefCode', () => {
    it('should return true for valid reference code', async () => {
      mockPrismaService.nsRefCodes.count.mockResolvedValue(1);
      
      const result = await service.isValidRefCode(1, 'test-entity');
      
      expect(result).toBeTruthy();
      expect(mockPrismaService.nsRefCodes.count).toHaveBeenCalledWith({
        where: {
          id: 1,
          entity: 'test-entity',
        },
      });
    });

    it('should return false for invalid reference code', async () => {
      mockPrismaService.nsRefCodes.count.mockResolvedValue(0);
      
      const result = await service.isValidRefCode(999, 'test-entity');
      
      expect(result).toBeFalsy();
    });
  });

  describe('findByAccessKeyAndCompany', () => {
    it('should find document by access key and company id', async () => {
      mockPrismaService.documents.findFirst.mockResolvedValue(mockDocuments[0]);
      
      const result = await service.findByAccessKeyAndCompany('key1', 1);
      
      expect(result).toEqual(mockDocuments[0]);
      expect(mockPrismaService.documents.findFirst).toHaveBeenCalledWith({
        where: {
          access_key: 'key1',
          id_company: 1,
        },
      });
    });

    it('should return null if document not found', async () => {
      mockPrismaService.documents.findFirst.mockResolvedValue(null);
      
      const result = await service.findByAccessKeyAndCompany('non-existent', 999);
      
      expect(result).toBeNull();
    });
  });
});
