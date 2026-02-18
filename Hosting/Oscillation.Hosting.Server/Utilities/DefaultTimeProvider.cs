using System;
using Oscillation.Core.Abstractions;

namespace Oscillation.Hosting.Server.Utilities
{
    public class DefaultTimeProvider : ITimeProvider
    {
        public DateTime UtcDateTimeNow => DateTime.UtcNow;
    }

    public static class DefaultTimeProviderHostingExtensions
    {
        public static OscillationServerServiceConfigurator UseDefaultTimeProvider(this OscillationServerServiceConfigurator configurator)
        {
            return configurator.UseTimeProvider(provider => new DefaultTimeProvider());
        }
    }
}