using Oscillation.Stores.EntityFrameworkCore.Abstractions;

namespace Oscillation.Stores.EntityFrameworkCore.Hosting.Templates;

public class PostgresSignalSelectTemplateProvider : ISignalSelectTemplateProvider
{
    private readonly string _tableName;

    public PostgresSignalSelectTemplateProvider(string? schema, string? prefix)
    {
        var tableName = $"{prefix}Signals";

        _tableName = !string.IsNullOrEmpty(schema) ? $"\"{schema}\".\"{tableName}\"" : $"\"{tableName}\"";
    }

    public string ProvideSelectSignalTemplate()
    {
        return $$"""
                    SELECT * FROM {{_tableName}} 
                    WHERE "Group" = {0} AND "LocalId" = {1} 
                    FOR UPDATE
                 """;
    }

    public string ProvideSelectReadySignalsTemplate()
    {
        return $$"""
                    SELECT * FROM {{_tableName}} 
                    WHERE "State" = 'Pending' AND "NextFireTime" <= {0} 
                    ORDER BY "NextFireTime" 
                    LIMIT {1} 
                    FOR UPDATE SKIP LOCKED
                 """;
    }

    public string ProvideSelectZombieSignalsTemplate()
    {
        return $$"""
                    SELECT * FROM {{_tableName}} 
                    WHERE "State" = 'Processing' AND "ProcessingTimeout" <= {0} 
                    ORDER BY "ProcessingTimeout" 
                    LIMIT {1} 
                    FOR UPDATE SKIP LOCKED
                 """;
    }
}

public class PostgresSignalSelectTemplateProviderBuilder
{
    private string? _schema;
    private string? _prefix;

    public PostgresSignalSelectTemplateProviderBuilder()
    {
        _schema = null;
        _prefix = null;
    }

    public PostgresSignalSelectTemplateProviderBuilder UseSchema(string schema)
    {
        _schema = schema;
        return this;
    }

    public PostgresSignalSelectTemplateProviderBuilder UsePrefix(string prefix)
    {
        _prefix = prefix;
        return this;
    }

    public PostgresSignalSelectTemplateProvider Build()
    {
        return new PostgresSignalSelectTemplateProvider(_schema, _prefix);
    }
}

public static class PostgresSignalSelectTemplateProviderHostingExtensions
{
    public static EntityFrameworkCoreSignalStoreBuilder UsePostgresSelectTemplateProvider(
        this EntityFrameworkCoreSignalStoreBuilder builder,
        Action<IServiceProvider, PostgresSignalSelectTemplateProviderBuilder> configure)
    {
        return builder.UseSelectTemplateProvider(provider =>
        {
            var selectTemplateProviderBuilder = new PostgresSignalSelectTemplateProviderBuilder();
            configure(provider, selectTemplateProviderBuilder);

            return selectTemplateProviderBuilder.Build();
        });
    }
}