# Aggregation Logic: NSdocs Consumption Module

This document explains the logic behind maintaining the `quantity` and `total` counters in the `consumption` table.

## Core Principle

The `consumption` table provides a snapshot of document counts aggregated by `id_company`, `consumption_date`, `origin`, `document_type`, and `status`. Each unique combination of these dimensions has its own row.

*   **`quantity`:** Represents the *current* number of documents matching the row's dimensions *at the time of the last flush*.
*   **`total`:** Represents the *cumulative* count of documents that have *ever* matched the row's dimensions.

## Logic Implementation

The logic is implemented through the interaction of the `RedisEventPublisher` and the `FlushWorker`.

1.  **Event Publishing (`RedisEventPublisher`):**
    *   When a document event occurs (Create, Update, Delete), the publisher calculates the necessary changes (deltas) for `quantity` and `total` based on the event type and state changes.
    *   These deltas are applied atomically to corresponding counter keys in Redis (e.g., `agg:...:quantity`, `agg:...:total`).
    *   The base key (e.g., `agg:...`) is added to the `agg:pending_flush` set.

2.  **Flushing (`FlushWorker`):**
    *   Periodically, the worker retrieves a base key from the `agg:pending_flush` set.
    *   It atomically reads the current delta values from the Redis counter keys (`{BaseKey}:quantity`, `{BaseKey}:total`) and resets them to 0.
    *   It finds the corresponding row in the `consumption` table based on the dimensions parsed from the base key.
        *   If the row exists, it updates the `quantity` and `total` columns by adding the retrieved deltas.
        *   If the row does not exist (e.g., first document for this combination), it creates a new row with the `quantity` and `total` set to the retrieved deltas.
    *   It saves the changes to the database.
    *   If the database save is successful, it removes the base key from the `agg:pending_flush` set.

## Counter Behavior Summary

| Event             | State Change Relevant? | Redis `quantity` Delta | Redis `total` Delta | DB `quantity` Change | DB `total` Change | Notes                                                                 |
| :---------------- | :--------------------- | :--------------------- | :------------------ | :------------------- | :---------------- | :-------------------------------------------------------------------- |
| **Create**        | N/A                    | +1                     | +1                  | +1                   | +1                | Increments both current count and cumulative count.                   |
| **Update**        | No                     | 0                      | 0                   | 0                    | 0                 | No change if `CompanyId`, `Origin`, `Status` remain the same.         |
| **Update (Old)**  | Yes                    | -1                     | -1                  | -1                   | -1                | Decrements counts when document leaves this state.                     |
| **Update (New)**  | Yes                    | +1                     | +1                  | +1                   | +1                | Increments counts when document enters this state.                     |
| **Delete**        | N/A                    | -1                     | 0                   | -1                   | 0                 | Decrements current count only. Historical total preserved.             |

**Note on `total` Behavior:** 
- During updates, the `total` counter tracks state transitions accurately by decrementing when a document leaves a state and incrementing when it enters a new state.
- During deletes, the `total` counter is preserved to maintain historical records of how many documents have passed through each state.
- This implementation ensures accurate tracking of both current state (`quantity`) and state transition history (`total`).
