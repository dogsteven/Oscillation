using System;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;
using Oscillation.Core;
using Oscillation.Core.Abstractions;
using Oscillation.Core.Observability;
using Oscillation.Core.Policies;
using Oscillation.Hosting.Server.Abstractions;
using Oscillation.Hosting.Server.Observability;

namespace Oscillation.Hosting.Server
{
    public static class OscillationServerHostingExtensions
    {
        public static IServiceCollection AddOscillationServer(this IServiceCollection services, Action<OscillationServerServiceConfigurator> configure)
        {
            var configurator = new OscillationServerServiceConfigurator();
            configure(configurator);

            configurator.Populate(services);
            return services;
        }

        public static OscillationServerServiceConfigurator UseSignalStore<TSignalStore>(this OscillationServerServiceConfigurator configurator)
            where TSignalStore : class, ISignalStore
        {
            return configurator.UseSignalStore(provider => provider.GetRequiredService<TSignalStore>());
        }

        public static OscillationServerServiceConfigurator UseDistributionGateway<TDistributionGateway>(this OscillationServerServiceConfigurator configurator)
            where TDistributionGateway : class, IDistributionGateway
        {
            return configurator.UseDistributionGateway(provider => provider.GetRequiredService<TDistributionGateway>());
        }

        public static OscillationServerServiceConfigurator UseDistributionPolicyProvider<TDistributionPolicyProvider>(this OscillationServerServiceConfigurator configurator)
            where TDistributionPolicyProvider : class, IDistributionPolicyProvider
        {
            return configurator.UseDistributionPolicyProvider(provider => provider.GetRequiredService<TDistributionPolicyProvider>());
        }

        public static OscillationServerServiceConfigurator UseTimeProvider<TTimeProvider>(this OscillationServerServiceConfigurator configurator)
            where TTimeProvider : class, ITimeProvider
        {
            return configurator.UseTimeProvider(provider => provider.GetRequiredService<TTimeProvider>());
        }

        public static OscillationServerServiceConfigurator UseSignalNotificationSubscriber<TSignalNotificationSubscriber>(this OscillationServerServiceConfigurator configurator)
            where TSignalNotificationSubscriber : class, ISignalNotificationSubscriber
        {
            return configurator.UseSignalNotificationSubscriber(provider => provider.GetRequiredService<TSignalNotificationSubscriber>());
        }

        public static OscillationServerServiceConfigurator AddSignalDistributorObserver<TObserver>(this OscillationServerServiceConfigurator configurator)
            where TObserver : class, ISignalDistributorObserver
        {
            return configurator.AddSignalDistributorObserver(provider => provider.GetRequiredService<TObserver>());
        }

        public static OscillationServerServiceConfigurator AddZombieSignalProcessorObserver<TObserver>(this OscillationServerServiceConfigurator configurator)
            where TObserver : class, IZombieSignalProcessorObserver
        {
            return configurator.AddZombieSignalProcessorObserver(provider => provider.GetRequiredService<TObserver>());
        }

        public static OscillationServerServiceConfigurator AddDeadSignalProcessorObserver<TObserver>(this OscillationServerServiceConfigurator configurator)
            where TObserver : class, IDeadSignalProcessorObserver
        {
            return configurator.AddDeadSignalProcessorObserver(provider => provider.GetRequiredService<TObserver>());
        }
    }

    public class OscillationServerServiceConfigurator
    {
        private Func<IServiceProvider, ISignalStore> _signalStoreFactory;
        private Func<IServiceProvider, IDistributionGateway> _distributionGatewayFactory;
        private Func<IServiceProvider, IDistributionPolicyProvider> _distributionPolicyProviderFactory;
        private Func<IServiceProvider, ITimeProvider> _timeProviderFactory;
        private Func<IServiceProvider, ISignalNotificationSubscriber> _signalNotificationSubscriberFactory;

        private Func<IServiceProvider, int> _numberOfDistributorsFactory;

