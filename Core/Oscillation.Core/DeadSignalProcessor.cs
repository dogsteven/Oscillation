using System;
using System.Threading;
using System.Threading.Tasks;
using Oscillation.Core.Abstractions;

namespace Oscillation.Core
{
    public class DeadSignalProcessor
    {
        private readonly ISignalStore _signalStore;
        private readonly DeadSignalProcessorOptions _deadProcessorOptions;
        
        private readonly ITimeProvider _timeProvider;

        public DeadSignalProcessor(ISignalStore signalStore, DeadSignalProcessorOptions deadProcessorOptions,
            ITimeProvider timeProvider)
        {
            _signalStore = signalStore;
            _deadProcessorOptions = deadProcessorOptions;
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

                        await session.CleanDeadSignalsAsync(now, _deadProcessorOptions.BatchSize, cancellationToken);
                    }, cancellationToken);
                }
                catch
                {
                    // ignored
                }
                finally
                {
                    await Task.Delay(_deadProcessorOptions.PollInterval, cancellationToken);
                }
            }
        }
    }
    
    public class DeadSignalProcessorOptions
    {
        public int BatchSize { get; set; }
        
        public TimeSpan PollInterval { get; set; }
    }
}