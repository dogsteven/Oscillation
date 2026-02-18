using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Oscillation.Core.Policies;

namespace Oscillation.Hosting.Server.Utilities
{
    public class DefaultDistributionPolicyProvider : IDistributionPolicyProvider
    {
        private readonly IReadOnlyDictionary<string, DistributionPolicy> _policies;
        private readonly DistributionPolicy _defaultPolicy;

        public DefaultDistributionPolicyProvider(IReadOnlyDictionary<string, DistributionPolicy> policies, DistributionPolicy defaultPolicy)
        {
            _policies = policies;
            _defaultPolicy = defaultPolicy;
        }
        
        public ValueTask<DistributionPolicy> GetDefaultPolicyAsync(CancellationToken cancellationToken)
        {
            return new ValueTask<DistributionPolicy>(_defaultPolicy);
        }

        public ValueTask<DistributionPolicy?> GetPolicyAsync(string group, CancellationToken cancellationToken)
        {
            if (!_policies.TryGetValue(group, out var policy))
            {
                return new ValueTask<DistributionPolicy?>(default(DistributionPolicy?));
            }

            return new ValueTask<DistributionPolicy?>(policy);
        }
    }
    
    public class DefaultDistributionPolicyProviderBuilder
    {
        private readonly Dictionary<string, DistributionPolicy> _policies;
        private DistributionPolicy? _defaultPolicy;

        public DefaultDistributionPolicyProviderBuilder()
        {
            _policies = new Dictionary<string, DistributionPolicy>();
            _defaultPolicy = new DistributionPolicy();
        }

        public DefaultDistributionPolicyProviderBuilder Register(string group, DistributionPolicy policy)
        {
            _policies.Add(group, policy);
            return this;
        }

        public DefaultDistributionPolicyProviderBuilder SetDefaultPolicy(DistributionPolicy defaultPolicy)
        {
            _defaultPolicy = defaultPolicy;
            return this;
        }

        public DefaultDistributionPolicyProvider Build()
        {
            if (_defaultPolicy == null)
            {
                throw new InvalidOperationException("Default policy must be provided");
            }

            return new DefaultDistributionPolicyProvider(_policies, _defaultPolicy);
        }
    }

    public static class DefaultDistributionPolicyProviderHostingExtensions
    {
        public static OscillationServerServiceConfigurator UseDefaultDistributionPolicyProvider(this OscillationServerServiceConfigurator configurator, Action<DefaultDistributionPolicyProviderBuilder> configure)
        {
            return configurator.UseDistributionPolicyProvider(provider =>
            {
                var distributionPolicyProviderBuilder = new DefaultDistributionPolicyProviderBuilder();
                configure(distributionPolicyProviderBuilder);
                
                return distributionPolicyProviderBuilder.Build();
            });
        }
    }
}