# Redis Key Structure: NSdocs Consumption Module

This document details the structure and purpose of Redis keys used by the NSdocs consumption aggregation system.

## 1. Aggregation Keys

These keys store the temporary counts (deltas) for document consumption before they are flushed to the database.

### Base Key Format

The core identifier for a specific consumption category.

```
agg:company:{companyId}:{date}:{origin}:{type}:{status}
```

*   `agg:company:`: Namespace prefix.
*   `{companyId}`: The integer ID of the company.
*   `{date}`: The UTC date of consumption in `yyyy-MM-dd` format.
*   `{origin}`: The document origin (e.g., `file`, `email`, `ws`). Enum values are stored in lowercase or kebab-case if multi-word.
*   `{type}`: The document type (e.g., `nfe`, `cte`, `nfce`). Enum values are stored in lowercase or kebab-case.
*   `{status}`: The document status (e.g., `ok`, `pending`, `error`). Enum values are stored in lowercase or kebab-case.

**Example:** `agg:company:227:2025-03-28:ws:nfe:ok`

### Counter Keys (Type: String)

Derived from the Base Key, these store the actual delta values.

*   **Quantity:** `{BaseKey}:quantity`
    *   Stores the net change (+/-) in the number of documents currently matching the Base Key's state since the last flush.
    *   Incremented (+1) on create/update-new.
    *   Decremented (-1) on delete/update-old.
    *   Value is read and reset to 0 by the `FlushWorker`.
    *   **Example:** `agg:company:227:2025-03-28:ws:nfe:ok:quantity`

*   **Total:** `{BaseKey}:total`
    *   Stores the net change (+/-) in the cumulative count of documents that have *ever* entered the state defined by the Base Key since the last flush.
    *   Incremented (+1) on create/update-new.
    *   *(Review Needed)* Currently decremented (-1) on update-old. Should likely *not* be decremented.
    *   *Not* changed on delete.
    *   Value is read and reset to 0 by the `FlushWorker`.
    *   **Example:** `agg:company:227:2025-03-28:ws:nfe:ok:total`

## 2. Pending Flush Tracking Key (Type: Set)

Tracks which Base Keys have pending updates that need to be processed by the `FlushWorker`.

*   **Key:** `agg:pending_flush`
*   **Members:** Base Keys (e.g., `agg:company:227:2025-03-28:ws:nfe:ok`)
*   **Usage:**
    *   `RedisEventPublisher` adds a Base Key to this set (`SADD`) whenever it increments/decrements the corresponding counter keys.
    *   `FlushWorker` retrieves members from this set (e.g., using `SPOP` or `SRANDMEMBER`) to find work.
    *   `FlushWorker` removes a Base Key (`SREM`) *after* successfully flushing its data to the database.

## 3. Coordination Keys (Used by FlushWorker/Infrastructure)

These keys are used for managing the distributed `FlushWorker` instances.

*   **Active Flushers (Type: Sorted Set):** `flushers:active`
    *   **Members:** Unique instance IDs of running `FlushWorker` instances.
    *   **Score:** Timestamp of the last heartbeat from the instance. Used to detect stale/dead instances.

*   **Base Key Flush Lock (Type: String):** `lock:{base_key}:flush`
    *   **Purpose:** Ensures only one `FlushWorker` instance processes a specific Base Key at a time.
    *   **Value:** The instance ID holding the lock.
    *   **TTL:** Set to prevent deadlocks if a worker crashes while holding the lock.
    *   **Example:** `lock:agg:company:227:2025-03-28:ws:nfe:ok:flush`

*   **(Deprecated/Removed) Work Distributor Lock:** `lock:workdistributor:redistribute`
    *   Previously used for coordinating the distribution of work (companies) among flushers. Not used in the current "Pending Flush Set" model where any worker can process any key.

*   **(Deprecated/Removed) Company Flush Lock:** `lock:company:{id}:flush`
    *   Previously used to lock processing for an entire company. Replaced by the more granular `lock:{base_key}:flush`.

*(Note: Includes correction placeholders for the 'total' counter logic during updates.)*
