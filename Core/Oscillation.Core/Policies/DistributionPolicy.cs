using System;
using System.Collections.Generic;

namespace Oscillation.Core.Policies
{
    public class DistributionPolicy
    {
        public IReadOnlyList<int> RetryPatterns { get; set; } = null!;

        public TimeSpan ProcessingTimeout { get; set; }

        public TimeSpan RetentionTimeout { get; set; }

        public int MaxRetryAttempts => RetryPatterns.Count;
    }
}