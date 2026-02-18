using System.Threading;
using System.Threading.Tasks;

namespace Oscillation.Core.Policies
{
    public interface IDistributionPolicyProvider
    {
        public ValueTask<DistributionPolicy> GetDefaultPolicyAsync(CancellationToken cancellationToken);
        public ValueTask<DistributionPolicy?> GetPolicyAsync(string group, CancellationToken cancellationToken);
    }

    public static class DistributionPolicyProviderExtensions
    {
        public static async ValueTask<DistributionPolicy> GetPolicyOrDefaultAsync(
            this IDistributionPolicyProvider provider,
            string group, CancellationToken cancellationToken)
        {
            return await provider.GetPolicyAsync(group, cancellationToken) ??
                   await provider.GetDefaultPolicyAsync(cancellationToken);
        }
    }
}