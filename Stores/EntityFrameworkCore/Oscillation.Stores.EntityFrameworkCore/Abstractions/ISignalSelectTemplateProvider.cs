namespace Oscillation.Stores.EntityFrameworkCore.Abstractions;

public interface ISignalSelectTemplateProvider
{
    public string ProvideSelectSignalTemplate();
    public string ProvideSelectSignalsTemplate(int count);
    public string ProvideSelectReadySignalsTemplate();
    public string ProvideSelectZombieSignalsTemplate();
}