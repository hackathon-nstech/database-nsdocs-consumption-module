namespace NSdocs.Infrastructure.Configuration;

public class RedisSettings
{
    public string ConnectionString { get; set; } = string.Empty;
    public int Database { get; set; }
    public int ReconnectRetryPolicy { get; set; }
    public int FlushIntervalMs { get; set; } = 1000;
}
