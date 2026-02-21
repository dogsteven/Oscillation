using System;

namespace Oscillation.Core.Abstractions
{
    public class Signal
    {
        public string Group { get; private set; }

        public Guid LocalId { get; private set; }

        public string Payload { get; private set; }

        public DateTime FireTime { get; private set; }

        public SignalState State { get; private set; }

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
            State = SignalState.Pending;
            NextFireTime = fireTime;
            ProcessingTimeout = null;
            RetryAttempts = 0;
            FinalizedTime = null;
        }

        public void AttemptProcessing(DateTime now, TimeSpan processingTimeout)
        {
            if (State != SignalState.Pending)
            {
                throw new InvalidOperationException("Signal is not in pending status.");
            }

            State = SignalState.Processing;
            ProcessingTimeout = now.Add(processingTimeout);
        }

        public void FailProcessingAttempt(DateTime now, TimeSpan delay)
        {
            if (State != SignalState.Processing)
            {
                throw new InvalidOperationException("Signal is not in processing status.");
            }

            State = SignalState.Pending;
            ProcessingTimeout = null;
            NextFireTime = now.Add(delay);
            RetryAttempts++;
        }

        public void CompleteProcessing(DateTime now, TimeSpan retentionTimeout)
        {
            if (State != SignalState.Processing)
            {
                throw new InvalidOperationException("Signal is not in processing status.");
            }

            State = SignalState.Success;
            ProcessingTimeout = null;
            FinalizedTime = now;
            DeadTime = now.Add(retentionTimeout);
        }

        public void FailProcessing(DateTime now, TimeSpan retentionTimeout)
        {
            if (State != SignalState.Processing)
            {
                throw new InvalidOperationException("Signal is not in processing status.");
            }

            State = SignalState.Failed;
            ProcessingTimeout = null;
            FinalizedTime = now;
            DeadTime = now.Add(retentionTimeout);
        }
    }

    public enum SignalState
    {
        Pending,
        Processing,
        Success,
        Failed
    }
}