# Troubleshooting: NSdocs Consumption Module

This document provides guidance on diagnosing and resolving common issues with the NSdocs consumption module.

## Common Issues & Solutions

### 1. Consumption Counts Incorrect / Not Updating

*   **Symptom:** Database `consumption` table values do not match expected counts based on `documents` table or recent activity. Counts might be stuck or lagging significantly.
*   **Possible Causes & Checks:**
    *   **`FlushWorker` Not Running/Crashing:**
        *   Check if `FlushWorker` instances are running (Docker/Kubernetes).
        *   Check `flushers:active` set in Redis for active instances and recent heartbeats.
        *   Check `FlushWorker` logs for startup errors or continuous crash loops.
    *   **`agg:pending_flush` Set Growing:**
        *   Monitor `SCARD agg:pending_flush`. If it's large and growing, workers can't keep up.
        *   **Reason:** High event rate, slow DB updates, slow Redis, insufficient worker instances, worker errors.
        *   Check `FlushWorker` logs for errors during processing (DB connection, query errors, Redis errors, parsing errors).
        *   Check DB performance (slow queries on `consumption` table).
        *   Check Redis performance (latency).
        *   Consider scaling up `FlushWorker` instances or increasing DB/Redis resources.
    *   **Events Not Being Published:**
        *   Check API logs for errors during `RedisEventPublisher.PublishAsync`.
        *   Verify Redis connection from the API instances.
        *   Ensure `DocumentCreated/Updated/DeletedEvent` handlers are correctly calling the publisher.
    *   **Redis Key Parsing Errors:**
        *   Check `FlushWorker` logs for "Failed to parse base key" errors. This indicates an issue with how keys are constructed by the publisher or parsed by the worker (e.g., unexpected characters, incorrect enum formatting). Correct the `BuildBaseKey` or `ParseBaseKey` logic.
    *   **Database Errors During Flush:**
        *   Check `FlushWorker` logs for errors during `SaveChangesAsync` (e.g., constraint violations, connection issues, timeouts).
        *   If DB errors occur, the worker *should* ideally not remove the key from `agg:pending_flush`, allowing for retry. Verify this behavior.
    *   **Locking Issues:**
        *   Check `FlushWorker` logs for errors acquiring or releasing locks (`lock:{base_key}:flush`). Persistent failures might indicate very long processing times (exceeding lock TTL) or clock skew issues. Investigate the cause of long processing or adjust lock TTL carefully.

### 2. High Resource Usage (CPU/Memory)

*   **Symptom:** `FlushWorker` instances or Redis/Database consuming excessive resources.
*   **Possible Causes & Checks:**
    *   **Inefficient `FlushWorker` Processing:**
        *   Profile the `FlushWorker` code. Are DB queries efficient? Is Redis interaction optimal? Is key parsing fast?
        *   Check `maxProcessPerCycle` in `FlushWorker` - if too high, a single worker might try to do too much at once.
    *   **High Event Rate:** If the rate of document changes is very high, the system might naturally require more resources. Consider scaling workers, Redis, and DB.
    *   **Redis Bottleneck:** Check Redis CPU/Memory. Consider scaling Redis or optimizing key structures/commands if applicable.
    *   **Database Bottleneck:** Check DB CPU/Memory/IO. Ensure proper indexing on the `consumption` table (especially the unique key). Optimize DB queries if needed.

### 3. Data Loss / Missed Updates

*   **Symptom:** Events seem to be published but never reflected in the database counts.
*   **Possible Causes & Checks:**
    *   **Key Lost During Flush Error:** If `FlushWorker` removes a key from `agg:pending_flush` *before* successfully saving to DB (or if an error occurs after SPOP but before SREM in some scenarios), the update might be lost. Review the `FlushWorker` error handling and key removal logic (`SREM` should only happen *after* successful DB save). Using `SRANDMEMBER` + `SREM` (after success) is generally safer than `SPOP`.
    *   **Publisher Errors:** If `RedisEventPublisher.PublishAsync` fails and doesn't retry or log properly, events might be lost. Ensure robust error handling in the publisher.
    *   **Redis Data Eviction:** If Redis runs out of memory and keys are evicted, aggregation data could be lost. Ensure Redis has sufficient memory and appropriate eviction policies (e.g., `noeviction` might be safer for this data if memory is sufficient).

## Diagnostic Steps

1.  **Check Logs:** Start by examining logs from the API (`RedisEventPublisher`) and `FlushWorker` instances around the time the issue occurred. Look for errors, warnings, or unusual patterns.
2.  **Check Redis State:**
    *   `SCARD agg:pending_flush`: How many keys are waiting? Is it growing?
    *   `SRANDMEMBER agg:pending_flush`: Look at examples of pending keys. Do they look correct?
    *   `GET {BaseKey}:quantity` / `GET {BaseKey}:total`: Check the delta values for specific keys if needed.
    *   `ZSCAN flushers:active 0`: Check active workers and heartbeats.
3.  **Check Database:**
    *   Query the `consumption` table directly for the relevant `id_company`, `consumption_date`, etc.
    *   Check database logs for errors or slow queries related to the `consumption` table.
4.  **Reproduce:** If possible, try to reproduce the issue with specific API calls (Create/Update/Delete) while monitoring logs and Redis state.
5.  **Trace:** Use distributed tracing if available to follow a specific event through the system.