        private Action<IServiceProvider, SignalDistributorOptions> _signalDistributorOptionsConfigure;
        private Action<IServiceProvider, ZombieSignalProcessorOptions> _zombieSignalProcessorOptionsConfigure;
        private Action<IServiceProvider, DeadSignalProcessorOptions> _deadSignalProcessorOptionsConfigure;

        private readonly List<Func<IServiceProvider, ISignalDistributorObserver>> _signalDistributorObserverFactories;
        private readonly List<Func<IServiceProvider, IZombieSignalProcessorObserver>> _zombieSignalProcessorObserverFactories;
        private readonly List<Func<IServiceProvider, IDeadSignalProcessorObserver>> _deadSignalProcessorObserverFactories;

        public OscillationServerServiceConfigurator()
        {
            _signalStoreFactory = provider => provider.GetRequiredService<ISignalStore>();
            _distributionGatewayFactory = provider => provider.GetRequiredService<IDistributionGateway>();
            _distributionPolicyProviderFactory = provider => provider.GetRequiredService<IDistributionPolicyProvider>();
            _timeProviderFactory = provider => provider.GetRequiredService<ITimeProvider>();
            _signalNotificationSubscriberFactory = provider => provider.GetRequiredService<ISignalNotificationSubscriber>();

            _numberOfDistributorsFactory = provider => 1;

            _signalDistributorOptionsConfigure = (provider, options) => { };
            _zombieSignalProcessorOptionsConfigure = (provider, options) => { };
            _deadSignalProcessorOptionsConfigure = (provider, options) => { };

            _signalDistributorObserverFactories = new List<Func<IServiceProvider, ISignalDistributorObserver>>();
            _zombieSignalProcessorObserverFactories = new List<Func<IServiceProvider, IZombieSignalProcessorObserver>>();
            _deadSignalProcessorObserverFactories = new List<Func<IServiceProvider, IDeadSignalProcessorObserver>>();
        }

        public OscillationServerServiceConfigurator UseSignalStore(Func<IServiceProvider, ISignalStore> signalStoreFactory)
        {
            _signalStoreFactory = signalStoreFactory;
            return this;
        }

        public OscillationServerServiceConfigurator UseDistributionGateway(Func<IServiceProvider, IDistributionGateway> distributionGatewayFactory)
        {
            _distributionGatewayFactory = distributionGatewayFactory;
            return this;
        }

        public OscillationServerServiceConfigurator UseDistributionPolicyProvider(Func<IServiceProvider, IDistributionPolicyProvider> distributionPolicyProviderFactory)
        {
            _distributionPolicyProviderFactory = distributionPolicyProviderFactory;
            return this;
        }

        public OscillationServerServiceConfigurator UseTimeProvider(Func<IServiceProvider, ITimeProvider> timeProviderFactory)
        {
            _timeProviderFactory = timeProviderFactory;
            return this;
        }

        public OscillationServerServiceConfigurator UseSignalNotificationSubscriber(Func<IServiceProvider, ISignalNotificationSubscriber> signalNotificationSubscriberFactory)
        {
            _signalNotificationSubscriberFactory = signalNotificationSubscriberFactory;
            return this;
        }

        public OscillationServerServiceConfigurator SetNumberOfDistributors(Func<IServiceProvider, int> numberOfDistributorsFactory)
        {
            _numberOfDistributorsFactory = numberOfDistributorsFactory;
            return this;
        }

        public OscillationServerServiceConfigurator ConfigureSignalDistributorOptions(Action<IServiceProvider, SignalDistributorOptions> signalDistributorOptionsConfigure)
        {
            _signalDistributorOptionsConfigure = signalDistributorOptionsConfigure;
            return this;
        }

        public OscillationServerServiceConfigurator ConfigureZombieSignalProcessorOptions(Action<IServiceProvider, ZombieSignalProcessorOptions> zombieSignalProcessorOptionsConfigure)
        {
            _zombieSignalProcessorOptionsConfigure = zombieSignalProcessorOptionsConfigure;
            return this;
        }

