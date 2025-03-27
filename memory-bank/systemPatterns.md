# System Patterns: NSdocs Document Consumption Module

## Architecture Overview

```mermaid
flowchart TD
    subgraph "API Layer"
        A1[Minimal API] --> A2[Endpoints]
        A2 --> A3[EF Core]
    end
    
    subgraph "Domain Layer"
        B1[Entities] --> B2[Enums]
        B2 --> B3[Interfaces]
    end
    
    subgraph "Infrastructure Layer"
        C1[DbContext] --> C2[Entity Configurations]
        C2 --> C3[Value Converters]
    end
    
    A3 --> C1
    C1 --> B1
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
```

### 3. API Layer
```mermaid
graph TD
    A[Program.cs] --> B[Dependency Injection]
    B --> C[Endpoint Configuration]
    C --> D[Swagger/OpenAPI]
```

## Design Patterns

### 1. Generic Value Converter
- `EnumToStringConverter<T>`: Handles enum-to-string mapping
- Automatic PascalCase to kebab-case conversion
- Reusable across all enum properties

### 2. Dependency Injection
- Infrastructure services in `AddInfrastructure()`
- Application services in `AddApplication()`
- Clean separation of concerns

### 3. Repository Configuration
- Fluent API configurations
- Strong typing with generics
- Consistent naming conventions

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
3. GET /api/documents - List documents

### Documentation
- OpenAPI/Swagger UI at /docs
- JSON schema at /docs/v1/swagger.json

## Directory Structure
```
src/
├── NSdocs.API/
│   ├── Endpoints/
│   └── Program.cs
├── NSdocs.Domain/
│   ├── Entities/
│   └── Enums/
└── NSdocs.Infrastructure/
    ├── Data/
    │   ├── Configurations/
    │   └── Converters/
    └── DependencyInjection.cs
```
