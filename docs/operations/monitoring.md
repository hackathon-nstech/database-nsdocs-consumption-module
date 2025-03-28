# Monitoring: NSdocs Consumption Module

This document outlines key aspects to monitor for the health and performance of the NSdocs consumption module, particularly the `FlushWorker` and its interaction with Redis and the database.

## Key Metrics & Indicators

### 1. FlushWorker Health

*   **Number of Running Instances:** Monitor the count of active `FlushWorker` instances. This should match the expected number based on deployment configuration (e.g., `docker-compose scale`). Unexpected drops indicate crashes or deployment issues.
    *   **Source:** Docker/Kubernetes monitoring, `flushers:active` Sorted Set size in Redis.
*   **Heartbeats:** Check the scores (timestamps) in the `flushers:active` Sorted Set. Stale timestamps indicate unresponsive or dead instances.
    *   **Source:** Redis (`ZSCAN flushers:active`).
*   **Error Logs:** Monitor `FlushWorker` logs for exceptions, especially during Redis operations (connection, locking, commands) and database updates. High error rates indicate potential problems.
    *   **Source:** Application logs (stdout/stderr, file, or centralized logging system).
*   **CPU/Memory Usage:** Track resource consumption of `FlushWorker` instances. Sustained high usage might indicate performance bottlenecks or inefficient processing.
    *   **Source:** Docker/Kubernetes metrics, host monitoring.

### 2. Redis Performance & State

*   **`agg:pending_flush` Set Size:** Monitor the number of keys waiting to be flushed. A continuously growing size indicates the `FlushWorker` cannot keep up with the rate of incoming events. Sudden drops to zero might indicate a stopped publisher or worker.
    *   **Source:** Redis (`SCARD agg:pending_flush`).
*   **Redis Latency:** Monitor command latency for `INCR`, `SADD`, `SPOP`, `GETSET`, `SREM`. High latency impacts both the API (event publishing) and the `FlushWorker`.
    *   **Source:** Redis monitoring tools (`redis-cli --latency`, cloud provider metrics).
*   **Redis CPU/Memory Usage:** Ensure the Redis instance is not resource-constrained.
    *   **Source:** Redis monitoring tools (`INFO` command), cloud provider metrics.
*   **Redis Connections:** Monitor the number of connected clients. Ensure it stays within limits.
    *   **Source:** Redis (`INFO clients`).
*   **Lock Contention/Errors:** Monitor logs for failures to acquire locks (`lock:{base_key}:flush`). Frequent contention might suggest the flush interval is too short or too many workers are competing for the same keys.
    *   **Source:** `FlushWorker` application logs.

### 3. Database Performance

*   **DB Query Latency:** Monitor the latency of `INSERT` and `UPDATE` statements on the `consumption` table executed by the `FlushWorker`. High latency slows down the flushing process.
    *   **Source:** Database performance monitoring tools (e.g., MySQL Slow Query Log, Performance Schema).
*   **DB Connections:** Ensure the database connection pool used by the `FlushWorker` is adequately sized and not exhausted.
    *   **Source:** Database monitoring, application metrics.
*   **DB CPU/Memory/IO:** Monitor overall database health.
    *   **Source:** Database/host monitoring.

### 4. End-to-End Latency

*   **Event Processing Lag:** Measure the time difference between an event being published (`RedisEventPublisher`) and the corresponding data being flushed to the database (`FlushWorker`). This can be estimated by comparing timestamps in logs or by observing the age of keys in `agg:pending_flush`. High lag indicates a bottleneck in the flushing process.
    *   **Source:** Application logs, potentially custom metrics.

## Monitoring Tools & Approaches

*   **Logging:** Configure structured logging (e.g., JSON) and ship logs to a centralized system (e.g., ELK stack, Grafana Loki, Datadog, Splunk) for analysis and alerting.
*   **Metrics:** Expose application metrics (e.g., using Prometheus client libraries) for queue sizes, processing times, error counts, etc. Visualize these in dashboards (e.g., Grafana, Datadog).
*   **Redis Monitoring:** Utilize Redis built-in commands (`INFO`, `SLOWLOG`) and cloud provider monitoring dashboards.
*   **Database Monitoring:** Use database-specific tools and cloud provider dashboards.
*   **Distributed Tracing:** Implement tracing (e.g., OpenTelemetry) to follow requests from the API through the event publisher, Redis, and the `FlushWorker` to pinpoint bottlenecks.
*   **Health Checks:** Implement health check endpoints in the API and potentially the `FlushWorker` (if accessible) that verify connectivity to Redis and the database.

## Alerting Thresholds (Examples)

*   `agg:pending_flush` size > X for Y minutes.
*   Number of active `FlushWorker` instances < expected count.
*   `FlushWorker` error rate > Z%.
*   Redis command latency > A ms.
*   DB query latency > B ms.
*   Event processing lag > C minutes.
