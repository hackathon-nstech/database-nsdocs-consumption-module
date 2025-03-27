# Active Context: NSdocs Document Consumption Module

## Current Focus
Converting MySQL triggers to Redis-based event aggregation with correct consumption tracking

## Implementation Updates

### Phase 1: RabbitMQ Cleanup
1. **Remove RabbitMQ Dependencies**
   - Delete NSdocs.Worker project
   - Remove MassTransit packages from Infrastructure
   - Remove RabbitMQ service from docker-compose.yml
   - Clean up RabbitMQ configuration from appsettings.json

2. **Code Cleanup**
   - Remove RabbitMQEventPublisher
   - Delete RabbitMQ configurations
   - Update dependency injection configuration

### Phase 2: Consumption Logic Correction
1. **Redis Key Structure Update**
```
agg:company:{company_id}:{date}:{origin}:{type}:{status}:quantity
agg:company:{company_id}:{date}:{origin}:{type}:{status}:total
```

2. **Event Publishing Changes**
   - Track full document context
   - Partition by request date
   - Handle status changes correctly
   - Apply proper total increment rules

3. **Flusher Service Updates**
   - Group updates by unique key components
   - Implement correct total field logic
   - Handle date partitioning
   - Add database consistency checks

### Phase 3: Data Consistency
1. **Total Field Logic**
```sql
total = total + if(quantity > 0, 1, 0)
```
- Only increment total on quantity increases
- Maintain total on quantity decreases
- Group by unique key components

2. **Date Partitioning**
```sql
WHERE request_date >= consumption_date
  AND request_date < date_add(consumption_date, interval 1 day)
```
- Implement date-based grouping
- Handle date boundaries correctly

3. **Unique Key Handling**
```sql
(id_company, consumption_date, origin, document_type, status)
```
- Ensure all components are considered
- Handle concurrent updates properly
- Maintain data integrity

## Action Items

1. **Immediate Tasks**
   - Remove RabbitMQ components
   - Implement new Redis key structure
   - Update event publishing logic

2. **Technical Updates**
   - Enhance Flusher service
   - Implement date partitioning
   - Add data consistency checks

3. **Testing & Validation**
   - Verify total calculation logic
   - Test date boundary cases
   - Validate unique key constraints

## Success Criteria

### Technical Validation
- Accurate consumption tracking
- Correct total field updates
- Proper date partitioning
- Data consistency maintenance
- Scalable event processing

### Business Requirements
- Match original trigger logic
- Maintain data accuracy
- Support concurrent operations
- Enable system scalability
