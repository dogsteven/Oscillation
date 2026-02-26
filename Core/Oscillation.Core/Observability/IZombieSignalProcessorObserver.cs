using System;

namespace Oscillation.Core.Observability
{
    public interface IZombieSignalProcessorObserver
    {
        void OnZombieSignalRecovered(string group, Guid localId);
        void OnZombieSignalTerminated(string group, Guid localId);
        void OnCycleError(Exception cause);
    }
}
