namespace Oscillation.Stores.EntityFrameworkCore.Abstractions;

public interface ISignalSelectTemplateProvider
{
    public string ProvideSelectSignalTemplate();
    public string ProvideSelectReadySignalsTemplate();
    public string ProvideSelectZombieSignalsTemplate();
}