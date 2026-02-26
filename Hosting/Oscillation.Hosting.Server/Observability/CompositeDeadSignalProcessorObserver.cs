using System;
using System.Collections.Generic;
using Oscillation.Core.Observability;

namespace Oscillation.Hosting.Server.Observability
{
    internal sealed class CompositeDeadSignalProcessorObserver : IDeadSignalProcessorObserver
    {
        private readonly IReadOnlyList<IDeadSignalProcessorObserver> _observers;

        internal CompositeDeadSignalProcessorObserver(IReadOnlyList<IDeadSignalProcessorObserver> observers)
        {
            _observers = observers;
        }

        public void OnCycleError(Exception cause)
        {
            foreach (var observer in _observers)
                observer.OnCycleError(cause);
        }
    }
}
