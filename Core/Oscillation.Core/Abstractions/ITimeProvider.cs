using System;

namespace Oscillation.Core.Abstractions
{
    public interface ITimeProvider
    {
        DateTime UtcDateTimeNow { get; }
    }
}