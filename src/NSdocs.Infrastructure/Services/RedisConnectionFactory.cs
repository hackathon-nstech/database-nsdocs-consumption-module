using Microsoft.Extensions.Options;
using NSdocs.Infrastructure.Configuration;
using StackExchange.Redis;

namespace NSdocs.Infrastructure.Services;

public interface IRedisConnectionFactory
{
    IConnectionMultiplexer GetConnection();
    IDatabase GetDatabase();
}

public class RedisConnectionFactory : IRedisConnectionFactory, IDisposable
{
    private readonly IConnectionMultiplexer _connection;
    private readonly RedisSettings _settings;
    private bool _disposed;

    public RedisConnectionFactory(IOptions<RedisSettings> settings)
    {
        _settings = settings.Value;
        
        var options = ConfigurationOptions.Parse(_settings.ConnectionString);
        options.DefaultDatabase = _settings.Database;
        
        if (_settings.ReconnectRetryPolicy > 0)
        {
            options.ConnectRetry = _settings.ReconnectRetryPolicy;
        }
        
        _connection = ConnectionMultiplexer.Connect(options);
    }

    public IConnectionMultiplexer GetConnection()
    {
        return _connection;
    }

    public IDatabase GetDatabase()
    {
        return _connection.GetDatabase();
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (_disposed)
            return;

        if (disposing)
        {
            _connection.Close();
            _connection.Dispose();
        }

        _disposed = true;
    }
}
