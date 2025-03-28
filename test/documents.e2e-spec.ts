import { Test, TestingModule } from '@nestjs/testing';
import { INestApplication } from '@nestjs/common';
import * as request from 'supertest';
import { AppModule } from '../src/modules/app/app.module';
import { PrismaService } from '../src/infra/prisma/prisma.service';
import { Documents } from '@prisma/client';

describe('DocumentsController (e2e)', () => {
  let app: INestApplication;
  let prismaService: PrismaService;
  let createdDocumentId: string;

  beforeAll(async () => {
    const moduleFixture: TestingModule = await Test.createTestingModule({
      imports: [AppModule],
    }).compile();

    app = moduleFixture.createNestApplication();
    prismaService = moduleFixture.get<PrismaService>(PrismaService);
    await app.init();
    
    // Clean up test documents before starting the tests
    await cleanupTestDocuments();
  });

  afterAll(async () => {
    await cleanupTestDocuments();
    await app.close();
  });

  async function cleanupTestDocuments() {
    // Delete test documents with specific access keys to ensure clean test state
    await prismaService.documents.deleteMany({
      where: {
        access_key: {
          startsWith: 'test-e2e-',
        },
      },
    });
  }

  describe('/documents (POST)', () => {
    it('should create a new document', async () => {
      const newDocument = {
        id_company: 1,
        access_key: `test-e2e-${Date.now()}`,
        request_date: new Date().toISOString(),
        updated_date: new Date().toISOString(),
        origin_id: 1,
        document_type_id: 1,
        status_id: 1
      };

      const response = await request(app.getHttpServer())
        .post('/documents')
        .send(newDocument)
        .expect(201);

      expect(response.body).toBeDefined();
      expect(response.body.id).toBeDefined();
      expect(response.body.access_key).toBe(newDocument.access_key);
      
      createdDocumentId = response.body.id;
    });

    it('should reject duplicate document with same access_key and id_company', async () => {
      const existingDocument = await prismaService.documents.findFirst({
        where: {
          id: BigInt(createdDocumentId)
        },
      });

      if (!existingDocument) {
        fail('Required test document not found');
        return;
      }

      const duplicateDocument = {
        id_company: existingDocument.id_company,
        access_key: existingDocument.access_key,
        request_date: new Date().toISOString(),
        updated_date: new Date().toISOString(),
        origin_id: 1,
        document_type_id: 1,
        status_id: 1
      };

      await request(app.getHttpServer())
        .post('/documents')
        .send(duplicateDocument)
        .expect(400);
    });

    it('should reject document with invalid reference codes', async () => {
      const invalidDocument = {
        id_company: 999,
        access_key: `test-e2e-${Date.now()}`,
        request_date: new Date().toISOString(),
        updated_date: new Date().toISOString(),
        origin_id: 9999, // Invalid origin_id
        document_type_id: 1,
        status_id: 1
      };

      await request(app.getHttpServer())
        .post('/documents')
        .send(invalidDocument)
        .expect(400);
    });
  });

  describe('/documents (GET)', () => {
    it('should retrieve all documents', async () => {
      const response = await request(app.getHttpServer())
        .get('/documents')
        .expect(200);

      expect(Array.isArray(response.body)).toBe(true);
    });
  });

  describe('/documents/:id (GET)', () => {
    it('should retrieve a document by ID', async () => {
      // First ensure we have a valid ID
      if (!createdDocumentId) {
        fail('Required test document ID not available');
        return;
      }

      const response = await request(app.getHttpServer())
        .get(`/documents/${createdDocumentId}`)
        .expect(200);

      expect(response.body).toBeDefined();
      expect(response.body.id).toBe(createdDocumentId);
    });

    it('should return null for non-existent document ID', async () => {
      const response = await request(app.getHttpServer())
        .get('/documents/999999999')
        .expect(200);

      expect(response.body).toBeNull();
    });
  });

  describe('/documents/:id (PATCH)', () => {
    it('should update a document', async () => {
      // First ensure we have a valid ID
      if (!createdDocumentId) {
        fail('Required test document ID not available');
        return;
      }

      const updateData = {
        access_key: `test-e2e-updated-${Date.now()}`
      };

      const response = await request(app.getHttpServer())
        .patch(`/documents/${createdDocumentId}`)
        .send(updateData)
        .expect(200);

      expect(response.body).toBeDefined();
      expect(response.body.access_key).toBe(updateData.access_key);
    });

    it('should reject update with invalid reference codes', async () => {
      // First ensure we have a valid ID
      if (!createdDocumentId) {
        fail('Required test document ID not available');
        return;
      }

      const updateData = {
        status_id: 9999 // Invalid status_id
      };

      await request(app.getHttpServer())
        .patch(`/documents/${createdDocumentId}`)
        .send(updateData)
        .expect(400);
    });
  });

  describe('/documents/:id (DELETE)', () => {
    it('should delete a document', async () => {
      // First ensure we have a valid ID
      if (!createdDocumentId) {
        fail('Required test document ID not available');
        return;
      }

      await request(app.getHttpServer())
        .delete(`/documents/${createdDocumentId}`)
        .expect(200);

      // Verify the document was actually deleted
      const response = await request(app.getHttpServer())
        .get(`/documents/${createdDocumentId}`)
        .expect(200);

      expect(response.body).toBeNull();
    });
  });
});