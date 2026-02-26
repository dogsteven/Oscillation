using System;
using System.Threading;
using System.Threading.Tasks;
using Oscillation.Core.Abstractions;
using Oscillation.Core.Observability;

namespace Oscillation.Core
{
    public class DeadSignalProcessor
    {
        private readonly ISignalStore _signalStore;
        private readonly DeadSignalProcessorOptions _deadProcessorOptions;

        private readonly ITimeProvider _timeProvider;

        private readonly IDeadSignalProcessorObserver? _observer;

        public DeadSignalProcessor(ISignalStore signalStore, DeadSignalProcessorOptions deadProcessorOptions,
            ITimeProvider timeProvider, IDeadSignalProcessorObserver? observer = null)
        {
            _signalStore = signalStore;
            _deadProcessorOptions = deadProcessorOptions;
            _timeProvider = timeProvider;
            _observer = observer;
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
                catch (Exception ex) when (!(ex is OperationCanceledException))
                {
                    _observer?.OnCycleError(ex);
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