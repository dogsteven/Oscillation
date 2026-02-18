using NATS.Net;
using Oscillation.Hosting.Server;

namespace Oscillation.Notification.Nats.Server;

public class NatsSignalNotificationSubscriberBuilder
{
    private NatsClient? _natsClient;
    private string? _subject;

    public NatsSignalNotificationSubscriberBuilder()
    {
        _natsClient = null;
        _subject = null;
    }

    public NatsSignalNotificationSubscriberBuilder UseNatsClient(NatsClient natsClient)
    {
        _natsClient = natsClient;
        return this;
    }

    public NatsSignalNotificationSubscriberBuilder UseSubject(string subject)
    {
        _subject = subject;
        return this;
    }
    
    public NatsSignalNotificationSubscriber Build()
    {
        if (_natsClient == null)
        {
            throw new InvalidOperationException("NatsClient is required");
        }

        if (string.IsNullOrWhiteSpace(_subject))
        {
            throw new InvalidOperationException("Subject is required");
        }

        return new NatsSignalNotificationSubscriber(_natsClient, _subject);
    }
}

public static class NatsSignalNotificationSubscriberHostingExtensions
{
    public static OscillationServerServiceConfigurator UseNats(
        this OscillationServerServiceConfigurator configurator,
        Action<IServiceProvider, NatsSignalNotificationSubscriberBuilder> configure)
    {
        return configurator.UseSignalNotificationSubscriber(provider =>
        {
            var natsPublisherBuilder = new NatsSignalNotificationSubscriberBuilder();
            configure(provider, natsPublisherBuilder);

            return natsPublisherBuilder.Build();
        });
    }
}