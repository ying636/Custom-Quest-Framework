using LudeonTK;
using RimWorld;
using Verse;

namespace QuestEditor_Library;

public static class DebugActions_Cinematics
{
    [DebugAction("CQF", "CQF_Cinematic_MapStudio", allowedGameStates = AllowedGameStates.PlayingOnMap)]
    public static DebugActionNode CinematicStudio()
    {
        return new DebugActionNode("CQF_Cinematic_MapStudio".Translate(), action: OpenStudio);
    }

    private static void OpenStudio()
    {
        if (Current.Game == null || Find.CurrentMap == null)
        {
            Messages.Message("CQF_Cinematic_MapRequired".Translate(), MessageTypeDefOf.CautionInput);
            return;
        }
        GameComponent_Cinematics? host = GameComponent_Cinematics.Instance;
        if (CQFCinematicTestSession.Current?.IsBusy == true || host?.Active != null || host?.Queue.Count > 0 || CQFPresentationGate.For(Current.Game).IsBusy)
        {
            Messages.Message("CQF_Cinematic_TestBusy".Translate(), MessageTypeDefOf.CautionInput);
            return;
        }
        if (Find.WindowStack.TryGetWindow<QuestEditor_Cinematics>(out QuestEditor_Cinematics studio))
            Find.WindowStack.Notify_ManuallySetFocus(studio);
        else
            Find.WindowStack.Add(new QuestEditor_Cinematics());
    }
}
