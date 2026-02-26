using System;
using System.Collections.Generic;

namespace Oscillation.Core.Observability
{
    public interface ISignalDistributorObserver
    {
        void OnPollCycleCompleted(IReadOnlyList<(string Group, Guid LocalId)> signals);
        void OnSignalSucceeded(string group, Guid localId);
        void OnSignalRetried(string group, Guid localId, int attempt, Exception cause);
        void OnSignalFailed(string group, Guid localId);
        void OnDistributionError(Exception cause);
    }
}
