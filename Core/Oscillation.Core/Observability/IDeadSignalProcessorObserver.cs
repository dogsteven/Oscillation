using System;

namespace Oscillation.Core.Observability
{
    public interface IDeadSignalProcessorObserver
    {
        void OnCycleError(Exception cause);
    }
}
