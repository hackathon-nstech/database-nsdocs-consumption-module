# Sequence Diagram: Document Create Flow

This diagram illustrates the sequence of events when a new document is created via the API.

```mermaid
sequenceDiagram
    participant Client
    participant APIEndpoint
    participant CreateCmdHandler
    participant AppDbContext
    participant RedisEventPublisher
    participant Redis
    participant FlushWorker

    Client->>+APIEndpoint: POST /api/documents (CreateDocumentCommand)
    APIEndpoint->>+CreateCmdHandler: Handle(command)
    CreateCmdHandler->>+AppDbContext: Create Document entity
    AppDbContext-->>-CreateCmdHandler: Document entity (with ID)
    CreateCmdHandler->>+AppDbContext: SaveChanges()
    AppDbContext-->>-CreateCmdHandler: Success
    CreateCmdHandler->>+RedisEventPublisher: PublishAsync(DocumentCreatedEvent)
    RedisEventPublisher->>+Redis: INCR {BaseKey}:quantity (by 1)
    Redis->>-RedisEventPublisher: OK
    RedisEventPublisher->>+Redis: INCR {BaseKey}:total (by 1)
    Redis->>-RedisEventPublisher: OK
    RedisEventPublisher->>+Redis: SADD agg:pending_flush {BaseKey}
    Redis->>-RedisEventPublisher: OK
    RedisEventPublisher-->>-CreateCmdHandler: Success
    CreateCmdHandler-->>-APIEndpoint: Success (Document ID)
    APIEndpoint-->>-Client: 201 Created (Document ID)

    Note over FlushWorker, Redis: FlushWorker runs periodically

    FlushWorker->>+Redis: SPOP agg:pending_flush
    Redis-->>-FlushWorker: {BaseKey}
    alt BaseKey found
        FlushWorker->>+Redis: GETSET {BaseKey}:quantity "0"
        Redis-->>-FlushWorker: "1" (deltaQuantity)
        FlushWorker->>+Redis: GETSET {BaseKey}:total "0"
        Redis-->>-FlushWorker: "1" (deltaTotal)
        FlushWorker->>+AppDbContext: Find or Create Consumption record (using dimensions from BaseKey)
        AppDbContext-->>-FlushWorker: Consumption record
        FlushWorker->>AppDbContext: Update record (quantity+=deltaQuantity, total+=deltaTotal)
        FlushWorker->>+AppDbContext: SaveChanges()
        AppDbContext-->>-FlushWorker: Success
        FlushWorker->>+Redis: SREM agg:pending_flush {BaseKey} (Only if DB succeeded)
        Redis-->>-FlushWorker: OK
    end
```

**Key:**

*   `{BaseKey}`: Represents the specific aggregation key, e.g., `agg:company:123:2025-03-28:ws:nfe:ok`
