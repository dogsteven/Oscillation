using System;
using System.Collections.Generic;
using Oscillation.Core.Observability;

namespace Oscillation.Hosting.Server.Observability
{
    internal sealed class CompositeZombieSignalProcessorObserver : IZombieSignalProcessorObserver
    {
        private readonly IReadOnlyList<IZombieSignalProcessorObserver> _observers;

        internal CompositeZombieSignalProcessorObserver(IReadOnlyList<IZombieSignalProcessorObserver> observers)
        {
            _observers = observers;
        }

        public void OnZombieSignalRecovered(string group, Guid localId)
        {
            foreach (var observer in _observers)
                observer.OnZombieSignalRecovered(group, localId);
        }

        public void OnZombieSignalTerminated(string group, Guid localId)
        {
            foreach (var observer in _observers)
                observer.OnZombieSignalTerminated(group, localId);
        }

        public void OnCycleError(Exception cause)
        {
            foreach (var observer in _observers)
                observer.OnCycleError(cause);
        }
    }
}
