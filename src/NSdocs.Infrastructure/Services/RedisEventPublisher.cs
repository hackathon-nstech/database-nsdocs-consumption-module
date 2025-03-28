using Microsoft.Extensions.Logging;
using NSdocs.Application.Common.Interfaces;
using NSdocs.Application.Documents.Events;
using System.Text;
using NSdocs.Infrastructure.Configuration;
using StackExchange.Redis;

namespace NSdocs.Infrastructure.Services;

public class RedisEventPublisher : IEventPublisher
{
    private readonly ILogger<RedisEventPublisher> _logger;
    private readonly IDatabase _redis;

    public RedisEventPublisher(
        IRedisConnectionFactory redisFactory,
        ILogger<RedisEventPublisher> logger)
    {
        _redis = redisFactory.GetDatabase();
        _logger = logger;
    }

    public async Task PublishAsync<TEvent>(TEvent @event, CancellationToken cancellationToken = default) where TEvent : class
    {
        DocumentEventBase? documentEvent = null;
        try
        {
            if (@event is not DocumentEventBase docEvent)
            {
                _logger.LogWarning("Event type {EventType} is not a DocumentEventBase", typeof(TEvent).Name);
                return;
            }

            documentEvent = docEvent;

            // Calculate deltas based on event type
            var (initialQuantity, initialTotal) = GetDeltas(docEvent); // Renamed for clarity

            // If it's not an update event and deltas are zero, no impact.
            if (initialQuantity == 0 && initialTotal == 0 && docEvent is not DocumentUpdatedEvent)
            {
                _logger.LogInformation("Event type {EventType} has no consumption impact", typeof(TEvent).Name);
                return;
            }

            var tasks = new List<Task>();
            var consumptionDate = DateTime.UtcNow.ToString("yyyy-MM-dd"); // Use current date for consumption period

            // --- Handle Current State (Create, Update-New, Delete) ---
            var currentBaseKey = BuildBaseKey(
                documentEvent.CompanyId,
                consumptionDate,
                documentEvent.Origin,
                documentEvent.DocumentType,
                documentEvent.Status);

            // Apply deltas based on event type
            if (docEvent is DocumentCreatedEvent)
            {
                tasks.Add(_redis.StringIncrementAsync($"{currentBaseKey}:quantity", 1));
                tasks.Add(_redis.StringIncrementAsync($"{currentBaseKey}:total", 1));
                tasks.Add(_redis.SetAddAsync("agg:pending_flush", currentBaseKey));
                _logger.LogInformation("Publishing CREATE deltas (+1, +1) for {BaseKey}", currentBaseKey);
            }
            else if (docEvent is DocumentDeletedEvent)
            {
                // Only decrement quantity, total remains unchanged on delete
                tasks.Add(_redis.StringIncrementAsync($"{currentBaseKey}:quantity", -1));
                // tasks.Add(_redis.StringIncrementAsync($"{currentBaseKey}:total", 0)); // No change to total
                tasks.Add(_redis.SetAddAsync("agg:pending_flush", currentBaseKey));
                 _logger.LogInformation("Publishing DELETE deltas (Quantity: -1, Total: 0) for {BaseKey}", currentBaseKey);
            }
            else if (docEvent is DocumentUpdatedEvent updateEvent)
            {
                 // Check if relevant fields actually changed
                 bool companyChanged = updateEvent.PreviousCompanyId != updateEvent.CompanyId;
                 bool originChanged = updateEvent.PreviousOrigin != updateEvent.Origin;
                 bool statusChanged = updateEvent.PreviousStatus != updateEvent.Status;
                 bool changed = companyChanged || originChanged || statusChanged;

                 if (changed)
                 {
                     // --- Handle Previous State ---
                     // Build the key based on the *previous* values of the changed fields
                     // and the *current* values of the unchanged fields.
                     var previousKeyCompany = companyChanged ? updateEvent.PreviousCompanyId : updateEvent.CompanyId;
                     var previousKeyOrigin = originChanged ? updateEvent.PreviousOrigin : updateEvent.Origin;
                     var previousKeyStatus = statusChanged ? updateEvent.PreviousStatus : updateEvent.Status;

                     var previousBaseKey = BuildBaseKey(
                         previousKeyCompany,
                         consumptionDate,
                         previousKeyOrigin,
                         updateEvent.DocumentType, // DocType doesn't change consumption grouping
                         previousKeyStatus);

                     // Decrement for the OLD state combination
                     tasks.Add(_redis.StringIncrementAsync($"{previousBaseKey}:quantity", -1));
                     tasks.Add(_redis.StringIncrementAsync($"{previousBaseKey}:total", -1)); // Also decrement total for the old record
                     tasks.Add(_redis.SetAddAsync("agg:pending_flush", previousBaseKey));
                     _logger.LogInformation("Publishing UPDATE-OLD deltas (-1, -1) for {BaseKey}", previousBaseKey);


                     // --- Handle New State ---
                     // Build the key based on the *current* values of all fields.
                     // Note: currentBaseKey is already calculated above based on documentEvent (new state)
                     tasks.Add(_redis.StringIncrementAsync($"{currentBaseKey}:quantity", 1));
                     tasks.Add(_redis.StringIncrementAsync($"{currentBaseKey}:total", 1)); // Increment total for the new state
                     tasks.Add(_redis.SetAddAsync("agg:pending_flush", currentBaseKey));
                     _logger.LogInformation("Publishing UPDATE-NEW deltas (+1, +1) for {BaseKey}", currentBaseKey);
                 }
                 else
                 {
                      _logger.LogInformation("DocumentUpdatedEvent for {AccessKey} had no relevant changes.", updateEvent.AccessKey);
                 }
            }

            if (tasks.Any())
            {
                await Task.WhenAll(tasks);
            }
            else
            {
                 _logger.LogInformation("No Redis operations needed for event {EventType}", typeof(TEvent).Name);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error publishing event {EventType} for company {CompanyId}",
                typeof(TEvent).Name,
                documentEvent?.CompanyId ?? 0); // Use null conditional access
            // Consider if re-throwing is appropriate or if logging is sufficient
            // throw;
        }
    }

