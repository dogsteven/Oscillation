using System;
using System.Collections.Generic;
using Oscillation.Core.Observability;

namespace Oscillation.Hosting.Server.Observability
{
    internal sealed class CompositeSignalDistributorObserver : ISignalDistributorObserver
    {
        private readonly IReadOnlyList<ISignalDistributorObserver> _observers;

        internal CompositeSignalDistributorObserver(IReadOnlyList<ISignalDistributorObserver> observers)
        {
            _observers = observers;
        }

        public void OnPollCycleCompleted(IReadOnlyList<(string Group, Guid LocalId)> signals)
        {
            foreach (var observer in _observers)
                observer.OnPollCycleCompleted(signals);
        }

        public void OnSignalSucceeded(string group, Guid localId)
        {
            foreach (var observer in _observers)
                observer.OnSignalSucceeded(group, localId);
        }

        public void OnSignalRetried(string group, Guid localId, int attempt, Exception cause)
        {
            foreach (var observer in _observers)
                observer.OnSignalRetried(group, localId, attempt, cause);
        }

        public void OnSignalFailed(string group, Guid localId)
        {
            foreach (var observer in _observers)
                observer.OnSignalFailed(group, localId);
        }

        public void OnDistributionError(Exception cause)
        {
            foreach (var observer in _observers)
                observer.OnDistributionError(cause);
        }
    }
}
