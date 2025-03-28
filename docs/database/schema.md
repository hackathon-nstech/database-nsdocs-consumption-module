# Database Schema: NSdocs Consumption Module

This document describes the structure of the database tables used by the consumption module. The primary database is MySQL.

## Tables

### 1. `documents`

Stores information about individual documents processed by the system.

```sql
CREATE TABLE IF NOT EXISTS `documents` (
  `id` bigint NOT NULL AUTO_INCREMENT,
  `id_company` int NOT NULL,
  `access_key` varchar(44) NOT NULL,
  `request_date` timestamp NOT NULL DEFAULT CURRENT_TIMESTAMP,
  `updated_date` timestamp NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  `origin` enum('file','email','ws') NOT NULL COMMENT 'Mapped from DocumentOrigin enum (File,Email,Ws)',
  `document_type` enum('cfe','cte','cteos','mdfe','nfce','nfe','nfse') NOT NULL COMMENT 'Mapped from DocumentType enum (Cfe,Cte,Cteos,Mdfe,Nfce,Nfe,Nfse)',
  `status` enum('ok','pending','error','non-existing') NOT NULL COMMENT 'Mapped from DocumentStatus enum (Ok,Pending,Error,NonExisting)',
  PRIMARY KEY (`id`) USING BTREE,
  UNIQUE KEY `uk_access_key_company` (`access_key`,`id_company`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;
```

**Columns:**

*   `id`: Auto-incrementing primary key.
*   `id_company`: Foreign key referencing the company associated with the document.
*   `access_key`: Unique identifier for the document (e.g., NFe access key).
*   `request_date`: Timestamp when the document was first processed or requested. Defaults to the time of insertion.
*   `updated_date`: Timestamp of the last update to the document record. Automatically updated by the database.
*   `origin`: Source from which the document was received (`file`, `email`, `ws`).
*   `document_type`: Type of the fiscal document (`cfe`, `cte`, etc.).
*   `status`: Current processing status of the document (`ok`, `pending`, `error`, `non-existing`).

**Indexes:**

*   `PRIMARY KEY (id)`
*   `UNIQUE KEY uk_access_key_company (access_key, id_company)`: Ensures a document with the same access key is unique per company.

### 2. `consumption`

Stores aggregated consumption counts based on document dimensions. This table is populated and updated by the `FlushWorker`.

```sql
CREATE TABLE IF NOT EXISTS `consumption` (
  `id` int NOT NULL AUTO_INCREMENT,
  `id_company` int NOT NULL,
  `consumption_date` date NOT NULL,
  `origin` enum('file','email','ws') NOT NULL COMMENT 'Mapped from DocumentOrigin enum (File,Email,Ws)',
  `document_type` enum('cfe','cte','cteos','mdfe','nfce','nfe','nfse') NOT NULL COMMENT 'Mapped from DocumentType enum (Cfe,Cte,Cteos,Mdfe,Nfce,Nfe,Nfse)',
  `status` enum('ok','pending','error','non-existing') NOT NULL COMMENT 'Mapped from DocumentStatus enum (Ok,Pending,Error,NonExisting)',
  `quantity` int NOT NULL DEFAULT 0,
  `total` int NOT NULL DEFAULT 0,
  PRIMARY KEY (`id`) USING BTREE,
  UNIQUE KEY `uk_company_type_origin_date_status` (`id_company`,`consumption_date`,`origin`,`document_type`,`status`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;
```

**Columns:**

*   `id`: Auto-incrementing primary key.
*   `id_company`: The company ID for this consumption record.
*   `consumption_date`: The date (day) for which these counts apply.
*   `origin`: The document origin dimension.
*   `document_type`: The document type dimension.
*   `status`: The document status dimension.
*   `quantity`: The *current* number of documents matching this exact combination of dimensions (`id_company`, `consumption_date`, `origin`, `document_type`, `status`).
*   `total`: The *cumulative* number of documents that have *ever* matched this exact combination of dimensions.

**Indexes:**

*   `PRIMARY KEY (id)`
*   `UNIQUE KEY uk_company_type_origin_date_status (id_company, consumption_date, origin, document_type, status)`: Ensures there is only one row for each unique combination of dimensions, preventing duplicate aggregation records.

## Entity Framework Core Mapping

### Enum Handling

Both tables use `EnumToStringConverter<T>` for mapping enum values between C# and MySQL:

1. **C# to Database:**
   - PascalCase enum values are converted to lowercase with hyphens
   - Example: `NonExisting` → `'non-existing'`

2. **Database to C#:**
   - Database values are converted back to their corresponding enum values
   - Example: `'non-existing'` → `NonExisting`

### Example Mappings:

```csharp
// DocumentOrigin
File → 'file'
Email → 'email'
Ws → 'ws'

// DocumentType
Cfe → 'cfe'
Cte → 'cte'
Cteos → 'cteos'
Mdfe → 'mdfe'
Nfce → 'nfce'
Nfe → 'nfe'
Nfse → 'nfse'

// DocumentStatus
Ok → 'ok'
Pending → 'pending'
Error → 'error'
NonExisting → 'non-existing'
```

## Relationships

*   The `consumption` table aggregates data derived from the `documents` table over time. There isn't a direct foreign key relationship, but the data in `consumption` represents counts of records that exist (or existed) in the `documents` table matching the specified dimensions.
