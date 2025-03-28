# Sequence Diagram: Document Update Flow (with Changes)

This diagram illustrates the sequence of events when an existing document is updated via the API, and the update involves changes to `CompanyId`, `Origin`, or `Status`.

```mermaid
sequenceDiagram
    participant Client
    participant APIEndpoint
    participant UpdateCmdHandler
    participant AppDbContext
    participant RedisEventPublisher
    participant Redis
    participant FlushWorker

    Client->>+APIEndpoint: PUT /api/documents/{id} (UpdateDocumentCommand)
    APIEndpoint->>+UpdateCmdHandler: Handle(command)
    UpdateCmdHandler->>+AppDbContext: Find Document entity by ID
    AppDbContext-->>-UpdateCmdHandler: Document entity (Old State)
    UpdateCmdHandler->>AppDbContext: Update Document entity fields (New State)
    UpdateCmdHandler->>+AppDbContext: SaveChanges()
    AppDbContext-->>-UpdateCmdHandler: Success
    UpdateCmdHandler->>+RedisEventPublisher: PublishAsync(DocumentUpdatedEvent(OldState, NewState))

    Note right of RedisEventPublisher: Processing Old State
    RedisEventPublisher->>+Redis: INCR {PreviousBaseKey}:quantity (by -1)
    Redis->>-RedisEventPublisher: OK
    RedisEventPublisher->>+Redis: INCR {PreviousBaseKey}:total (by -1)
    Redis->>-RedisEventPublisher: OK
    RedisEventPublisher->>+Redis: SADD agg:pending_flush {PreviousBaseKey}
    Redis->>-RedisEventPublisher: OK

    Note right of RedisEventPublisher: Processing New State
    RedisEventPublisher->>+Redis: INCR {NewBaseKey}:quantity (by 1)
    Redis->>-RedisEventPublisher: OK
    RedisEventPublisher->>+Redis: INCR {NewBaseKey}:total (by 1)
    Redis->>-RedisEventPublisher: OK
    RedisEventPublisher->>+Redis: SADD agg:pending_flush {NewBaseKey}
    Redis->>-RedisEventPublisher: OK

    RedisEventPublisher-->>-UpdateCmdHandler: Success
    UpdateCmdHandler-->>-APIEndpoint: Success (True)
    APIEndpoint-->>-Client: 200 OK / 204 No Content

    Note over FlushWorker, Redis: FlushWorker runs periodically

    loop Process Pending Keys
        FlushWorker->>+Redis: SPOP agg:pending_flush
        Redis-->>-FlushWorker: {BaseKeyToProcess}
        alt {BaseKeyToProcess} is {PreviousBaseKey}
            FlushWorker->>+Redis: GETSET {PreviousBaseKey}:quantity "0"
            Redis-->>-FlushWorker: "-1" (deltaQuantity)
            FlushWorker->>+Redis: GETSET {PreviousBaseKey}:total "0"
            Redis-->>-FlushWorker: "-1" (deltaTotal)
            FlushWorker->>+AppDbContext: Find Consumption record (using dimensions from PreviousBaseKey)
            AppDbContext-->>-FlushWorker: Consumption record
            FlushWorker->>AppDbContext: Update record (quantity+=deltaQuantity, total+=deltaTotal)
            FlushWorker->>+AppDbContext: SaveChanges()
            AppDbContext-->>-FlushWorker: Success
            FlushWorker->>+Redis: SREM agg:pending_flush {PreviousBaseKey}
            Redis-->>-FlushWorker: OK
        else {BaseKeyToProcess} is {NewBaseKey}
            FlushWorker->>+Redis: GETSET {NewBaseKey}:quantity "0"
            Redis-->>-FlushWorker: "1" (deltaQuantity)
            FlushWorker->>+Redis: GETSET {NewBaseKey}:total "0"
            Redis-->>-FlushWorker: "1" (deltaTotal)
            FlushWorker->>+AppDbContext: Find or Create Consumption record (using dimensions from NewBaseKey)
            AppDbContext-->>-FlushWorker: Consumption record
            FlushWorker->>AppDbContext: Update record (quantity+=deltaQuantity, total+=deltaTotal)
            FlushWorker->>+AppDbContext: SaveChanges()
            AppDbContext-->>-FlushWorker: Success
            FlushWorker->>+Redis: SREM agg:pending_flush {NewBaseKey}
            Redis-->>-FlushWorker: OK
        end
    end

```

**Key:**

*   `{PreviousBaseKey}`: Represents the aggregation key for the document's state *before* the update.
*   `{NewBaseKey}`: Represents the aggregation key for the document's state *after* the update.
*   Note: The total counter is decremented for the old state and incremented for the new state during updates, tracking state transitions accurately.
