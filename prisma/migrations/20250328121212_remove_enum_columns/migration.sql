/*
  Warnings:

  - You are about to drop the column `document_type` on the `consumption` table. All the data in the column will be lost.
  - You are about to drop the column `origin` on the `consumption` table. All the data in the column will be lost.
  - You are about to drop the column `status` on the `consumption` table. All the data in the column will be lost.
  - You are about to drop the column `document_type` on the `documents` table. All the data in the column will be lost.
  - You are about to drop the column `origin` on the `documents` table. All the data in the column will be lost.
  - You are about to drop the column `status` on the `documents` table. All the data in the column will be lost.
  - A unique constraint covering the columns `[id_company,consumption_date,origin_id,document_type_id,status_id]` on the table `consumption` will be added. If there are existing duplicate values, this will fail.

*/
-- DropIndex
DROP INDEX `uk_company_type_origin_date_status` ON `consumption`;

-- AlterTable
ALTER TABLE `consumption` DROP COLUMN `document_type`,
    DROP COLUMN `origin`,
    DROP COLUMN `status`;

-- AlterTable
ALTER TABLE `documents` DROP COLUMN `document_type`,
    DROP COLUMN `origin`,
    DROP COLUMN `status`;

-- CreateIndex
CREATE UNIQUE INDEX `uk_company_type_origin_date_status` ON `consumption`(`id_company`, `consumption_date`, `origin_id`, `document_type_id`, `status_id`);
