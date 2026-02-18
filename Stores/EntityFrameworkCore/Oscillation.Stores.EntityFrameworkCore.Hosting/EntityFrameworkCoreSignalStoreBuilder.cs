using Microsoft.Extensions.DependencyInjection;
using Oscillation.Hosting.Client;
using Oscillation.Hosting.Server;
using Oscillation.Stores.EntityFrameworkCore.Abstractions;
using Oscillation.Stores.EntityFrameworkCore.Hosting.Standalone;

namespace Oscillation.Stores.EntityFrameworkCore.Hosting;

public class EntityFrameworkCoreSignalStoreBuilder
{
    private Func<IServiceProvider, ISignalStoreDbContextFactory> _dbContextFactoryFactory;
    private Func<IServiceProvider, ISignalSelectTemplateProvider?> _selectTemplateProviderFactory;

    public EntityFrameworkCoreSignalStoreBuilder()
    {
        _dbContextFactoryFactory = provider => provider.GetRequiredService<ISignalStoreDbContextFactory>();
        _selectTemplateProviderFactory = provider => provider.GetRequiredService<ISignalSelectTemplateProvider>();
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

    public EntityFrameworkCoreSignalStore Build(IServiceProvider provider)
    {
        var dbContextFactory = _dbContextFactoryFactory(provider);
        var selectTemplateProvider = _selectTemplateProviderFactory(provider);
        
        return new EntityFrameworkCoreSignalStore(dbContextFactory, selectTemplateProvider);
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