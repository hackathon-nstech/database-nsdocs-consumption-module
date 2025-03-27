import { PrismaClient } from '@prisma/client';

const prisma = new PrismaClient();

async function main() {
  const origin = ['file', 'email', 'ws'];
  const documentType = ['cfe', 'cte', 'cteos', 'mdfe', 'nfce', 'nfe', 'nfse'];
  const status = ['ok', 'pending', 'error', 'non-existing'];

  const entities = [
    { name: 'origin', values: origin },
    { name: 'document_type', values: documentType },
    { name: 'status', values: status },
  ];

  for (const entity of entities) {
    for (const value of entity.values) {
      await prisma.nsRefCodes.upsert({
        where: { uk_entity_description: { entity: entity.name, description: value } },
        update: {},
        create: {
          entity: entity.name,
          description: value,
        },
      });
    }
  }
}

main()
  .catch((e) => {
    console.error(e);
    process.exit(1);
  })
  .finally(async () => {
    await prisma.$disconnect();
  });