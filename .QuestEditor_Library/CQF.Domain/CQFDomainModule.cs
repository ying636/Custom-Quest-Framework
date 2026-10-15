using HarmonyLib;

namespace QuestEditor_Library;

public sealed class CQFDomainModule : ICQFModule
{
    public void Initialize()
    {
        new Harmony("CQF.Domain").PatchAll(typeof(CQFDomainModule).Assembly);
        CQFModuleLoader.RegisterMapModuleFactory(host => new CQFDomainRuntime(host));
    }
}
