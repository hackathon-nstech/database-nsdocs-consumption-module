using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NSdocs.Application.Common.Interfaces;

namespace NSdocs.Flusher.Services;

/// <summary>
/// Hosted service wrapper to start and manage the HealthMonitor background task.
/// </summary>
public class HealthMonitorService : IHostedService, IDisposable
{
    private readonly ILogger<HealthMonitorService> _logger;
    private readonly IHealthMonitor _healthMonitor;
    private Task? _executingTask;
    private readonly CancellationTokenSource _stoppingCts = new();

    public HealthMonitorService(ILogger<HealthMonitorService> logger, IHealthMonitor healthMonitor)
    {
        _logger = logger;
        _healthMonitor = healthMonitor;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Health Monitor Service is starting.");

        // Store the task we're executing
        _executingTask = _healthMonitor.StartMonitoringAsync(_stoppingCts.Token);

        // If the task is completed then return it, otherwise it's running
        return _executingTask.IsCompleted ? _executingTask : Task.CompletedTask;
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Health Monitor Service is stopping.");

        if (_executingTask == null)
        {
            return;
        }

        try
        {
            // Signal cancellation to the executing method
            _stoppingCts.Cancel();
        }
        finally
        {
            // Wait until the task completes or the stop token triggers
            await Task.WhenAny(_executingTask, Task.Delay(Timeout.Infinite, cancellationToken));
        }
        _logger.LogInformation("Health Monitor Service stopped.");
    }

    public void Dispose()
    {
        _stoppingCts.Cancel();
        _stoppingCts.Dispose();
        GC.SuppressFinalize(this);
    }
}
