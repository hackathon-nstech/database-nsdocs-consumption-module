
# Redis-Only Aggregation Architecture (No Lua Scripts)

## Overview

This architecture enables lightweight, horizontally scalable processing of document changes that affect a shared per-company **consumption table**. It eliminates RabbitMQ and background workers by using **direct Redis writes from the API** and a **single flusher service** that periodically applies the aggregated values to the database. It avoids Lua scripting and uses only standard Redis operations.

---

## Components

### 1. API Service
On each document change (`create`, `update`, `delete`), the API calculates the `delta` and performs the following Redis operations:

#### a. Increment company delta:
```csharp
INCRBY agg:company:{company_id} {delta}
```

#### b. Register company in active set:
```csharp
SADD agg:companies {company_id}
```

This ensures the flusher knows which companies have pending updates.

---

## 2. Redis Aggregation Store

- **Key:** `agg:company:{company_id}`  
  Holds the current delta (e.g., `+10`, `-3`)

- **Key:** `agg:companies`  
  A Redis Set containing all company IDs with pending deltas

This design avoids key scanning (which can be expensive) and allows safe, efficient delta reads.

---

## 3. Flusher Service

A separate process or cron-like service that runs periodically (e.g., every 1–5 seconds):

### Steps:
1. **Read all active company IDs:**
   ```csharp
   var companyIds = redis.SMEMBERS("agg:companies");
   ```

2. **For each company:**
   - Atomically **get and reset** the delta with `GETSET`:
     ```csharp
     var delta = redis.GETSET("agg:company:{company_id}", 0);
     ```
   - If `delta == 0`, skip.
   - Apply delta to the database:
     ```sql
     UPDATE consumption SET value = value + @delta WHERE company_id = @id;
     ```

3. **Cleanup:**
   - After successful DB update, remove company ID from the set:
     ```csharp
     SREM agg:companies {company_id}
     ```

---

## Database

- **Table:** `consumption`
- One row per `company_id`
- SQL update is simple, fast, and atomic:
  ```sql
  UPDATE consumption SET value = value + @delta WHERE company_id = @id;
  ```

---

## Observability

- Metrics to track:
  - Number of companies flushed per cycle
  - Total delta applied
  - Redis key count in `agg:companies`
  - Flush failures (e.g., DB errors)

---

## Resilience and Safety

- **No message queue needed**
- **No Lua scripts** — all Redis commands are native and safe
- If the flusher or API crashes:
  - Redis still holds the state (`agg:company:*` and `agg:companies`)
  - No data is lost until flushed
- Flusher can retry failed company updates without risk of duplication

---

## Optional Enhancements

- Use `EXPIRE` on `agg:company:{company_id}` keys (e.g., 24h) to auto-clean stale entries
- Use a sorted set (`ZADD`) with timestamps instead of `SADD` for time-based flushing
