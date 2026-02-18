using System;
using System.Threading;
using System.Threading.Tasks;
using Oscillation.Core.Abstractions;
using Oscillation.Core.Policies;

namespace Oscillation.Core
{
    public class ZombieSignalProcessor
    {
        private readonly ISignalStore _signalStore;
        private readonly IDistributionPolicyProvider _distributionPolicyProvider;
        
        private readonly ZombieSignalProcessorOptions _zombieProcessorOptions;
        
        private readonly ITimeProvider _timeProvider;

        public ZombieSignalProcessor(ISignalStore signalStore, IDistributionPolicyProvider distributionPolicyProvider, 
            ZombieSignalProcessorOptions zombieProcessorOptions, ITimeProvider timeProvider)
        {
            _signalStore = signalStore;
            _distributionPolicyProvider = distributionPolicyProvider;
            _zombieProcessorOptions = zombieProcessorOptions;
            _timeProvider = timeProvider;
        }
        
        public async Task StartAsync(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    await _signalStore.RunSessionAsync(async session =>
                    {
                        var now = _timeProvider.UtcDateTimeNow;

                        var signals = await session.GetZombieSignalsAsync(now, _zombieProcessorOptions.BatchSize, cancellationToken);

                        foreach (var signal in signals)
                        {
                            var policy = await _distributionPolicyProvider.GetPolicyOrDefaultAsync(signal.Group, cancellationToken);

                            now = _timeProvider.UtcDateTimeNow;

                            if (signal.RetryAttempts >= policy.MaxRetryAttempts)
                            {
                                signal.FailProcessing(now, policy.RetentionTimeout);
                            }
                            else
                            {
                                var delay = TimeSpan.FromMilliseconds(policy.RetryPatterns[signal.RetryAttempts]);

                                signal.FailProcessingAttempt(now, delay);
                            }
                        }

                        await session.SaveChangesAsync(cancellationToken);
                    }, cancellationToken);
                }
                finally
                {
                    await Task.Delay(_zombieProcessorOptions.PollInterval, cancellationToken);
                }
            }
        }
    }
    
    public class ZombieSignalProcessorOptions
    {
        public int BatchSize { get; set; }
        
        public TimeSpan PollInterval { get; set; }
    }
}