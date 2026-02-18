using Microsoft.EntityFrameworkCore;
using Oscillation.Stores.EntityFrameworkCore.Abstractions;

namespace Oscillation.Stores.EntityFrameworkCore.Hosting.Standalone;

public class StandaloneSignalStoreDbContextFactory : ISignalStoreDbContextFactory
{
    private readonly DbContextOptions<StandaloneSignalStoreDbContext> _options;
    private readonly string? _schema;
    private readonly string? _prefix;

    public StandaloneSignalStoreDbContextFactory(DbContextOptions<StandaloneSignalStoreDbContext> options, string? schema, string? prefix)
    {
        _options = options;
        _schema = schema;
        _prefix = prefix;
    }
    
    public SignalStoreDbContext Create()
    {
        return new StandaloneSignalStoreDbContext(_options, _schema, _prefix);
    }
}

public class StandaloneSignalStoreDbContextFactoryBuilder
{
    private readonly DbContextOptionsBuilder<StandaloneSignalStoreDbContext> _optionsBuilder;
    private string? _schema;
    private string? _prefix;

    public StandaloneSignalStoreDbContextFactoryBuilder()
    {
        _optionsBuilder = new DbContextOptionsBuilder<StandaloneSignalStoreDbContext>();
        _schema = null;
        _prefix = null;
    }

    public StandaloneSignalStoreDbContextFactoryBuilder ConfigureOptions(Action<DbContextOptionsBuilder<StandaloneSignalStoreDbContext>> configure)
    {
        configure(_optionsBuilder);
        return this;
    }

    public StandaloneSignalStoreDbContextFactoryBuilder UseSchema(string schema)
    {
        _schema = schema;
        return this;
    }

    public StandaloneSignalStoreDbContextFactoryBuilder UsePrefix(string prefix)
    {
        _prefix = prefix;
        return this;
    }

    public StandaloneSignalStoreDbContextFactory Build()
    {
        return new StandaloneSignalStoreDbContextFactory(_optionsBuilder.Options, _schema, _prefix);
    }
}

public static class StandaloneSignalStoreDbContextFactoryHostingExtensions
{
    public static EntityFrameworkCoreSignalStoreBuilder UseStandaloneDbContextFactory(
        this EntityFrameworkCoreSignalStoreBuilder builder,
        Action<IServiceProvider, StandaloneSignalStoreDbContextFactoryBuilder> configure)
    {
        return builder.UseDbContextFactory(provider =>
        {
            var dbContextFactoryBuilder = new StandaloneSignalStoreDbContextFactoryBuilder();
            configure(provider, dbContextFactoryBuilder);

            return dbContextFactoryBuilder.Build();
        });
    }
} 