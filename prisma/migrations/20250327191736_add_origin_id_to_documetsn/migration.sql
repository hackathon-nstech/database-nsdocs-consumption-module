-- CreateTable
CREATE TABLE `documents` (
    `id` BIGINT NOT NULL AUTO_INCREMENT,
    `id_company` INTEGER NOT NULL,
    `access_key` VARCHAR(44) NOT NULL,
    `request_date` TIMESTAMP(0) NOT NULL DEFAULT CURRENT_TIMESTAMP(0),
    `updated_date` TIMESTAMP(0) NOT NULL DEFAULT CURRENT_TIMESTAMP(0),
    `origin` ENUM('file', 'email', 'ws') NOT NULL,
    `origin_id` INTEGER NULL,
    `document_type` ENUM('cfe', 'cte', 'cteos', 'mdfe', 'nfce', 'nfe', 'nfse') NOT NULL,
    `status` ENUM('ok', 'pending', 'error', 'non-existing') NOT NULL,

    UNIQUE INDEX `uk_access_key_company`(`access_key`, `id_company`),
    PRIMARY KEY (`id`)
) DEFAULT CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;

-- CreateTable
CREATE TABLE `consumption` (
    `id` INTEGER NOT NULL AUTO_INCREMENT,
    `id_company` INTEGER NOT NULL,
    `consumption_date` DATE NOT NULL,
    `origin` ENUM('file', 'email', 'ws') NOT NULL,
    `document_type` ENUM('cfe', 'cte', 'cteos', 'mdfe', 'nfce', 'nfe', 'nfse') NOT NULL,
    `status` ENUM('ok', 'pending', 'error', 'non-existing') NOT NULL,
    `quantity` INTEGER NOT NULL DEFAULT 0,
    `total` INTEGER NOT NULL DEFAULT 0,

    UNIQUE INDEX `uk_company_type_origin_date_status`(`id_company`, `consumption_date`, `origin`, `document_type`, `status`),
    PRIMARY KEY (`id`)
) DEFAULT CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;

-- CreateTable
CREATE TABLE `ns_ref_codes` (
    `id` INTEGER NOT NULL AUTO_INCREMENT,
    `entity` VARCHAR(50) NOT NULL,
    `description` VARCHAR(100) NOT NULL,
    `created_at` TIMESTAMP(0) NOT NULL DEFAULT CURRENT_TIMESTAMP(0),
    `updated_at` TIMESTAMP(0) NOT NULL DEFAULT CURRENT_TIMESTAMP(0),

    INDEX `idx_entity`(`entity`),
    UNIQUE INDEX `ns_ref_codes_entity_description_key`(`entity`, `description`),
    PRIMARY KEY (`id`)
) DEFAULT CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;

-- AddForeignKey
ALTER TABLE `documents` ADD CONSTRAINT `documents_origin_id_fkey` FOREIGN KEY (`origin_id`) REFERENCES `ns_ref_codes`(`id`) ON DELETE SET NULL ON UPDATE CASCADE;
