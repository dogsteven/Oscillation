using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Oscillation.Core.Abstractions;
using Oscillation.Hosting.Client.Abstractions;

namespace Oscillation.Hosting.Client
{
    public class SignalSubmissionTemplate
    {
        private readonly ISignalStore _signalStore;
        private readonly ISignalNotificationPublisher _signalNotificationPublisher;

        public SignalSubmissionTemplate(ISignalStore signalStore, ISignalNotificationPublisher signalNotificationPublisher)
        {
            _signalStore = signalStore;
            _signalNotificationPublisher = signalNotificationPublisher;
        }

        public async Task SubmitAsync(SignalSubmission submission, CancellationToken cancellationToken)
        {
            await _signalStore.RunSessionAsync(async session =>
            {
                var signal = new Signal(submission.Group, submission.LocalId, submission.Payload, submission.FireTime);
                session.Add(signal);
                
                await session.SaveChangesAsync(cancellationToken);
            }, cancellationToken);

            _signalNotificationPublisher.PublishPotentialNextFireTime(submission.FireTime);
        }

        public async Task SubmitAsync(IEnumerable<SignalSubmission> submissions, CancellationToken cancellationToken)
        {
            DateTime? potentialNextFireTime = null;
            
            await _signalStore.RunSessionAsync(async session =>
            {
                foreach (var submission in submissions)
                {
                    var signal = new Signal(submission.Group, submission.LocalId, submission.Payload, submission.FireTime);
                    session.Add(signal);

                    if (potentialNextFireTime == null || potentialNextFireTime > submission.FireTime)
                    {
                        potentialNextFireTime = submission.FireTime;
                    }
                }

                await session.SaveChangesAsync(cancellationToken);
            }, cancellationToken);

            if (potentialNextFireTime != null)
            {
                _signalNotificationPublisher.PublishPotentialNextFireTime(potentialNextFireTime.Value);
            }
        }

        public async Task<bool> TryCancelAsync(string group, Guid localId, CancellationToken cancellationToken)
        {
            return await _signalStore.RunSessionAsync(async session =>
            {
                var signal = await session.GetSignalAsync(group, localId, cancellationToken);

                if (signal == null || signal.State != SignalState.Pending)
                {
                    return false;
                }
                
                session.Remove(signal);
                
                await session.SaveChangesAsync(cancellationToken);

                return true;
            }, cancellationToken);
        }
    }

    public readonly struct SignalSubmission
    {
        public readonly string Group;

        public readonly Guid LocalId;

        public readonly string Payload;

        public readonly DateTime FireTime;

        public SignalSubmission(string group, Guid localId, string payload, DateTime fireTime)
        {
            Group = group;
            LocalId = localId;
            Payload = payload;
            FireTime = fireTime;
        }
    }
}