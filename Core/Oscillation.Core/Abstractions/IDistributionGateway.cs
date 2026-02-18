using System;
using System.Threading;
using System.Threading.Tasks;

namespace Oscillation.Core.Abstractions
{
    public interface IDistributionGateway
    {
        public Task DistributeAsync(string group, Guid localId, string payload, CancellationToken cancellationToken);
    }
}