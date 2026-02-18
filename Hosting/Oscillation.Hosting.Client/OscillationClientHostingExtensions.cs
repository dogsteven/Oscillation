using System;
using Microsoft.Extensions.DependencyInjection;
using Oscillation.Core.Abstractions;
using Oscillation.Hosting.Client.Abstractions;

namespace Oscillation.Hosting.Client
{
    public static class OscillationClientHostingExtensions
    {
        public static IServiceCollection AddOscillationClient(this IServiceCollection services, Action<OscillationClientServiceConfigurator> configure)
        {
            var configurator = new OscillationClientServiceConfigurator();
            configure(configurator);
            
            configurator.Populate(services);
            return services;
        }

        public static OscillationClientServiceConfigurator UseSignalStore<TSignalStore>(this OscillationClientServiceConfigurator configurator)
            where TSignalStore : class, ISignalStore
        {
            return configurator.UseSignalStore(provider => provider.GetRequiredService<TSignalStore>());
        }

        public static OscillationClientServiceConfigurator UseSignalNotificationPublisher<TSignalNotificationPublisher>(this OscillationClientServiceConfigurator configurator)
            where TSignalNotificationPublisher : class, ISignalNotificationPublisher
        {
            return configurator.UseSignalNotificationPublisher(provider => provider.GetRequiredService<TSignalNotificationPublisher>());
        }
    }

    public class OscillationClientServiceConfigurator
    {
        private Func<IServiceProvider, ISignalStore> _signalStoreFactory;
        private Func<IServiceProvider, ISignalNotificationPublisher> _signalNotificationPublisherFactory;

        public OscillationClientServiceConfigurator()
        {
            _signalStoreFactory = provider => provider.GetRequiredService<ISignalStore>();
            _signalNotificationPublisherFactory = provider => provider.GetRequiredService<ISignalNotificationPublisher>();
        }

        public OscillationClientServiceConfigurator UseSignalStore(Func<IServiceProvider, ISignalStore> signalStoreFactory)
        {
            _signalStoreFactory = signalStoreFactory;
            return this;
        }

        public OscillationClientServiceConfigurator UseSignalNotificationPublisher(Func<IServiceProvider, ISignalNotificationPublisher> signalNotificationPublisherFactory)
        {
            _signalNotificationPublisherFactory = signalNotificationPublisherFactory;
            return this;
        }

        public void Populate(IServiceCollection services)
        {
            services.AddSingleton<SignalSubmissionTemplate>(provider =>
            {
                var signalStore = _signalStoreFactory(provider);
                var signalNotificationPublisher = _signalNotificationPublisherFactory(provider);
                return new SignalSubmissionTemplate(signalStore, signalNotificationPublisher);
            });
        }
    }
}