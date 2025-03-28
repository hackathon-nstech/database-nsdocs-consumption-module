# NSdocs Document Consumption Module

This project implements a .NET Core API for document consumption tracking, replacing MySQL triggers with command handlers and leveraging Redis for coordination and potential eventing.

## Project Structure

- **NSdocs.API**: API endpoints and controllers
- **NSdocs.Application**: Application logic, commands, queries, and events
- **NSdocs.Domain**: Domain entities and enums
- **NSdocs.Infrastructure**: Infrastructure concerns like database access, Redis interactions, etc.
- **NSdocs.Flusher**: Background worker service for processing tasks/events and updating the database

## How the Solution Works

This solution transitions from a database-trigger-based system to an event-driven architecture for tracking document consumption.

### Core Workflow

1.  **API Interaction**: When a document is created, updated, or deleted via the `NSdocs.API`, the corresponding command handler is invoked.
2.  **Coordination/Eventing**: Instead of directly modifying consumption data in the API request path, the system uses Redis. This could involve Redis Pub/Sub for simple eventing or Redis Streams for more robust event handling, along with Redis data structures for coordination (e.g., locking, work distribution). The `NSdocs.API` interacts with Redis to signal changes or queue work.
3.  **Processing**: The `NSdocs.Flusher` service (acting as a worker) interacts with Redis to pick up tasks or events.
4.  **Consumption Update**: Upon receiving an event, the worker service processes it and updates the consumption records in the database accordingly.

This decoupling improves scalability, maintainability, and resilience compared to using database triggers.

### Key Components

-   **NSdocs.API**: Handles incoming requests, validates data, executes commands, and publishes events.
-   **NSdocs.Application**: Contains the core business logic, command/query handlers, and event definitions.
-   **NSdocs.Infrastructure**: Provides implementations for data access (database context), Redis interactions (locking, potentially Pub/Sub or Streams), etc.
-   **NSdocs.Flusher**: A background service (worker) responsible for processing tasks/events from Redis and updating the database.

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
   - Start the Redis container
   - Deploy the API service (`NSdocs.API`)
   - Deploy the Flusher service (`NSdocs.Flusher`)

## Running the Application

### Prerequisites

- .NET 9.0 SDK
- Docker and Docker Compose

### Using Docker Compose

The `docker-compose.yml` file defines the necessary infrastructure services for local development.

1.  **Start Services**: Navigate to the project root directory in your terminal and run:
    ```bash
    docker-compose up -d
    ```
    The `-d` flag runs the containers in detached mode (in the background).

2.  **Services Started**: This command will build (if necessary) and start the following services defined in `docker-compose.yml`:
    *   `db`: A MySQL database instance, accessible on `localhost:3306`. Database name: `nsdocs_consumption`.
    *   `redis`: A Redis instance, accessible on `localhost:6379`.
    *   `flusher`: Replicas of the background worker service.
    *   `api`: Replicas of the API service.
    *   `nginx`: An Nginx load balancer distributing traffic to the API replicas, accessible on `localhost:5030`.

3.  **Stopping Services**: To stop the services, run:
    ```bash
    docker-compose down
    ```

### Running the .NET Applications

After starting the infrastructure with Docker Compose:

1.  **Run the API**:

```bash
cd src/NSdocs.API
dotnet run
```

2.  **Run the Flusher (Worker)**:
```bash
cd src/NSdocs.Flusher
dotnet run
```

*(Note: When running locally via `dotnet run`, you might need to adjust connection strings in `appsettings.Development.json` to point to `localhost` instead of the service names used in Docker networking, e.g., `localhost:6379` for Redis and `localhost:3306` for MySQL.)*

## Future Enhancements

1. **Refine Redis Usage**: Potentially optimize Redis usage (e.g., choosing between Pub/Sub, Streams, specific data structures).
2. **Flusher Logic**: Enhance the processing logic within the `NSdocs.Flusher`.
3. **Monitoring**: Add detailed monitoring and logging for Redis interactions and Flusher processing.
4. **Error Handling**: Implement robust error handling, potentially using Redis features for retries or dead-letter queues if applicable.
5. **Scalability Tuning**: Adjust replica counts and resource limits based on performance testing.
