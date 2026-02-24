using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Oscillation.Core.Abstractions;
using Oscillation.Core.Policies;
using Oscillation.Core.Utilities;

namespace Oscillation.Core
{
    public class SignalDistributor : IDisposable
    {
        private readonly ISignalStore _signalStore;
        private readonly IDistributionGateway _distributionGateway;
        private readonly IDistributionPolicyProvider _distributionPolicyProvider;
        
        private readonly SignalDistributorOptions _distributorOptions;

        private readonly ITimeProvider _timeProvider;
        private readonly SemaphoreSlim _semaphore;

        private readonly DetachedTaskTracker _detachedTaskTracker;

        private long _runningFlag;

        private DateTime _nextPoll;
        private DateTime _minNextPoll;

        private bool _disposed = false;

        public SignalDistributor(ISignalStore signalStore, IDistributionGateway distributionGateway,
            IDistributionPolicyProvider distributionPolicyProvider, SignalDistributorOptions distributorOptions,
            ITimeProvider timeProvider)
        {
            _signalStore = signalStore;
            _distributionGateway = distributionGateway;
            _distributionPolicyProvider = distributionPolicyProvider;
            _distributorOptions = distributorOptions;
            _timeProvider = timeProvider;
            _semaphore = new SemaphoreSlim(1, 1);
            _detachedTaskTracker = new DetachedTaskTracker();
            _runningFlag = 0;
            _nextPoll = DateTime.MaxValue;
            _minNextPoll = DateTime.MaxValue;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            if (Interlocked.CompareExchange(ref _runningFlag, 1, 0) == 1)
            {
                return;
            }

            _ = _detachedTaskTracker.StartAsync();

            var random = new Random();

            _nextPoll = _timeProvider.UtcDateTimeNow.Add(TimeSpan.FromSeconds(2.0 + random.NextDouble() * 3.0));
            _minNextPoll = _nextPoll.Add(_distributorOptions.MinPollInterval);

            while (!cancellationToken.IsCancellationRequested)
            {
                var noPoll = false;
                
                try
                {
                    await _semaphore.WaitAsync(cancellationToken);
                    
                    if (_timeProvider.UtcDateTimeNow < _nextPoll)
                    {
                        noPoll = true;
                        continue;
                    }

                    var distributionInfos = new List<(string Group, Guid LocalId, string Payload, DistributionPolicy Policy)>();

                    await _signalStore.RunSessionAsync(async session =>
                    {
                        var now = _timeProvider.UtcDateTimeNow;

                        var signals = await session.GetReadySignalsAsync(now, _distributorOptions.BatchSize, cancellationToken);

                        foreach (var signal in signals)
                        {
                            var policy = await _distributionPolicyProvider.GetPolicyOrDefaultAsync(signal.Group, cancellationToken);

                            distributionInfos.Add((signal.Group, signal.LocalId, signal.Payload, policy));

                            now = _timeProvider.UtcDateTimeNow;

                            signal.AttemptProcessing(now, policy.ProcessingTimeout);
                        }

                        await session.SaveChangesAsync(cancellationToken);
                    }, cancellationToken);

                    if (distributionInfos.Any())
                    {
                        if (_distributorOptions.UseBatchCommit)
                        {
                            _detachedTaskTracker.Track(DistributeSignalsAsync(distributionInfos), distributionInfos.Max(distributionInfo => distributionInfo.Policy.ProcessingTimeout));
                        }
                        else
                        {
                            foreach (var distributionInfo in distributionInfos)
                            {
                                var (group, localId, payload, policy) = distributionInfo;

                                _detachedTaskTracker.Track(DistributeSignalAsync(group, localId, payload, policy), policy.ProcessingTimeout);
                            }
                        }
                    }

                    var now = _timeProvider.UtcDateTimeNow;
                    _minNextPoll = now.Add(_distributorOptions.MinPollInterval);
                    _nextPoll = now.Add(_distributorOptions.MaxPollInterval);

                    await FetchNextFireTimeAsync(cancellationToken);
                }
                catch
                {
                    // ignored
                }
                finally
                {
                    _semaphore.Release();
                    
                    if (noPoll)
                    {
                        try
                        {
                            await Task.Delay(_distributorOptions.IdleTick, cancellationToken);
                        }
                        catch
                        {
                            // ignored
                        }
                    }
                }
            }
            
            await _detachedTaskTracker.WaitForAllAsync();

            _runningFlag = 0;
        }

