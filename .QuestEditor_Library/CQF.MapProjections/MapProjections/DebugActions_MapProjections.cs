#if RIMWORLD_1_7
using LudeonTK;
using Verse;

namespace QuestEditor_Library;

public static class DebugActions_MapProjections
{
    [DebugAction("CQF", "CQF_MP_Title", allowedGameStates = AllowedGameStates.PlayingOnMap)]
    public static DebugActionNode Menu()
    {
        return new DebugActionNode("CQF_MP_Title".Translate(), action: Open)
        {
            labelGetter = () => "CQF_MP_Title".Translate()
        };
    }

    public static void Open()
    {
        Map host = CQFMapProjectionContext.HostMap ?? throw new InvalidOperationException("[CQF] Map projection editor requires a host map.");
        GameComponent_CQFProjectionUI.Instance!.OpenManager(host);
    }
}
#endif
