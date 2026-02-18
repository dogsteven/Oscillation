using NATS.Net;
using Oscillation.Hosting.Client;

namespace Oscillation.Notification.Nats.Client;

public class NatsSignalNotificationPublisherBuilder
{
    private NatsClient? _natsClient;
    private string? _subject;

    public NatsSignalNotificationPublisherBuilder()
    {
        _natsClient = null;
        _subject = null;
    }

    public NatsSignalNotificationPublisherBuilder UseNatsClient(NatsClient natsClient)
    {
        _natsClient = natsClient;
        return this;
    }

    public NatsSignalNotificationPublisherBuilder UseSubject(string subject)
    {
        _subject = subject;
        return this;
    }

    public NatsSignalNotificationPublisher Build()
    {
        if (_natsClient == null)
        {
            throw new InvalidOperationException("NatsClient is required");
        }

        if (string.IsNullOrWhiteSpace(_subject))
        {
            throw new InvalidOperationException("Subject is required");
        }
        
        return new NatsSignalNotificationPublisher(_natsClient, _subject);
    }
}

public static class NatsSignalNotificationPublisherHostingExtensions
{
    public static OscillationClientServiceConfigurator UseNats(
        this OscillationClientServiceConfigurator configurator,
        Action<IServiceProvider, NatsSignalNotificationPublisherBuilder> configure)
    {
        return configurator.UseSignalNotificationPublisher(provider =>
        {
            var natsPublisherBuilder = new NatsSignalNotificationPublisherBuilder();
            configure(provider, natsPublisherBuilder);

            return natsPublisherBuilder.Build();
        });
    }
}