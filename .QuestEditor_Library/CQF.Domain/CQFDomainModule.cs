namespace QuestEditor_Library;

public sealed class CQFDomainModule : ICQFModule
{
    public void Initialize()
    {
        CQFModuleLoader.RegisterMapModuleFactory(host => new CQFDomainRuntime(host));
    }
}
