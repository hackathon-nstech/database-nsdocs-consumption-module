# Project Brief: NSdocs Document Consumption Module

## Core Objective
Convert an existing MySQL-based document consumption tracking system into a .NET Core 9.0 API, following clean architecture principles.

## Key Requirements

### Functional Requirements
1. Migrate existing database logic from MySQL procedures/triggers to API endpoints:
   - Document registration and tracking
   - Consumption calculation and updates
   - Status management
   - Multi-origin support (file, email, ws)

2. Support all existing document types:
   - CFE (Consumer Fiscal Electronic)
   - CTE (Electronic Transport Knowledge)
   - CTEOS (Electronic Transport Knowledge - Others)
   - MDFE (Electronic Freight Manifest)
   - NFCE (Electronic Consumer Invoice)
   - NFE (Electronic Invoice)
   - NFSE (Electronic Service Invoice)

3. Maintain existing business rules:
   - Unique access key per company
   - Monthly consumption tracking
   - Status lifecycle management
   - Origin-based tracking

### Technical Requirements
1. Clean Architecture implementation
2. Standard design patterns usage
3. Performance optimization for high concurrency
4. Data consistency maintenance
5. API-first approach
6. Scalable solution design

## Success Criteria
1. All database business logic successfully migrated to API
2. Zero data loss during migration
3. Improved performance metrics
4. Maintainable and testable codebase
5. Documented API endpoints
6. Reduced system complexity

## Critical Constraints
1. Must maintain backward compatibility
2. Zero downtime during migration
3. Data consistency preservation
4. Performance requirements met under load