        private async Task FetchNextFireTimeAsync(CancellationToken cancellationToken)
        {
            var nextFireTime = await _signalStore.RunSessionAsync(session => session.GetNextFireTimeAsync(cancellationToken), cancellationToken);

            if (nextFireTime.HasValue)
            {
                AdjustNextPollTime(nextFireTime.Value);
            }
        }

        private async Task DistributeSignalsAsync(List<(string Group, Guid LocalId, string Payload, DistributionPolicy Policy)> distributionInfos)
        {
            var successes = await Task.WhenAll(distributionInfos.Select(TryDistributeSignalAsync));

            var resultMap = new Dictionary<(string Group, Guid LocalId), (bool success, DistributionPolicy Policy)>();

            for (var i = 0; i < distributionInfos.Count; ++i)
            {
                var (group, localId, _, policy) = distributionInfos[i];
                var success = successes[i];

                resultMap[(group, localId)] = (success, policy);
            }

            await _signalStore.RunSessionAsync(async session =>
            {
                var signals = await session.GetSignalsAsync(distributionInfos.Select(distributionInfo => (distributionInfo.Group, distributionInfo.LocalId)).ToList(), CancellationToken.None);

                foreach (var signal in signals)
                {
                    if (signal.State != SignalState.Processing)
                    {
                        continue;
                    }

                    if (resultMap.TryGetValue((signal.Group, signal.LocalId), out var result))
                    {
                        var (success, policy) = result;

                        var now = _timeProvider.UtcDateTimeNow;

                        if (success)
                        {
                            signal.CompleteProcessing(now, policy.RetentionTimeout);
                        }
                        else
                        {
                            if (signal.RetryAttempts >= policy.MaxRetryAttempts)
                            {
                                signal.FailProcessing(now, policy.RetentionTimeout);
                            }
                            else
                            {
                                var delay = policy.RetryPatterns[signal.RetryAttempts];

                                signal.FailProcessingAttempt(now, delay);
                            }
                        }
                    }
                }

                await session.SaveChangesAsync(CancellationToken.None);
            }, CancellationToken.None);
        }

        private async Task<bool> TryDistributeSignalAsync((string Group, Guid LocalId, string Payload, DistributionPolicy Policy) distributionInfo)
        {
            var (group, localId, payload, _) = distributionInfo;

            try
            {
                await _distributionGateway.DistributeAsync(group, localId, payload, CancellationToken.None);
                return true;
            }
            catch
            {
                return false;
            }
        }

        private async Task DistributeSignalAsync(string group, Guid localId, string payload, DistributionPolicy policy)
        {
            var cancellationToken = CancellationToken.None;

            var success = false;
            
            try
            {
                await _distributionGateway.DistributeAsync(group, localId, payload, cancellationToken);
                success = true;
            }
            catch
            {
                // ignore
            }

            await _signalStore.RunSessionAsync(async session =>
            {
                var signal = await session.GetSignalAsync(group, localId, cancellationToken);

                if (signal == null || signal.State != SignalState.Processing)
                {
                    return;
                }
                
                var now = _timeProvider.UtcDateTimeNow;

                if (success)
                {
                    signal.CompleteProcessing(now, policy.RetentionTimeout);
                }
                else
                {
                    if (signal.RetryAttempts >= policy.MaxRetryAttempts)
                    {
                        signal.FailProcessing(now, policy.RetentionTimeout);
                    }
                    else
                    {
                        var delay = policy.RetryPatterns[signal.RetryAttempts];

                        signal.FailProcessingAttempt(now, delay);
                    }
                }
                
                await session.SaveChangesAsync(cancellationToken);
            }, cancellationToken);
        }

        public async Task AdjustNextPollTimeAsync(DateTime potentialNextFireTime, CancellationToken cancellationToken)
        {
            if (Interlocked.Read(ref _runningFlag) == 0)
            {
                return;
            }

            try
            {
                await _semaphore.WaitAsync(cancellationToken);

                AdjustNextPollTime(potentialNextFireTime);
            }
            finally
            {
                _semaphore.Release();
            }
        }

        private void AdjustNextPollTime(DateTime potentialNextFireTime)
        {
            _nextPoll = potentialNextFireTime < _minNextPoll ? _minNextPoll : potentialNextFireTime;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
            {
                return;
            }

            if (disposing)
            {
                _semaphore.Dispose();
            }

            _disposed = true;
        }

        ~SignalDistributor()
        {
            Dispose(false);
        }
    }
    
    public class SignalDistributorOptions
    {
        public TimeSpan IdleTick { get; set; }
        
        public int BatchSize { get; set; }
        
        public TimeSpan MinPollInterval { get; set; }
        
        public TimeSpan MaxPollInterval { get; set; }

        public bool UseBatchCommit { get; set; }
    }
}