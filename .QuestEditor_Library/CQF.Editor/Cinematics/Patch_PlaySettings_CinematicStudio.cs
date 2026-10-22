using HarmonyLib;
using RimWorld;
using Verse;

namespace QuestEditor_Library;

[HarmonyPatch(typeof(PlaySettings), nameof(PlaySettings.DoPlaySettingsGlobalControls))]
public static class Patch_PlaySettings_CinematicStudio
{
    private static void Postfix(WidgetRow row, bool worldView)
    {
        if (worldView || Current.Game == null || Find.CurrentMap == null || CQFCinematicTestSession.Current?.IsBusy == true) return;
        GameComponent_Cinematics? host = GameComponent_Cinematics.Instance;
        if (host?.Active != null || host?.Queue.Count > 0 || CQFPresentationGate.For(Current.Game).IsBusy) return;
        if (!row.ButtonIcon(TexButton.Play, "CQF_Cinematic_MapStudio".Translate())) return;
        if (Find.WindowStack.TryGetWindow<QuestEditor_Cinematics>(out QuestEditor_Cinematics studio))
            Find.WindowStack.Notify_ManuallySetFocus(studio);
        else
            Find.WindowStack.Add(new QuestEditor_Cinematics());
    }
}
