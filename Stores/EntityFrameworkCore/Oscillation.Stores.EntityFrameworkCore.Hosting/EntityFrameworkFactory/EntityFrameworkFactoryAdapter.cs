using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Oscillation.Stores.EntityFrameworkCore.Abstractions;

namespace Oscillation.Stores.EntityFrameworkCore.Hosting.EntityFrameworkFactory;

public class EntityFrameworkFactoryAdapter<TDbContext> : ISignalStoreDbContextFactory
    where TDbContext : SignalStoreDbContext
{
    private readonly IDbContextFactory<TDbContext> _dbContextFactory;

    public EntityFrameworkFactoryAdapter(IDbContextFactory<TDbContext> dbContextFactory)
    {
        _dbContextFactory = dbContextFactory;
    }
    
    public SignalStoreDbContext Create()
    {
        return _dbContextFactory.CreateDbContext();
    }
}

public static class EntityFrameworkFactoryAdapterHostingExtensions
{
    public static EntityFrameworkCoreSignalStoreBuilder UseEntityFrameworkFactory<TDbContext>(this EntityFrameworkCoreSignalStoreBuilder builder)
        where TDbContext : SignalStoreDbContext
    {
        return builder.UseDbContextFactory(provider =>
        {
            var dbContextFactory = provider.GetRequiredService<IDbContextFactory<TDbContext>>();
            return new EntityFrameworkFactoryAdapter<TDbContext>(dbContextFactory);
        });
    }
}