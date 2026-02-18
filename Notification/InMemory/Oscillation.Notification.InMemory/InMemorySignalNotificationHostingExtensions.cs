using Microsoft.Extensions.DependencyInjection;
using Oscillation.Hosting.Client;
using Oscillation.Hosting.Server;

namespace Oscillation.Notification.InMemory
{
    public static class InMemorySignalNotificationHostingExtensions
    {
        public static IServiceCollection AddInMemorySignalNotificationCenter(this IServiceCollection services)
        {
            return services.AddSingleton<InMemorySignalNotificationCenter>();
        }

        public static OscillationClientServiceConfigurator UseInMemoryNotification(this OscillationClientServiceConfigurator configurator)
        {
            return configurator.UseSignalNotificationPublisher(provider =>
            {
                var notificationCenter = provider.GetRequiredService<InMemorySignalNotificationCenter>();

                return notificationCenter.GetPublisher();
            });
        }

        public static OscillationServerServiceConfigurator UseInMemoryNotification(this OscillationServerServiceConfigurator configurator)
        {
            return configurator.UseSignalNotificationSubscriber(provider =>
            {
                var notificationCenter = provider.GetRequiredService<InMemorySignalNotificationCenter>();

                return notificationCenter.GetSubscriber();
            });
        }
    }
}