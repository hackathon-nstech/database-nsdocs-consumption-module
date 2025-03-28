CREATE DATABASE IF NOT EXISTS `nsdocs_consumption`;

USE `nsdocs_consumption`;

CREATE TABLE IF NOT EXISTS `documents` (
  `id` bigint NOT NULL AUTO_INCREMENT,
  `id_company` int NOT NULL,
  `access_key` varchar(44) NOT NULL,
  `request_date` timestamp NOT NULL DEFAULT CURRENT_TIMESTAMP,
  `updated_date` timestamp NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  `origin` enum('file','email','ws') NOT NULL,
  `document_type` enum('cfe','cte','cteos','mdfe','nfce','nfe','nfse') NOT NULL,
  `status` enum('ok','pending','error','non-existing') NOT NULL,
  PRIMARY KEY (`id`) USING BTREE,
  UNIQUE KEY `uk_access_key_company` (`access_key`,`id_company`) 
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE TABLE IF NOT EXISTS `consumption` (
  `id` int NOT NULL AUTO_INCREMENT,
  `id_company` int NOT NULL,
  `consumption_date` date NOT NULL,
  `origin` enum('file','email','ws') NOT NULL,
  `document_type` enum('cfe','cte','cteos','mdfe','nfce','nfe','nfse') NOT NULL,
  `status` enum('ok','pending','error','non-existing') NOT NULL,
  `quantity` int NOT NULL DEFAULT 0,
  `total` int NOT NULL DEFAULT 0,
  PRIMARY KEY (`id`) USING BTREE,
  UNIQUE KEY `uk_company_type_origin_date_status` (`id_company`,`consumption_date`,`origin`,`document_type`,`status`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- Removendo procedures e triggers
-- ...procedures e triggers removidos...
