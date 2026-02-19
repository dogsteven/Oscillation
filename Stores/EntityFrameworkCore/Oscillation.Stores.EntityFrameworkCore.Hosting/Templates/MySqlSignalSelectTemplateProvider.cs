using Oscillation.Stores.EntityFrameworkCore.Abstractions;

namespace Oscillation.Stores.EntityFrameworkCore.Hosting.Templates;

public class MySqlSignalSelectTemplateProvider : ISignalSelectTemplateProvider
{
    private readonly string _tableName;

    public MySqlSignalSelectTemplateProvider(string? schema, string? prefix)
    {
        var tableName = $"{prefix}Signals";

        _tableName = !string.IsNullOrEmpty(schema) ? $"`{schema}`.`{tableName}`" : $"`{tableName}`";
    }

    public string ProvideSelectSignalTemplate()
    {
        return $$"""
                    SELECT * FROM {{_tableName}} 
                    WHERE `Group` = {0} AND `LocalId` = {1} 
                    FOR UPDATE
                 """;
    }

    public string ProvideSelectReadySignalsTemplate()
    {
        return $$"""
                    SELECT * FROM {{_tableName}} 
                    WHERE `State` = 'Pending' AND `NextFireTime` <= {0} 
                    ORDER BY `NextFireTime` 
                    LIMIT {1} 
                    FOR UPDATE SKIP LOCKED
                 """;
    }

    public string ProvideSelectZombieSignalsTemplate()
    {
        return $$"""
                    SELECT * FROM {{_tableName}} 
                    WHERE `State` = 'Processing' AND `ProcessingTimeout` <= {0} 
                    ORDER BY `ProcessingTimeout` 
                    LIMIT {1} 
                    FOR UPDATE SKIP LOCKED
                 """;
    }
}

public class MySqlSignalSelectTemplateProviderBuilder
{
    private string? _schema;
    private string? _prefix;

    public MySqlSignalSelectTemplateProviderBuilder()
    {
        _schema = null;
        _prefix = null;
    }

    public MySqlSignalSelectTemplateProviderBuilder UseSchema(string schema)
    {
        _schema = schema;
        return this;
    }

    public MySqlSignalSelectTemplateProviderBuilder UsePrefix(string prefix)
    {
        _prefix = prefix;
        return this;
    }

    public MySqlSignalSelectTemplateProvider Build()
    {
        return new MySqlSignalSelectTemplateProvider(_schema, _prefix);
    }
}

public static class MySqlSignalSelectTemplateProviderHostingExtensions
{
    public static EntityFrameworkCoreSignalStoreBuilder UseMysqlSelectTemplateProvider(
        this EntityFrameworkCoreSignalStoreBuilder builder,
        Action<IServiceProvider, MySqlSignalSelectTemplateProviderBuilder> configure)
    {
        return builder.UseSelectTemplateProvider(provider =>
        {
            var selectTemplateProviderBuilder = new MySqlSignalSelectTemplateProviderBuilder();
            configure(provider, selectTemplateProviderBuilder);

            return selectTemplateProviderBuilder.Build();
        });
    }
}