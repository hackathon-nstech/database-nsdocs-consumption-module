-- AlterTable
ALTER TABLE `consumption` ADD COLUMN `document_type_id` INTEGER NULL,
    ADD COLUMN `origin_id` INTEGER NULL,
    ADD COLUMN `status_id` INTEGER NULL;

-- AddForeignKey
ALTER TABLE `consumption` ADD CONSTRAINT `consumption_origin_id_fkey` FOREIGN KEY (`origin_id`) REFERENCES `ns_ref_codes`(`id`) ON DELETE SET NULL ON UPDATE CASCADE;

-- AddForeignKey
ALTER TABLE `consumption` ADD CONSTRAINT `consumption_document_type_id_fkey` FOREIGN KEY (`document_type_id`) REFERENCES `ns_ref_codes`(`id`) ON DELETE SET NULL ON UPDATE CASCADE;

-- AddForeignKey
ALTER TABLE `consumption` ADD CONSTRAINT `consumption_status_id_fkey` FOREIGN KEY (`status_id`) REFERENCES `ns_ref_codes`(`id`) ON DELETE SET NULL ON UPDATE CASCADE;
