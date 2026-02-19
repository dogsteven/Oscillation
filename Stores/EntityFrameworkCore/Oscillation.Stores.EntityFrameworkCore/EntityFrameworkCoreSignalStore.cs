using Microsoft.EntityFrameworkCore;
using Oscillation.Core.Abstractions;
using Oscillation.Stores.EntityFrameworkCore.Abstractions;

namespace Oscillation.Stores.EntityFrameworkCore;

public class EntityFrameworkCoreSignalStore : ISignalStore
{
    private readonly ISignalStoreDbContextFactory _dbContextFactory;
    private readonly ISignalSelectTemplateProvider? _selectTemplateProvider;

    public EntityFrameworkCoreSignalStore(ISignalStoreDbContextFactory dbContextFactory,
        ISignalSelectTemplateProvider? selectTemplateProvider)
    {
        _dbContextFactory = dbContextFactory;
        _selectTemplateProvider = selectTemplateProvider;
    }

    public async Task RunSessionAsync(Func<ISignalStoreSession, Task> operation, CancellationToken cancellationToken)
    {
        await using var dummyContext = _dbContextFactory.Create();

        var strategy = dummyContext.Database.CreateExecutionStrategy();

        await strategy.ExecuteAsync(async ct =>
        {
            await using var context = _dbContextFactory.Create();
            
            await using var transaction = await context.Database.BeginTransactionAsync(ct);

            try
            {
                var session = new EntityFrameworkCoreSignalStoreSession(context, _selectTemplateProvider);

                await operation(session);

                await transaction.CommitAsync(ct);
            }
            catch
            {
                await transaction.RollbackAsync(ct);
                throw;
            }
        }, cancellationToken);
    }

    public async Task<TValue> RunSessionAsync<TValue>(Func<ISignalStoreSession, Task<TValue>> operation, CancellationToken cancellationToken)
    {
        await using var dummyDbContext = _dbContextFactory.Create();

        var strategy = dummyDbContext.Database.CreateExecutionStrategy();

        return await strategy.ExecuteAsync(async ct =>
        {
            await using var dbContext = _dbContextFactory.Create();
            
            await using var transaction = await dbContext.Database.BeginTransactionAsync(ct);

            try
            {
                var session = new EntityFrameworkCoreSignalStoreSession(dbContext, _selectTemplateProvider);

                var result = await operation(session);

                await transaction.CommitAsync(ct);

                return result;
            }
            catch
            {
                await transaction.RollbackAsync(ct);
                throw;
            }
        }, cancellationToken);
    }
}

public class EntityFrameworkCoreSignalStoreSession : ISignalStoreSession
{
    private readonly SignalStoreDbContext _dbContext;
    private readonly ISignalSelectTemplateProvider? _selectTemplateProvider;

    public EntityFrameworkCoreSignalStoreSession(SignalStoreDbContext dbContext,
        ISignalSelectTemplateProvider? selectTemplateProvider)
    {
        _dbContext = dbContext;
        _selectTemplateProvider = selectTemplateProvider;
    }

    public async Task<Signal?> GetSignalAsync(string group, Guid localId, CancellationToken cancellationToken)
    {
        if (_selectTemplateProvider != null)
        {
            return await _dbContext.Signals
                .FromSqlRaw(_selectTemplateProvider.ProvideSelectSignalTemplate(), group, localId)
                .SingleOrDefaultAsync(cancellationToken);
        }
        
        return await _dbContext.Signals.FindAsync([group, localId], cancellationToken);
    }

    public async Task<DateTime?> GetNextFireTimeAsync(CancellationToken cancellationToken)
    {
        return await _dbContext.Signals.AsNoTracking()
            .Where(signal => signal.State == SignalState.Pending)
            .MinAsync(signal => (DateTime?)signal.NextFireTime, cancellationToken);
    }

    public async Task<List<Signal>> GetReadySignalsAsync(DateTime now, int maxCount, CancellationToken cancellationToken)
    {
        if (_selectTemplateProvider != null)
        {
            return await _dbContext.Signals
                .FromSqlRaw(_selectTemplateProvider.ProvideSelectReadySignalsTemplate(), now, maxCount)
                .ToListAsync(cancellationToken);
        }
        
        return await _dbContext.Signals
            .Where(signal => signal.State == SignalState.Pending && signal.NextFireTime <= now)
            .OrderBy(signal => signal.NextFireTime)
            .Take(maxCount)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<Signal>> GetZombieSignalsAsync(DateTime now, int maxCount, CancellationToken cancellationToken)
    {
        if (_selectTemplateProvider != null)
        {
            return await _dbContext.Signals
                .FromSqlRaw(_selectTemplateProvider.ProvideSelectZombieSignalsTemplate(), now, maxCount)
                .ToListAsync(cancellationToken);
        }
        
        return await _dbContext.Signals
            .Where(signal => signal.State == SignalState.Processing && signal.ProcessingTimeout != null && signal.ProcessingTimeout <= now)
            .OrderBy(signal => signal.ProcessingTimeout)
            .Take(maxCount)
            .ToListAsync(cancellationToken);
    }

    public async Task CleanDeadSignalsAsync(DateTime now, int maxCount, CancellationToken cancellationToken)
    {
        await _dbContext.Signals
            .Where(signal => signal.DeadTime != null && signal.DeadTime <= now)
            .Take(maxCount)
            .ExecuteDeleteAsync(cancellationToken);
    }

    public void Add(Signal signal)
    {
        _dbContext.Signals.Add(signal);
    }

    public void Remove(Signal signal)
    {
        _dbContext.Signals.Remove(signal);
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
