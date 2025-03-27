# Technical Context: NSdocs Document Consumption Module

## Technology Stack

### Core Technologies
- .NET Core 9.0
- MySQL Database
- RabbitMQ
- Docker/Docker Compose

### Framework & Libraries
- MediatR for CQRS
- FluentValidation
- Entity Framework Core
- MassTransit
- xUnit & Moq

### Development Setup
```bash
# Start infrastructure
docker-compose up -d

# Database initialization
# Automatically runs:
# 1. ddl.sql - Creates schema and objects
# 2. dml.sql - Inserts sample data

# RabbitMQ
# Available at localhost:15672
# Default credentials: guest/guest
# Queue name: nsdocs-documents
```

## Project Configuration

### Solution Structure
```
NSdocs.sln
├── src/
│   ├── NSdocs.API/
│   │   ├── Controllers/
│   │   ├── Program.cs
│   │   └── appsettings.json
│   ├── NSdocs.Application/
│   │   ├── Commands/
│   │   ├── Events/
│   │   ├── Validation/
│   │   └── Handlers/
│   ├── NSdocs.Domain/
│   │   ├── Entities/
│   │   ├── Enums/
│   │   └── Interfaces/
│   ├── NSdocs.Infrastructure/
│   │   ├── Data/
│   │   ├── Messaging/
│   │   └── Repositories/
│   └── NSdocs.Worker/
│       ├── Consumers/
│       ├── Program.cs
│       └── appsettings.json
└── tests/
    ├── NSdocs.UnitTests/
    └── NSdocs.IntegrationTests/
```

### Database Schema
Using existing schema with EF Core mapping:
```sql
# Documents Table
CREATE TABLE documents (
  id bigint NOT NULL AUTO_INCREMENT,
  id_company int NOT NULL,
  access_key varchar(44) NOT NULL,
  request_date timestamp NOT NULL DEFAULT CURRENT_TIMESTAMP,
  updated_date timestamp NOT NULL DEFAULT CURRENT_TIMESTAMP,
  origin enum('file','email','ws') NOT NULL,
  document_type enum('cfe','cte','cteos','mdfe','nfce','nfe','nfse') NOT NULL,
  status enum('ok','pending','error','non-existing') NOT NULL,
  PRIMARY KEY (id),
  UNIQUE KEY uk_access_key_company (access_key,id_company)
)

# Consumption Table
CREATE TABLE consumption (
  id int NOT NULL AUTO_INCREMENT,
  id_company int NOT NULL,
  consumption_date date NOT NULL,
  origin enum('file','email','ws') NOT NULL,
  document_type enum('cfe','cte','cteos','mdfe','nfce','nfe','nfse') NOT NULL,
  status enum('ok','pending','error','non-existing') NOT NULL,
  quantity int NOT NULL DEFAULT 0,
  total int NOT NULL DEFAULT 0,
  PRIMARY KEY (id),
  UNIQUE KEY uk_company_type_origin_date_status (id_company,consumption_date,origin,document_type,status)
)
```

## Configuration Files

### API Configuration
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=nsdocs_consumption;User=root;Password=your_password;"
  },
  "RabbitMQ": {
    "Host": "localhost",
    "Username": "guest",
    "Password": "guest",
    "QueueName": "nsdocs-documents"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft": "Warning"
    }
  }
}
```

### Worker Configuration
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=nsdocs_consumption;User=root;Password=your_password;"
  },
  "RabbitMQ": {
    "Host": "localhost",
    "Username": "guest",
    "Password": "guest",
    "QueueName": "nsdocs-documents",
    "PrefetchCount": 100
  }
}
```

## Technical Requirements

### API Requirements
1. **Performance**
   - Response time < 200ms
   - Concurrent request handling
   - Efficient command processing

2. **Scalability**
   - Horizontal scaling ready
   - Load balancing support
   - Event-driven design

3. **Reliability**
   - Error handling
   - Retries for transient failures
   - Circuit breakers
   - Dead letter handling

4. **Security**
   - Input validation
   - SQL injection prevention
   - Event validation
   - Error masking

### Worker Requirements
1. **Event Processing**
   - Concurrent message handling
   - Ordered processing when needed
   - Error recovery
   - Dead letter handling

2. **Performance**
   - Processing time < 100ms
   - Batch capabilities
   - Resource efficient
   - Memory management

3. **Monitoring**
   - Queue metrics
   - Processing statistics
   - Error tracking
   - Health checks

## NuGet Dependencies

### API & Application
```xml
<ItemGroup>
  <!-- Core -->
  <PackageReference Include="Microsoft.AspNetCore.App" />
  <PackageReference Include="MediatR" Version="12.x" />
  <PackageReference Include="FluentValidation" Version="11.x" />
  
  <!-- Database -->
  <PackageReference Include="Microsoft.EntityFrameworkCore" Version="9.x" />
  <PackageReference Include="Pomelo.EntityFrameworkCore.MySql" Version="9.x" />
  
  <!-- Messaging -->
  <PackageReference Include="MassTransit" Version="8.x" />
  <PackageReference Include="MassTransit.RabbitMQ" Version="8.x" />
  
  <!-- Testing -->
  <PackageReference Include="xunit" Version="2.x" />
  <PackageReference Include="Moq" Version="4.x" />
  <PackageReference Include="FluentAssertions" Version="6.x" />
</ItemGroup>
```

### Worker Service
```xml
<ItemGroup>
  <!-- Core -->
  <PackageReference Include="Microsoft.Extensions.Hosting" Version="9.x" />
  
  <!-- Messaging -->
  <PackageReference Include="MassTransit" Version="8.x" />
  <PackageReference Include="MassTransit.RabbitMQ" Version="8.x" />
  
  <!-- Database -->
  <PackageReference Include="Microsoft.EntityFrameworkCore" Version="9.x" />
  <PackageReference Include="Pomelo.EntityFrameworkCore.MySql" Version="9.x" />
</ItemGroup>
```

## Technical Constraints

### API Constraints
- Maximum response time: 200ms
- Maximum concurrent requests: 1000
- Database connection pool size: 100
- Request timeout: 30 seconds

### Worker Constraints
- Maximum processing time: 100ms
- Maximum queue depth: 1000
- Prefetch count: 100
- Retry limit: 3

### RabbitMQ Constraints
- Queue TTL: 24 hours
- Message size limit: 1MB
- Delivery mode: Persistent
- Acknowledgment required

### Database Constraints
- Connection timeout: 30 seconds
- Command timeout: 30 seconds
- Max pool size: 100
- Retry count: 3
