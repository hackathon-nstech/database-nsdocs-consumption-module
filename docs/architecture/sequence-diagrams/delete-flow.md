# Sequence Diagram: Document Delete Flow

This diagram illustrates the sequence of events when an existing document is deleted via the API.

```mermaid
sequenceDiagram
    participant Client
    participant APIEndpoint
    participant DeleteCmdHandler
    participant AppDbContext
    participant RedisEventPublisher
    participant Redis
    participant FlushWorker

    Client->>+APIEndpoint: DELETE /api/documents/{id}
    APIEndpoint->>+DeleteCmdHandler: Handle(DeleteDocumentCommand(id))
    DeleteCmdHandler->>+AppDbContext: Find Document entity by ID
    AppDbContext-->>-DeleteCmdHandler: Document entity (State before delete)
    alt Document Found
        DeleteCmdHandler->>+AppDbContext: Remove Document entity
        AppDbContext-->>-DeleteCmdHandler: Entity marked for deletion
        DeleteCmdHandler->>+AppDbContext: SaveChanges()
        AppDbContext-->>-DeleteCmdHandler: Success (DB delete confirmed)
        DeleteCmdHandler->>+RedisEventPublisher: PublishAsync(DocumentDeletedEvent(State before delete))

        Note right of RedisEventPublisher: Processing Deleted State
        RedisEventPublisher->>+Redis: INCR {BaseKey}:quantity (by -1)
        Redis->>-RedisEventPublisher: OK
        RedisEventPublisher->>+Redis: SADD agg:pending_flush {BaseKey}
        Redis->>-RedisEventPublisher: OK

        RedisEventPublisher-->>-DeleteCmdHandler: Success
        DeleteCmdHandler-->>-APIEndpoint: Success (True)
        APIEndpoint-->>-Client: 204 No Content
    else Document Not Found
        DeleteCmdHandler-->>-APIEndpoint: Failure (False)
        APIEndpoint-->>-Client: 404 Not Found
    end

    Note over FlushWorker, Redis: FlushWorker runs periodically

    FlushWorker->>+Redis: SPOP agg:pending_flush
    Redis-->>-FlushWorker: {BaseKey}
    alt BaseKey found (Matches the deleted document's state)
        FlushWorker->>+Redis: GETSET {BaseKey}:quantity "0"
        Redis-->>-FlushWorker: "-1" (deltaQuantity)
        FlushWorker->>+Redis: GETSET {BaseKey}:total "0"
        Redis-->>-FlushWorker: "0" (deltaTotal - total is not changed on delete)
        FlushWorker->>+AppDbContext: Find Consumption record (using dimensions from BaseKey)
        AppDbContext-->>-FlushWorker: Consumption record
        alt Consumption Record Found
             FlushWorker->>AppDbContext: Update record (quantity+=deltaQuantity)
             FlushWorker->>+AppDbContext: SaveChanges()
             AppDbContext-->>-FlushWorker: Success
             FlushWorker->>+Redis: SREM agg:pending_flush {BaseKey}
             Redis-->>-FlushWorker: OK
        else Consumption Record Not Found (Should not happen if created correctly)
             FlushWorker->>FlushWorker: Log Warning: Consumption record not found for delete flush.
             FlushWorker->>+Redis: SREM agg:pending_flush {BaseKey}  // Remove key to prevent reprocessing
             Redis-->>-FlushWorker: OK
        end
    end

```

**Key:**

*   `{BaseKey}`: Represents the specific aggregation key for the document's state *before* it was deleted.