    // Helper method to build the base key
    private static string BuildBaseKey(int companyId, string consumptionDate,
        Domain.Enums.DocumentOrigin origin, Domain.Enums.DocumentType docType, Domain.Enums.DocumentStatus status)
    {
        // Use the enum value directly if it's already lowercase (like 'ws')
        // Otherwise, use the improved ToKebabCase for PascalCase enums
        var originStr = origin.ToString().All(char.IsLower) ? origin.ToString() : ToKebabCase(origin.ToString());
        var typeStr = docType.ToString().All(char.IsUpper) ? docType.ToString().ToLowerInvariant() : ToKebabCase(docType.ToString()); // Handle acronyms like NFE
        var statusStr = status.ToString().All(char.IsLower) ? status.ToString() : ToKebabCase(status.ToString());

        return $"agg:company:{companyId}:{consumptionDate}:{originStr}:{typeStr}:{statusStr}";
    }

    private static string ToKebabCase(string value)
    {
        if (string.IsNullOrEmpty(value))
            return value;

        // Handle known acronyms directly first for performance and accuracy
        switch (value)
        {
            case "NFE": return "nfe";
            case "NFCE": return "nfce";
            case "CTE": return "cte";
            case "CTEOS": return "cteos";
            case "MDFE": return "mdfe";
            case "CFE": return "cfe";
            case "NFSE": return "nfse";
            // Add other known acronyms if needed
        }

        // General PascalCase to kebab-case conversion
        var builder = new StringBuilder();
        builder.Append(char.ToLowerInvariant(value[0]));
        for (int i = 1; i < value.Length; i++)
        {
            char currentChar = value[i];
            if (char.IsUpper(currentChar))
            {
                // Add hyphen if previous char was not upper/hyphen OR if next char is lower
                // Handles "MyID" -> "my-id" and "PascalCase" -> "pascal-case"
                if (value[i - 1] != '-' &&
                    (!char.IsUpper(value[i - 1]) ||
                     (i + 1 < value.Length && !char.IsUpper(value[i + 1]) && char.IsLower(value[i + 1]))))
                {
                    builder.Append('-');
                }
                builder.Append(char.ToLowerInvariant(currentChar));
            }
            else
            {
                builder.Append(currentChar); // Append lower/digit/symbol as is
            }
        }
        return builder.ToString();
    }


    // This method now only provides the *initial* deltas for create/delete.
    // Update logic is handled directly in PublishAsync.
    private static (int quantity, int total) GetDeltas<TEvent>(TEvent @event) where TEvent : DocumentEventBase
    {
        return @event switch
        {
            DocumentCreatedEvent => (1, 1),
            DocumentDeletedEvent => (-1, -1),
            DocumentUpdatedEvent => (0, 0), // Update event itself doesn't have initial delta, logic is in PublishAsync
            _ => throw new ArgumentException($"Unsupported event type: {typeof(TEvent).Name}")
        };
    }
}
