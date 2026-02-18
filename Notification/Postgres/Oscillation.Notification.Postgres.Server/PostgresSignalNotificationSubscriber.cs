using System.Collections.Concurrent;
using System.Globalization;
using Npgsql;
using Oscillation.Hosting.Server.Abstractions;

namespace Oscillation.Notification.Postgres.Server;

public class PostgresSignalNotificationSubscriber : ISignalNotificationSubscriber
{
    private readonly string _connectionString;
    private readonly string _channel;
    
    private readonly ConcurrentBag<ISignalNotificationHandler> _handlers;
    private int _runningFlag;

    public PostgresSignalNotificationSubscriber(string connectionString, string channel)
    {
        _connectionString = connectionString;
        _channel = channel;
        
        _handlers = new ConcurrentBag<ISignalNotificationHandler>();
        _runningFlag = 0;
    }
    
    public void RegisterHandler(ISignalNotificationHandler handler)
    {
        if (_runningFlag == 0)
        {
            return;
        }
        
        _handlers.Add(handler);
    }

    public async Task StartSubscribingAsync(CancellationToken cancellationToken)
    {
        if (Interlocked.CompareExchange(ref _runningFlag, 1, 0) == 1)
        {
            return;
        }

        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                await using var connection = new NpgsqlConnection(_connectionString);
                await connection.OpenAsync(cancellationToken);
                
                connection.Notification += (_, args) =>
                {
                    if (!DateTime.TryParse(args.Payload, null, DateTimeStyles.RoundtripKind, out var potentialNextFireTime))
                    {
                        return;
                    }

                    foreach (var handler in _handlers)
                    {
                        handler.HandlePotentialNextFireTimePublished(potentialNextFireTime);
                    }
                };
                
                await using var command = new NpgsqlCommand($"LISTEN {_channel}", connection);
                await command.ExecuteNonQueryAsync(cancellationToken);

                while (!cancellationToken.IsCancellationRequested)
                {
                    await connection.WaitAsync(TimeSpan.FromSeconds(30), cancellationToken);
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception)
            {
                await Task.Delay(TimeSpan.FromSeconds(5), cancellationToken);
            }
        }

        _runningFlag = 0;
    }
}