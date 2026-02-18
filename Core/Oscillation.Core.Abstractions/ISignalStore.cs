using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Oscillation.Core.Abstractions
{
    public interface ISignalStore
    {
        public Task RunSessionAsync(Func<ISignalStoreSession, Task> operation, CancellationToken cancellationToken);
        public Task<TValue> RunSessionAsync<TValue>(Func<ISignalStoreSession, Task<TValue>> operation, CancellationToken cancellationToken);
    }

    public interface ISignalStoreSession
    {
        public Task<Signal?> GetSignalAsync(string group, Guid localId, CancellationToken cancellationToken);
        public Task<DateTime?> GetNextFireTimeAsync(CancellationToken cancellationToken);
        public Task<List<Signal>> GetReadySignalsAsync(DateTime now, int maxCount, CancellationToken cancellationToken);
        public Task<List<Signal>> GetZombieSignalsAsync(DateTime now, int maxCount, CancellationToken cancellationToken);
        public Task CleanDeadSignalsAsync(DateTime now, int maxCount, CancellationToken cancellationToken);
    
        public void Add(Signal signal);
        public Task SaveChangesAsync(CancellationToken cancellationToken);
    }

}