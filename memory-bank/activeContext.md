# Active Context: NSdocs Document Consumption Module

## Current Focus
Refining Redis Aggregation Strategy to Support Consumption Record Creation.

## Phase Status

### Phase 1: RabbitMQ Cleanup ✅
1. **Remove RabbitMQ Dependencies** (Completed)
   - Deleted NSdocs.Worker project
   - Removed MassTransit packages from Infrastructure
   - Removed RabbitMQ service from docker-compose.yml
   - Cleaned up RabbitMQ configuration from appsettings.json

2. **Code Cleanup** (Completed)
   - Removed RabbitMQEventPublisher
   - Deleted RabbitMQ configurations
   - Updated dependency injection configuration

### Phase 2: Redis Implementation Setup ✅ 
*(Initial setup complete, key structure refinement needed)*
1. **Initial Redis Key Structure (Problematic for Creation)**
```
agg:company:{company_id}:quantity
agg:company:{company_id}:total
```
2. **Flusher Coordination Keys** (Still valid)
```
flushers:active             - Sorted Set: Active flusher instance IDs with heartbeat scores
agg:companies               - Set: Company IDs with *any* pending aggregation (May be deprecated/changed)
lock:workdistributor:redistribute - Lock for coordinating redistribution
lock:company:{id}:flush     - Lock for processing a specific company's simple aggregation (May change to base key lock)
```

### Phase 3: Redis Coordination Implementation ✅
*(Core coordination services are implemented but rely on the simple aggregation structure)*

1. **Lock Management** (Completed) - `RedisLockManager` implemented.
2. **Work Distribution** (Completed) - `WorkDistributor` implemented (assigns companies).
3. **Health Monitoring** (Completed) - `HealthMonitor` implemented (tracks instances).
4. **Flusher Updates** (Completed) - `FlushWorker` integrated with coordination services.
5. **Configuration & Deployment** (Completed) - Basic config, DI, Dockerfile, docker-compose scaling setup done.

### Phase 4: Redis Aggregation Refinement (Current)

**Problem Identified:**
- The current Redis aggregation keys (`agg:company:{id}:quantity/total`) lack dimensional data (`date`, `origin`, `type`, `status`).
- This prevents the `FlushWorker` from creating *new* `Consumption` records in the database, as it doesn't know which specific record dimensions the aggregated delta applies to.

**Proposed Solution: "Pending Flush Set" Approach**
1.  **Granular Aggregation Keys:** Use detailed keys in Redis that include all dimensions:
    `agg:company:{companyId}:{date}:{origin}:{type}:{status}:quantity`
    `agg:company:{companyId}:{date}:{origin}:{type}:{status}:total`
    (Note: `date` should be formatted consistently, e.g., `yyyy-MM-dd`. Enums need consistent string representation e.g., kebab-case).
2.  **Pending Set:** Maintain a Redis Set (e.g., `agg:pending_flush`) containing the *base keys* (without `quantity`/`total`, e.g., `agg:company:1:2025-03-28:file:nfe:ok`) that have pending updates.
3.  **`RedisEventPublisher` Update:** Modify to increment/decrement the granular keys and `SADD` the corresponding base key to `agg:pending_flush`.
4.  **`FlushWorker` Update:**
    *   Fetch base keys from `agg:pending_flush` (using `SPOP` or `SRANDMEMBER` + `SREM`).
    *   (Optional Filtering: Check if the company in the key is assigned to the current worker, or let any worker process any key).
    *   Lock the *base key* using `RedisLockManager`.
    *   Atomically get/reset the granular `quantity` and `total` keys.
    *   Parse dimensions from the base key.
    *   Find or Create the `Consumption` record in the DB using all dimensions.
    *   Apply deltas and save DB.
    *   If successful, remove the base key from `agg:pending_flush` (using `SREM`).
    *   Release lock.

**Updated Redis Keys (Target State):**
```
# Granular Aggregation (Strings)
agg:company:{companyId}:{date}:{origin}:{type}:{status}:quantity
agg:company:{companyId}:{date}:{origin}:{type}:{status}:total

# Pending Flush Tracking (Set)
agg:pending_flush           - Set: Members are base keys like "agg:company:..."

# Coordination Keys (Unchanged for now)
flushers:active             - Sorted Set: Instance IDs + Heartbeats
lock:workdistributor:redistribute - String: Lock for redistribution
lock:{base_key}:flush       - String: Lock for processing a specific base aggregation key
```

### Technical Considerations (Focus for Refinement)

1. **Performance:** "Pending Flush Set" avoids `SCAN`. Evaluate `SPOP` vs `SRANDMEMBER`/`SREM`.
2. **Atomicity:** Ensure atomicity of getting/resetting Redis counts, updating DB, and removing from `agg:pending_flush`. Locking the base key is crucial. Consider Lua scripts for multi-step Redis operations if needed.
3. **Key Parsing:** Implement robust parsing for dimensions from the base key string. Define delimiters clearly.
4. **Date/Enum Formatting:** Standardize formats used in keys (e.g., `yyyy-MM-dd`, kebab-case enums).
5. **Error Handling:** Refine revert logic. What happens if DB fails after `SPOP` but before `SREM`? The key might be lost. `SRANDMEMBER` + `SREM` might be safer.
6. **Work Distribution:** Decide if `FlushWorker` processes any key from `agg:pending_flush` or only those matching its assigned companies. Processing any key simplifies worker logic but relies entirely on the base key lock for coordination.

### Phase 5: Testing & Refinement (Post-Refinement)
*(This phase will commence after the aggregation strategy is refined)*

## Next Actions (Current Task: Prepare for Refinement)

1.  **Update Memory Bank (This step):**
    *   Reflect the need for aggregation refinement in `activeContext.md`. (Done)
    *   Update `progress.md` to list refinement tasks. (Next)
2.  **End Current Task:** Use `attempt_completion` to signal readiness for the user to create a new task focused on the refinement.

## Success Criteria (Unchanged)

### Functional Requirements
- Successful horizontal scaling
- Proper work distribution
- Reliable failover handling
- Consistent data processing (including creation of new records)

### Non-functional Requirements
- Sub-100ms processing (for individual flush operations)
- Zero data loss
- Automatic recovery
- Efficient resource usage