        public OscillationServerServiceConfigurator ConfigureDeadSignalProcessorOptions(Action<IServiceProvider, DeadSignalProcessorOptions> deadSignalProcessorOptionsConfigure)
        {
            _deadSignalProcessorOptionsConfigure = deadSignalProcessorOptionsConfigure;
            return this;
        }

        public OscillationServerServiceConfigurator AddSignalDistributorObserver(Func<IServiceProvider, ISignalDistributorObserver> observerFactory)
        {
            _signalDistributorObserverFactories.Add(observerFactory);
            return this;
        }

        public OscillationServerServiceConfigurator AddZombieSignalProcessorObserver(Func<IServiceProvider, IZombieSignalProcessorObserver> observerFactory)
        {
            _zombieSignalProcessorObserverFactories.Add(observerFactory);
            return this;
        }

        public OscillationServerServiceConfigurator AddDeadSignalProcessorObserver(Func<IServiceProvider, IDeadSignalProcessorObserver> observerFactory)
        {
            _deadSignalProcessorObserverFactories.Add(observerFactory);
            return this;
        }

        public void Populate(IServiceCollection services)
        {
            services.AddHostedService(provider =>
            {
                var numberOfDistributors = _numberOfDistributorsFactory(provider);
                var signalStore = _signalStoreFactory(provider);
                var distributionGateway = _distributionGatewayFactory(provider);
                var distributionPolicyProvider = _distributionPolicyProviderFactory(provider);
                var timeProvider = _timeProviderFactory(provider);
                var signalNotificationSubscriber = _signalNotificationSubscriberFactory(provider);

                var signalDistributorOptions = new SignalDistributorOptions
                {
                    IdleTick = TimeSpan.FromMilliseconds(100),
                    BatchSize = 50,
                    MinPollInterval = TimeSpan.FromSeconds(1),
                    MaxPollInterval = TimeSpan.FromSeconds(60),
                    UseBatchCommit = true
                };

                var zombieSignalProcessorOptions = new ZombieSignalProcessorOptions
                {
                    BatchSize = 50,
                    PollInterval = TimeSpan.FromSeconds(120),
                };

                var deadSignalProcessorOptions = new DeadSignalProcessorOptions
                {
                    BatchSize = 100,
                    PollInterval = TimeSpan.FromSeconds(300)
                };

                _signalDistributorOptionsConfigure(provider, signalDistributorOptions);
                _zombieSignalProcessorOptionsConfigure(provider, zombieSignalProcessorOptions);
                _deadSignalProcessorOptionsConfigure(provider, deadSignalProcessorOptions);

                ISignalDistributorObserver? signalDistributorObserver = _signalDistributorObserverFactories.Count switch
                {
                    0 => null,
                    1 => _signalDistributorObserverFactories[0](provider),
                    _ => new CompositeSignalDistributorObserver(_signalDistributorObserverFactories.ConvertAll(f => f(provider)))
                };

                IZombieSignalProcessorObserver? zombieSignalProcessorObserver = _zombieSignalProcessorObserverFactories.Count switch
                {
                    0 => null,
                    1 => _zombieSignalProcessorObserverFactories[0](provider),
                    _ => new CompositeZombieSignalProcessorObserver(_zombieSignalProcessorObserverFactories.ConvertAll(f => f(provider)))
                };

                IDeadSignalProcessorObserver? deadSignalProcessorObserver = _deadSignalProcessorObserverFactories.Count switch
                {
                    0 => null,
                    1 => _deadSignalProcessorObserverFactories[0](provider),
                    _ => new CompositeDeadSignalProcessorObserver(_deadSignalProcessorObserverFactories.ConvertAll(f => f(provider)))
                };

                return new SignalProcessingBackgroundService(
                    numberOfDistributors,
                    signalStore,
                    distributionGateway,
                    distributionPolicyProvider,
                    timeProvider,
                    signalNotificationSubscriber,
                    signalDistributorOptions,
                    zombieSignalProcessorOptions,
                    deadSignalProcessorOptions,
                    signalDistributorObserver,
                    zombieSignalProcessorObserver,
                    deadSignalProcessorObserver);
            });
        }
    }
}