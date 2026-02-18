using Oscillation.Hosting.Client;

namespace Oscillation.Notification.Postgres.Client;

public class PostgresSignalNotificationPublisherBuilder
{
    private string? _connectionString;
    private string? _channel;

    public PostgresSignalNotificationPublisherBuilder()
    {
        _connectionString = null;
        _channel = null;
    }

    public PostgresSignalNotificationPublisherBuilder UseConnectionString(string connectionString)
    {
        _connectionString = connectionString;
        return this;
    }

    public PostgresSignalNotificationPublisherBuilder UseChannel(string channel)
    {
        _channel = channel;
        return this;
    }

    public PostgresSignalNotificationPublisher Build()
    {
        if (string.IsNullOrWhiteSpace(_connectionString))
        {
            throw new InvalidOperationException("Connection string is required");
        }

        if (string.IsNullOrWhiteSpace(_channel))
        {
            throw new InvalidOperationException("Channel is required");
        }
        
        return new PostgresSignalNotificationPublisher(_connectionString, _channel);
    }
}

public static class PostgresSignalNotificationPublisherHostingExtensions
{
    public static OscillationClientServiceConfigurator UsePostgresNotification(
        this OscillationClientServiceConfigurator configurator,
        Action<IServiceProvider, PostgresSignalNotificationPublisherBuilder> configure)
    {
        return configurator.UseSignalNotificationPublisher(provider =>
        {
            var postgresPublisherBuilder = new PostgresSignalNotificationPublisherBuilder();
            configure(provider, postgresPublisherBuilder);

            return postgresPublisherBuilder.Build();
        });
    }
}