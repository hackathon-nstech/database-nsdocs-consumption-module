# Event Flow Documentation: NSdocs Consumption Module

This document outlines the flow of document events (create, update, delete) through the NSdocs consumption system, focusing on how Redis is used for aggregation and how the `FlushWorker` updates the database.

## Overview

The system tracks document consumption counts based on several dimensions: Company ID, Date, Origin, Document Type, and Status. Instead of updating the database directly on every document change, the system uses Redis for intermediate aggregation to handle high throughput. A background worker (`FlushWorker`) periodically reads these aggregated values from Redis and flushes them to the `consumption` table in the database.

## Key Concepts

*   **Consumption Record:** A row in the `consumption` table representing the counts for a unique combination of `id_company`, `consumption_date`, `origin`, `document_type`, and `status`.
*   **Quantity Counter:** The `quantity` field in a `consumption` record. It represents the *current* number of documents matching that specific combination. It increases when a document enters this state and decreases when it leaves.
*   **Total Counter:** The `total` field in a `consumption` record. It represents the *cumulative* number of documents that have *ever* entered this specific state. It only increases and should never decrease.
*   **Redis Aggregation Keys:** Redis keys used to temporarily store increments/decrements before they are flushed to the database.
    *   Format: `agg:company:{companyId}:{date}:{origin}:{type}:{status}` (Base Key)
    *   Counters: `{BaseKey}:quantity`, `{BaseKey}:total`
*   **Pending Flush Set:** A Redis Set (`agg:pending_flush`) containing the Base Keys that have pending updates to be processed by the `FlushWorker`.
*   **Aggregation Period:** Currently based on the UTC date (`yyyy-MM-dd`) when the event is processed.

## Event Types and Behaviors

The `RedisEventPublisher` listens for document events and updates Redis counters accordingly.

### 1. Document Creation (`DocumentCreatedEvent`)

*   **Action:** A new document record is saved in the `documents` table.
*   **RedisEventPublisher:**
    *   Identifies the Base Key corresponding to the new document's state.
    *   Increments `{BaseKey}:quantity` by 1.
    *   Increments `{BaseKey}:total` by 1.
    *   Adds the Base Key to the `agg:pending_flush` set.
*   **FlushWorker:**
    *   Picks up the Base Key from the set.
    *   Reads and resets the Redis counters (+1, +1).
    *   Finds or creates the corresponding `consumption` record in the database.
    *   Applies the deltas: `quantity = quantity + 1`, `total = total + 1`.

### 2. Document Update (`DocumentUpdatedEvent`)

Updates are handled differently depending on whether the fields defining the consumption record (`CompanyId`, `Origin`, `Status`) have changed. `DocumentType` changes do not affect consumption counts in this model.

*   **Case A: No Change in `CompanyId`, `Origin`, or `Status`**
    *   **Action:** Document fields like `AccessKey` or `UpdatedDate` are modified.
    *   **RedisEventPublisher:** Detects no relevant change, performs no Redis operations.
    *   **FlushWorker:** No action related to this event.

*   **Case B: Change in `CompanyId`, `Origin`, or `Status`**
    *   **Action:** Document state changes (e.g., `status` from `pending` to `error`).
    *   **RedisEventPublisher:**
        *   **Old State:**
            *   Identifies the Base Key for the *previous* state.
            *   Decrements `{PreviousBaseKey}:quantity` by 1.
            *   Decrements `{PreviousBaseKey}:total` by 1. *(Correction: Total should likely NOT be decremented here, needs review)*
            *   Adds the Previous Base Key to `agg:pending_flush`.
        *   **New State:**
            *   Identifies the Base Key for the *new* state.
            *   Increments `{NewBaseKey}:quantity` by 1.
            *   Increments `{NewBaseKey}:total` by 1.
            *   Adds the New Base Key to `agg:pending_flush`.
    *   **FlushWorker:**
        *   Processes the Previous Base Key: Reads/resets Redis (-1, -1), updates DB `consumption` record (`quantity -= 1`, `total -= 1`). *(Correction needed for total)*
        *   Processes the New Base Key: Reads/resets Redis (+1, +1), updates DB `consumption` record (`quantity += 1`, `total += 1`).

### 3. Document Deletion (`DocumentDeletedEvent`)

*   **Action:** A document record is removed from the `documents` table.
*   **RedisEventPublisher:**
    *   Identifies the Base Key corresponding to the deleted document's state.
    *   Decrements `{BaseKey}:quantity` by 1.
    *   *Does not change* `{BaseKey}:total`.
    *   Adds the Base Key to the `agg:pending_flush` set.
*   **FlushWorker:**
    *   Picks up the Base Key from the set.
    *   Reads and resets the Redis counters (-1, 0).
    *   Finds the corresponding `consumption` record in the database.
    *   Applies the deltas: `quantity = quantity - 1`. `total` remains unchanged.

## Example Scenarios (Illustrative)

*(To be filled in with concrete examples)*

*   Create new document (Company 1, ws, nfe, ok)
*   Update document status (Company 1, ws, nfe, ok -> error)
*   Delete document (Company 1, ws, nfe, error)
*   Multiple updates (Company 2, file, cte, ok -> pending -> ok)

## Component Interaction

*(Sequence diagrams will be added in separate files)*

*   API Endpoint -> Command Handler -> DB Save -> Event Publisher
*   Event Publisher -> Redis (INCR/SADD)
*   FlushWorker -> Redis (SPOP/GETSET/SREM) -> DB Save (Update/Insert)

*(Note: Includes correction placeholders for the 'total' counter logic during updates, which needs further review based on business requirements.)*
