using System.Data;
using Microsoft.Extensions.DependencyInjection;
using Oscillation.Hosting.Client;
using Oscillation.Hosting.Server;
using Oscillation.Stores.EntityFrameworkCore.Abstractions;

namespace Oscillation.Stores.EntityFrameworkCore.Hosting;

public class EntityFrameworkCoreSignalStoreBuilder
{
    private Func<IServiceProvider, ISignalStoreDbContextFactory> _dbContextFactoryFactory;
    private Func<IServiceProvider, ISignalSelectTemplateProvider?> _selectTemplateProviderFactory;
    private IsolationLevel? _isolationLevel;

    public EntityFrameworkCoreSignalStoreBuilder()
    {
        _dbContextFactoryFactory = provider => provider.GetRequiredService<ISignalStoreDbContextFactory>();
        _selectTemplateProviderFactory = provider => provider.GetRequiredService<ISignalSelectTemplateProvider>();
        _isolationLevel = null;
    }

    public EntityFrameworkCoreSignalStoreBuilder UseDbContextFactory(Func<IServiceProvider, ISignalStoreDbContextFactory> dbContextFactoryFactory)
    {
        _dbContextFactoryFactory = dbContextFactoryFactory;
        return this;
    }

    public EntityFrameworkCoreSignalStoreBuilder UseSelectTemplateProvider(Func<IServiceProvider, ISignalSelectTemplateProvider?> selectTemplateProviderFactory)
    {
        _selectTemplateProviderFactory = selectTemplateProviderFactory;
        return this;
    }

    public EntityFrameworkCoreSignalStoreBuilder UseIsolationLevel(IsolationLevel isolationLevel)
    {
        _isolationLevel = isolationLevel;
        return this;
    }

    public EntityFrameworkCoreSignalStore Build(IServiceProvider provider)
    {
        var dbContextFactory = _dbContextFactoryFactory(provider);
        var selectTemplateProvider = _selectTemplateProviderFactory(provider);
        
        return new EntityFrameworkCoreSignalStore(dbContextFactory, selectTemplateProvider, _isolationLevel);
    }
}

public static class EntityFrameworkCoreSignalStoreHostingExtensions
{
    public static OscillationClientServiceConfigurator UseEntityFrameworkCoreSignalStore(
        this OscillationClientServiceConfigurator configurator,
        Action<EntityFrameworkCoreSignalStoreBuilder> configure)
    {
        return configurator.UseSignalStore(provider =>
        {
            var builder = new EntityFrameworkCoreSignalStoreBuilder();
            configure(builder);

            return builder.Build(provider);
        });
    }
    
    public static OscillationServerServiceConfigurator UseEntityFrameworkCoreSignalStore(
        this OscillationServerServiceConfigurator configurator,
        Action<EntityFrameworkCoreSignalStoreBuilder> configure)
    {
        return configurator.UseSignalStore(provider =>
        {
            var builder = new EntityFrameworkCoreSignalStoreBuilder();
            configure(builder);

            return builder.Build(provider);
        });
    }
}