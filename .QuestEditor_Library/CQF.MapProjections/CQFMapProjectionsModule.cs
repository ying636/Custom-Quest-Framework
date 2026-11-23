using HarmonyLib;
using Verse;

namespace QuestEditor_Library;

public sealed class CQFMapProjectionsModule : ICQFModule, ICQFMapLifetimeCollector
{
    public void Initialize()
    {
        Harmony harmony = new("CQF.MapProjections");
        try
        {
            harmony.PatchAll(typeof(CQFMapProjectionsModule).Assembly);
            CQFModuleLoader.RegisterMapLifetimeModule(this);
        }
        catch
        {
            harmony.UnpatchAll(harmony.Id);
            throw;
        }
    }

    public bool KeepsMapActive(Game game, Map map) => MapComponent_CQFMapProjections.KeepsActive(game, map);

    public void CollectActiveMaps(Game game, HashSet<Map> maps) => MapComponent_CQFMapProjections.CollectActiveSources(game, maps);

    public void BeforeMapsDisposed(Game game)
    {
        if (game.components != null) game.GetComponent<GameComponent_CQFProjectionUI>()?.Dispose();
    }
}
