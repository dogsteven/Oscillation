using System;

namespace Oscillation.Hosting.Client.Abstractions
{
    public interface ISignalNotificationPublisher
    {
        public void PublishPotentialNextFireTime(DateTime potentialNextFireTime);
    }
}