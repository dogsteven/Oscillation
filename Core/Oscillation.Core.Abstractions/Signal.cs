using System;

namespace Oscillation.Core.Abstractions
{
    public class Signal
    {
        public readonly string Group;

        public readonly Guid LocalId;

        public readonly string Payload;

        public readonly DateTime FireTime;

        public SignalDistributingStatus DistributingStatus { get; private set; }

        public DateTime NextFireTime { get; private set; }

        public DateTime? ProcessingTimeout { get; private set; }

        public int RetryAttempts { get; private set; }

        public DateTime? FinalizedTime { get; private set; }

        public DateTime? DeadTime { get; private set; }

    #pragma warning disable CS8618
        public Signal() { }
    #pragma warning restore CS8618

        public Signal(string group, Guid localId, string payload, DateTime fireTime)
        {
            Group = group;
            LocalId = localId;
            Payload = payload;
            FireTime = fireTime;
            DistributingStatus = SignalDistributingStatus.Pending;
            NextFireTime = fireTime;
            ProcessingTimeout = null;
            RetryAttempts = 0;
            FinalizedTime = null;
        }

        public void AttemptProcessing(DateTime now, TimeSpan processingTimeout)
        {
            if (DistributingStatus != SignalDistributingStatus.Pending)
            {
                throw new InvalidOperationException("Signal is not in pending status.");
            }

            DistributingStatus = SignalDistributingStatus.Processing;
            ProcessingTimeout = now.Add(processingTimeout);
        }

        public void FailProcessingAttempt(DateTime now, TimeSpan delay)
        {
            if (DistributingStatus != SignalDistributingStatus.Processing)
            {
                throw new InvalidOperationException("Signal is not in processing status.");
            }

            DistributingStatus = SignalDistributingStatus.Pending;
            ProcessingTimeout = null;
            NextFireTime = now.Add(delay);
            RetryAttempts++;
        }

        public void CompleteProcessing(DateTime now, TimeSpan retentionTimeout)
        {
            if (DistributingStatus != SignalDistributingStatus.Processing)
            {
                throw new InvalidOperationException("Signal is not in processing status.");
            }

            DistributingStatus = SignalDistributingStatus.Success;
            ProcessingTimeout = null;
            FinalizedTime = now;
            DeadTime = now.Add(retentionTimeout);
        }

        public void FailProcessing(DateTime now, TimeSpan retentionTimeout)
        {
            if (DistributingStatus != SignalDistributingStatus.Processing)
            {
                throw new InvalidOperationException("Signal is not in processing status.");
            }

            DistributingStatus = SignalDistributingStatus.Failed;
            ProcessingTimeout = null;
            FinalizedTime = now;
            DeadTime = now.Add(retentionTimeout);
        }
    }

    public enum SignalDistributingStatus
    {
        Pending,
        Processing,
        Success,
        Failed
    }
}