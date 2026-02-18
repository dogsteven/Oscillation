using Oscillation.Hosting.Server;

namespace Oscillation.Notification.Postgres.Server;

public class PostgresSignalNotificationSubscriberBuilder
{
    private string? _connectionString;
    private string? _channel;

    public PostgresSignalNotificationSubscriberBuilder()
    {
        _connectionString = null;
        _channel = null;
    }

    public PostgresSignalNotificationSubscriberBuilder UseConnectionString(string connectionString)
    {
        _connectionString = connectionString;
        return this;
    }

    public PostgresSignalNotificationSubscriberBuilder UseChannel(string channel)
    {
        _channel = channel;
        return this;
    }

    public PostgresSignalNotificationSubscriber Build()
    {
        if (string.IsNullOrWhiteSpace(_connectionString))
        {
            throw new InvalidOperationException("Connection string is required");
        }

        if (string.IsNullOrWhiteSpace(_channel))
        {
            throw new InvalidOperationException("Channel is required");
        }
        
        return new PostgresSignalNotificationSubscriber(_connectionString, _channel);
    }
}

public static class PostgresSignalNotificationSubscriberHostingExtensions
{
    public static OscillationServerServiceConfigurator UsePostgresNotification(
        this OscillationServerServiceConfigurator configurator,
        Action<IServiceProvider, PostgresSignalNotificationSubscriberBuilder> configure)
    {
        return configurator.UseSignalNotificationSubscriber(provider =>
        {
            var postgresSubscriberBuilder = new PostgresSignalNotificationSubscriberBuilder();
            configure(provider, postgresSubscriberBuilder);
            
            return postgresSubscriberBuilder.Build();
        });
    }
}