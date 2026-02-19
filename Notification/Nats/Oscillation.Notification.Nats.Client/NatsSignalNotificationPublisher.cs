using NATS.Net;
using Oscillation.Hosting.Client.Abstractions;

namespace Oscillation.Notification.Nats.Client;

public class NatsSignalNotificationPublisher : ISignalNotificationPublisher
{
    private readonly NatsClient _natsClient;
    private readonly string _subject;

    public NatsSignalNotificationPublisher(NatsClient natsClient, string subject)
    {
        _natsClient = natsClient;
        _subject = subject;
    }
    
    public void PublishPotentialNextFireTime(DateTime potentialNextFireTime)
    {
        _ = PublishPotentialNextFireTimeAsync(potentialNextFireTime);
    }

    private async Task PublishPotentialNextFireTimeAsync(DateTime potentialNextFireTime)
    {
        await _natsClient.PublishAsync(subject: _subject, data: potentialNextFireTime.ToString("O"));
    }
}