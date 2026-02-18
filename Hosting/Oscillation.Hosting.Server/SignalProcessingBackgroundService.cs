using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Oscillation.Core;
using Oscillation.Core.Abstractions;
using Oscillation.Core.Policies;
using Oscillation.Hosting.Server.Abstractions;

namespace Oscillation.Hosting.Server
{
    public class SignalProcessingBackgroundService : BackgroundService
    {
        private readonly List<SignalDistributor> _signalDistributors;
        private readonly ZombieSignalProcessor _zombieSignalProcessor;
        private readonly DeadSignalProcessor _deadSignalProcessor;
        private readonly ISignalNotificationSubscriber _signalNotificationSubscriber;

        public SignalProcessingBackgroundService(
            int numberOfDistributors,
            ISignalStore signalStore,
            IDistributionGateway distributionGateway,
            IDistributionPolicyProvider distributionPolicyProvider,
            ITimeProvider timeProvider,
            ISignalNotificationSubscriber signalNotificationSubscriber,
            SignalDistributorOptions signalDistributorOptions,
            ZombieSignalProcessorOptions zombieSignalProcessorOptions,
            DeadSignalProcessorOptions deadSignalProcessorOptions)
        {
            _signalDistributors = new List<SignalDistributor>(numberOfDistributors);
            
            for (var index = 0; index < numberOfDistributors; index++)
            {
                var signalDistributor = new SignalDistributor(
                    signalStore,
                    distributionGateway,
                    distributionPolicyProvider,
                    signalDistributorOptions,
                    timeProvider);
                
                _signalDistributors.Add(signalDistributor);
            }

            _zombieSignalProcessor = new ZombieSignalProcessor(
                signalStore,
                distributionPolicyProvider,
                zombieSignalProcessorOptions,
                timeProvider);

            _deadSignalProcessor = new DeadSignalProcessor(
                signalStore,
                deadSignalProcessorOptions,
                timeProvider);
            
            _signalNotificationSubscriber = signalNotificationSubscriber;
        }
        
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            foreach (var signalDistributor in _signalDistributors)
            {
                _signalNotificationSubscriber.RegisterHandler(new LambdaSignalNotificationHandler(potentialNextFireTime =>
                {
                    _ = signalDistributor.AdjustNextPollAsync(potentialNextFireTime, stoppingToken);
                }));
            }
            
            var processingTasks = new List<Task>(_signalDistributors.Count + 3)
            {
                _signalNotificationSubscriber.StartSubscribingAsync(stoppingToken),
                _zombieSignalProcessor.StartAsync(stoppingToken),
                _deadSignalProcessor.StartAsync(stoppingToken)
            };
            
            processingTasks.AddRange(_signalDistributors.Select(signalDistributor => signalDistributor.StartAsync(stoppingToken)));
            
            await Task.WhenAll(processingTasks);
        }

        public override void Dispose()
        {
            foreach (var signalDistributor in _signalDistributors)
            {
                signalDistributor.Dispose();
            }
            
            base.Dispose();
        }
    }

    public class LambdaSignalNotificationHandler : ISignalNotificationHandler
    {
        private readonly Action<DateTime> _handlePotentialNextFireTimePublished;

        public LambdaSignalNotificationHandler(Action<DateTime> handlePotentialNextFireTimePublished)
        {
            _handlePotentialNextFireTimePublished = handlePotentialNextFireTimePublished;
        }
        
        public void HandlePotentialNextFireTimePublished(DateTime potentialNextFireTime)
        {
            _handlePotentialNextFireTimePublished(potentialNextFireTime);
        }
    }
}