# System Patterns: NSdocs Document Consumption Module

## Architecture Overview

```mermaid
flowchart TD
    subgraph "API Layer"
        A1[API Instances] --> A2[Endpoints]
        A2 --> A3[EF Core]
        A2 --> R[Redis]
    end
    
    subgraph "Domain Layer"
        B1[Entities] --> B2[Enums]
        B2 --> B3[Interfaces]
    end
    
    subgraph "Infrastructure Layer"
        C1[DbContext] --> C2[Entity Configurations]
        C2 --> C3[Value Converters]
        C4[Redis Services] --> R
    end

    subgraph "Flusher Service"
        F1[Flusher Instances] --> R
        F1 --> A3
    end
    
    A3 --> C1
    C1 --> B1
    A1 --> C4
```

## Clean Architecture Implementation

### 1. Domain Layer
```mermaid
graph TD
    A[Document Entity] --> B[Enums]
    B --> B1[DocumentType]
    B --> B2[DocumentOrigin]
    B --> B3[DocumentStatus]
```

### 2. Infrastructure Layer
```mermaid
graph TD
    A[ApplicationDbContext] --> B[Entity Configurations]
    B --> C[Value Converters]
    C --> D[EnumToStringConverter]
    E[Redis Services] --> F[RedisConnectionFactory]
    E --> G[RedisEventPublisher]
    E --> H[RedisLockManager]
```

### 3. API Layer
```mermaid
graph TD
    A[Program.cs] --> B[Dependency Injection]
    B --> C[Endpoint Configuration]
    C --> D[Swagger/OpenAPI]
```

### 4. Flusher Service Layer
```mermaid
graph TD
    FS1[Program.cs] --> FS2[Dependency Injection]
    FS2 --> FS3[FlushWorker]
    FS3 --> FS4[WorkDistributor]
    FS3 --> FS5[HealthMonitor]
    FS3 --> H[RedisLockManager]
```

## Design Patterns

### 1. Redis-Based Aggregation
- **API**: Writes deltas directly to Redis using atomic operations (`INCRBY`, `SADD`).
- **Redis Keys**: Structured keys for aggregation (`agg:company:{id}:{date}:{origin}:{type}:{status}:{metric}`).
- **Flusher**: Periodically reads aggregated values, updates the database, and resets Redis counters.

### 2. Distributed Flusher Coordination
- **Goal**: Allow multiple flusher instances to run concurrently without conflicts.
- **Components**:
    - `RedisLockManager`: Provides distributed locking using Redis `SET NX PX`.
    - `WorkDistributor`: Assigns companies to specific flushers based on hashing.
    - `HealthMonitor`: Detects failed flushers via heartbeats and triggers work redistribution.
- **Protocol**: Flushers register, claim work, acquire locks per company, process, and release locks.

### 3. Database Contention Mitigation (Flusher)
- **Optimize Updates**: Ensure efficient `UPDATE` statements on the `consumption` table using proper indexing (`uk_company_type_origin_date_status`).
- **Reduce Lock Scope**: Use row-level locking (InnoDB default) and keep database transactions short (fetch from Redis first, then transact DB update, commit, then update Redis state).
- **Batching (Optional)**: Consider batching multiple company updates within a single flusher transaction if needed, balancing throughput vs. lock duration.
- **Connection Pooling**: Utilize EF Core's connection pooling effectively.
- **Monitoring**: Track database lock waits, transaction times, and deadlocks.

### 4. Generic Value Converter
- `EnumToStringConverter<T>`: Handles enum-to-string mapping.
- Automatic PascalCase to kebab-case conversion.
- Reusable across all enum properties.

### 5. Dependency Injection
- Services registered in respective layers (`AddInfrastructure`, `AddApplication`, Flusher DI).
- Clean separation of concerns.

### 6. Repository Configuration
- Fluent API configurations for EF Core entities.
- Strong typing with generics.
- Consistent naming conventions.

## Data Mappings

### Enum Mapping Strategy
```mermaid
flowchart LR
    A[PascalCase Enum] -->|ToLower| B[Lowercase]
    B -->|Insert Hyphens| C[kebab-case]
    C -->|Database| D[String Column]
    D -->|Remove Hyphens| E[Uppercase]
    E -->|Parse Enum| F[PascalCase Enum]
```

### Entity Configuration
```csharp
// Example configuration pattern
builder.Property(x => x.EnumProperty)
    .HasColumnName("column_name")
    .HasConversion(new EnumToStringConverter<TEnum>())
    .IsRequired();
```

## API Documentation

### Endpoints
1. GET / - Welcome message
2. GET /health - Health check
3. GET /api/documents - List documents (CRUD endpoints)

### Documentation
- OpenAPI/Swagger UI at /docs
- JSON schema at /docs/v1/swagger.json

## Directory Structure
```
src/
├── NSdocs.API/
│   ├── Endpoints/
│   └── Program.cs
├── NSdocs.Application/
│   ├── Commands/
│   ├── Queries/
│   └── Common/Interfaces/
├── NSdocs.Domain/
│   ├── Entities/
│   └── Enums/
├── NSdocs.Infrastructure/
│   ├── Data/
│   │   ├── Configurations/
│   │   └── Converters/
│   ├── Services/ (Redis, Locking)
│   └── DependencyInjection.cs
└── NSdocs.Flusher/
    ├── Workers/ (FlushWorker)
    ├── Services/ (Distributor, Monitor)
    └── Program.cs
