using Npgsql;
using Oscillation.Hosting.Client.Abstractions;

namespace Oscillation.Notification.Postgres.Client;

public class PostgresSignalNotificationPublisher : ISignalNotificationPublisher
{
    private readonly string _connectionString;
    private readonly string _channel;

    public PostgresSignalNotificationPublisher(string connectionString, string channel)
    {
        _connectionString = connectionString;
        _channel = channel;
    }
    
    public void PublishPotentialNextFireTime(DateTime potentialNextFireTime)
    {
        _ = PublishPotentialNextFireTimeAsync(potentialNextFireTime);
    }

    private async Task PublishPotentialNextFireTimeAsync(DateTime potentialNextFireTime)
    {
        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();

        await using var command = new NpgsqlCommand("SELECT pg_notify(@Channel, @Payload)", connection);
        command.Parameters.AddWithValue("Channel", _channel);
        command.Parameters.AddWithValue("Payload", potentialNextFireTime.ToString("0"));

        await command.ExecuteNonQueryAsync();
    }
}