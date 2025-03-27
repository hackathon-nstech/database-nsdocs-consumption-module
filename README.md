# NSdocs Document Consumption Module

This project implements a .NET Core API for document consumption tracking, replacing MySQL triggers with command handlers and RabbitMQ events.

## Project Structure

- **NSdocs.API**: API endpoints and controllers
- **NSdocs.Application**: Application logic, commands, queries, and events
- **NSdocs.Domain**: Domain entities and enums
- **NSdocs.Infrastructure**: Infrastructure concerns like database access and event publishing
- **NSdocs.Worker**: Background worker for processing document events

## Implementation Details

### Replacing MySQL Triggers

The original implementation used MySQL triggers to update consumption records when documents were created, updated, or deleted. This implementation replaces those triggers with:

1. Command handlers that publish events when documents are modified
2. A worker service that consumes these events and updates consumption records

### Event-Driven Architecture

- **Document Events**: Events are published when documents are created, updated, or deleted
- **Event Publishing**: Currently using an in-memory publisher, with a RabbitMQ implementation ready for production
- **Event Consumption**: A worker service consumes events and updates consumption records

## Database Changes

To migrate from the trigger-based approach to the event-driven approach:

1. Backup the current consumption data and remove triggers using one of these methods:

   **Option 1**: Run the shell script (recommended):
   ```bash
   chmod +x scripts/run-drop-triggers.sh
   ./scripts/run-drop-triggers.sh
   ```

   **Option 2**: Run the SQL script directly:
   ```bash
   mysql -u root nsdocs_consumption < scripts/drop_triggers.sql
   ```

   The script will:
   - Create a backup of current consumption data in a `backup_consumptions` table
   - Remove the MySQL triggers
   - Keep the stored procedure for reference

2. Deploy services:
   - Start the RabbitMQ container
   - Deploy the API service
   - Deploy the worker service

## Running the Application

### Prerequisites

- .NET 9.0 SDK
- Docker and Docker Compose

### Development Setup

1. Clone the repository
2. Start the infrastructure services:

```bash
docker-compose up -d
```

This will start:
- MySQL database on port 3306
- RabbitMQ on port 5672 (AMQP) and 15672 (Management UI)

3. Run the API:

```bash
cd src/NSdocs.API
dotnet run
```

4. Run the worker (when implemented):

```bash
cd src/NSdocs.Worker
dotnet run
```

### RabbitMQ Management UI

The RabbitMQ Management UI is available at http://localhost:15672 with the following credentials:
- Username: guest
- Password: guest

## Implementation Checkpoints

### Checkpoint 1: Event Publishing ✅

- Created event classes for document operations
- Implemented event publishing in command handlers
- Created stub implementations for event publishers

### Checkpoint 2: Database Migration ✅

- Created migration script to drop triggers
- Kept stored procedure for reference

### Checkpoint 3: Worker Service (Placeholder) ✅

- Created worker project structure
- Implemented placeholder for event consumers
- Ready for RabbitMQ integration

### Checkpoint 4: Docker Setup ✅

- Added RabbitMQ to docker-compose.yml
- Configured connection settings in appsettings.json
- Set up networking between services

## Future Enhancements

1. **RabbitMQ Integration**: Replace the in-memory publisher with the RabbitMQ implementation
2. **Worker Implementation**: Implement the consumption update logic in the worker
3. **Monitoring**: Add monitoring and logging for event processing
4. **Error Handling**: Implement retry and dead letter handling for failed events
