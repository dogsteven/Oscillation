using System.Collections.Concurrent;
using System.Globalization;
using NATS.Net;
using Oscillation.Hosting.Server.Abstractions;

namespace Oscillation.Notification.Nats.Server;

public class NatsSignalNotificationSubscriber : ISignalNotificationSubscriber
{
    private readonly NatsClient _natsClient;
    private readonly string _subject;

    private readonly ConcurrentBag<ISignalNotificationHandler> _handlers;
    private long _runningFlag;

    public NatsSignalNotificationSubscriber(NatsClient natsClient, string subject)
    {
        _natsClient = natsClient;
        _subject = subject;

        _handlers = new ConcurrentBag<ISignalNotificationHandler>();
        _runningFlag = 0;
    }
    
    public void RegisterHandler(ISignalNotificationHandler handler)
    {
        if (Interlocked.Read(ref _runningFlag) == 1)
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

        await foreach (var message in _natsClient.SubscribeAsync<string>(_subject, cancellationToken: cancellationToken))
        {
            if (message.Data == null)
            {
                continue;
            }

            if (!DateTime.TryParse(message.Data, null, DateTimeStyles.RoundtripKind, out var potentialNextFireTime))
            {
                continue;
            }

            foreach (var handler in _handlers)
            {
                handler.HandlePotentialNextFireTimePublished(potentialNextFireTime);
            }
        }

        _runningFlag = 0;
    }
}