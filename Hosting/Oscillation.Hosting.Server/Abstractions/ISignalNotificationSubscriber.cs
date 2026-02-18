using System;
using System.Threading;
using System.Threading.Tasks;

namespace Oscillation.Hosting.Server.Abstractions
{
    public interface ISignalNotificationSubscriber
    {
        public void RegisterHandler(ISignalNotificationHandler handler);
        public Task StartSubscribingAsync(CancellationToken cancellationToken);
    }

    public interface ISignalNotificationHandler
    {
        public void HandlePotentialNextFireTimePublished(DateTime potentialNextFireTime);
    }
}