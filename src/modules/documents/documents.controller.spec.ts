import { Test, TestingModule } from '@nestjs/testing';
import { DocumentsController } from './documents.controller';
import { DocumentsService } from './documents.service';
import { BadRequestException } from '@nestjs/common';
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

// Mock DocumentsService
const mockDocumentsService = {
  findAll: jest.fn(),
  findOne: jest.fn(),
  create: jest.fn(),
  update: jest.fn(),
  delete: jest.fn(),
  updateCompanyConsumption: jest.fn(),
  isValidRefCode: jest.fn(),
  findByAccessKeyAndCompany: jest.fn(),
};

describe('DocumentsController', () => {
  let controller: DocumentsController;

  beforeEach(async () => {
    const module: TestingModule = await Test.createTestingModule({
      controllers: [DocumentsController],
      providers: [
        {
          provide: DocumentsService,
          useValue: mockDocumentsService,
        },
      ],
    }).compile();

    controller = module.get<DocumentsController>(DocumentsController);
    
    // Reset all mocks before each test
    jest.clearAllMocks();
  });

  it('should be defined', () => {
    expect(controller).toBeDefined();
  });

  describe('findAll', () => {
    it('should return an array of documents', async () => {
      mockDocumentsService.findAll.mockResolvedValue(mockDocuments);
      
      const result = await controller.findAll();
      
      expect(result).toEqual(mockDocuments);
      expect(mockDocumentsService.findAll).toHaveBeenCalled();
    });
  });

  describe('findOne', () => {
    it('should return a document by id', async () => {
      mockDocumentsService.findOne.mockResolvedValue(mockDocuments[0]);
      
      const result = await controller.findOne('1');
      
      expect(result).toEqual(mockDocuments[0]);
      expect(mockDocumentsService.findOne).toHaveBeenCalledWith(BigInt(1));
    });

    it('should return null if document not found', async () => {
      mockDocumentsService.findOne.mockResolvedValue(null);
      
      const result = await controller.findOne('999');
      
      expect(result).toBeNull();
    });
  });

  describe('create', () => {
    it('should create a document successfully', async () => {
      const createDocumentData = {
        id_company: 3,
        access_key: 'key3',
        request_date: new Date(),
        updated_date: new Date(),
        origin_id: 3,
        document_type_id: 3,
        status_id: 3,
      };
      
      const createdDocument = {
        id: BigInt(3),
        ...createDocumentData,
      };
      
      mockDocumentsService.isValidRefCode.mockResolvedValue(true);
      mockDocumentsService.findByAccessKeyAndCompany.mockResolvedValue(null);
      mockDocumentsService.create.mockResolvedValue(createdDocument);
      
      const result = await controller.create(createDocumentData);
      
      expect(result).toEqual(createdDocument);
      expect(mockDocumentsService.isValidRefCode).toHaveBeenCalledTimes(3);
      expect(mockDocumentsService.findByAccessKeyAndCompany).toHaveBeenCalledWith(
        createDocumentData.access_key,
        createDocumentData.id_company
      );
      expect(mockDocumentsService.create).toHaveBeenCalled();
    });

    it('should throw BadRequestException for invalid origin_id', async () => {
      const createDocumentData = {
        id_company: 3,
        access_key: 'key3',
        origin_id: 999,
        document_type_id: 3,
        status_id: 3,
      };
      
      mockDocumentsService.isValidRefCode.mockImplementation(
        (id: number, entity: string) => entity === 'origin' ? false : true
      );
      
      await expect(controller.create(createDocumentData)).rejects.toThrow(BadRequestException);
      expect(mockDocumentsService.isValidRefCode).toHaveBeenCalledWith(999, 'origin');
      expect(mockDocumentsService.create).not.toHaveBeenCalled();
    });

    it('should throw BadRequestException for invalid document_type_id', async () => {
      const createDocumentData = {
        id_company: 3,
        access_key: 'key3',
        origin_id: 3,
        document_type_id: 999,
        status_id: 3,
      };
      
      mockDocumentsService.isValidRefCode.mockImplementation(
        (id: number, entity: string) => entity === 'document_type' ? false : true
      );
      
      await expect(controller.create(createDocumentData)).rejects.toThrow(BadRequestException);
      expect(mockDocumentsService.create).not.toHaveBeenCalled();
    });

    it('should throw BadRequestException for invalid status_id', async () => {
      const createDocumentData = {
        id_company: 3,
        access_key: 'key3',
        origin_id: 3,
        document_type_id: 3,
        status_id: 999,
      };
      
      mockDocumentsService.isValidRefCode.mockImplementation(
        (id: number, entity: string) => entity === 'status' ? false : true
      );
      
      await expect(controller.create(createDocumentData)).rejects.toThrow(BadRequestException);
      expect(mockDocumentsService.create).not.toHaveBeenCalled();
    });

    it('should throw BadRequestException for duplicate access_key and id_company', async () => {
      const createDocumentData = {
        id_company: 1,
        access_key: 'key1',
        origin_id: 1,
        document_type_id: 1,
        status_id: 1,
      };
      
      mockDocumentsService.isValidRefCode.mockResolvedValue(true);
      mockDocumentsService.findByAccessKeyAndCompany.mockResolvedValue(mockDocuments[0]);
      
      await expect(controller.create(createDocumentData)).rejects.toThrow(BadRequestException);
      expect(mockDocumentsService.create).not.toHaveBeenCalled();
    });
  });

  describe('update', () => {
    it('should update a document successfully', async () => {
      const updateData = {
        access_key: 'updated_key',
      };
      
      const updatedDocument = {
        ...mockDocuments[0],
        access_key: 'updated_key',
      };
      
      mockDocumentsService.update.mockResolvedValue(updatedDocument);
      
      const result = await controller.update('1', updateData);
      
      expect(result).toEqual(updatedDocument);
      expect(mockDocumentsService.update).toHaveBeenCalledWith(BigInt(1), updateData);
    });

    it('should throw BadRequestException for invalid origin_id', async () => {
      const updateData = {
        origin_id: 999,
      };
      
      mockDocumentsService.isValidRefCode.mockResolvedValue(false);
      
      await expect(controller.update('1', updateData)).rejects.toThrow(BadRequestException);
      expect(mockDocumentsService.update).not.toHaveBeenCalled();
    });
    
    it('should throw BadRequestException for duplicate access_key and id_company', async () => {
      const updateData = {
        access_key: 'key2',
        id_company: 2,
      };
      
      mockDocumentsService.isValidRefCode.mockResolvedValue(true);
      mockDocumentsService.findByAccessKeyAndCompany.mockResolvedValue(mockDocuments[1]);
      
      await expect(controller.update('1', updateData)).rejects.toThrow(BadRequestException);
      expect(mockDocumentsService.update).not.toHaveBeenCalled();
    });
    
    it('should remove id from update data', async () => {
      const updateData = {
        id: BigInt(100),
        access_key: 'updated_key',
      };
      
      const expectedUpdateData = {
        access_key: 'updated_key',
      };
      
      const updatedDocument = {
        ...mockDocuments[0],
        access_key: 'updated_key',
      };
      
      mockDocumentsService.update.mockResolvedValue(updatedDocument);
      
      const result = await controller.update('1', updateData);
      
      expect(result).toEqual(updatedDocument);
      expect(mockDocumentsService.update).toHaveBeenCalledWith(BigInt(1), expectedUpdateData);
    });
  });

  describe('delete', () => {
    it('should delete a document', async () => {
      mockDocumentsService.delete.mockResolvedValue(mockDocuments[0]);
      
      const result = await controller.delete('1');
      
      expect(result).toEqual(mockDocuments[0]);
      expect(mockDocumentsService.delete).toHaveBeenCalledWith(BigInt(1));
    });
  });

  describe('updateConsumption', () => {
    it('should call the service to update company consumption', async () => {
      mockDocumentsService.updateCompanyConsumption.mockResolvedValue(undefined);
      
      await controller.updateConsumption();
      
      expect(mockDocumentsService.updateCompanyConsumption).toHaveBeenCalled();
    });
  });
});
