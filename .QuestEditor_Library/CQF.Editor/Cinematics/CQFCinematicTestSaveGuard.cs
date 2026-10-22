using HarmonyLib;
using Verse;

namespace QuestEditor_Library;

internal sealed class CQFCinematicTestSaveGuard : IDisposable
{
    public CQFCinematicTestSaveGuard(CQFCinematicTestSession session)
    {
        if (active != null) throw new InvalidOperationException("CQF_Cinematic_TestBusy".Translate());
        this.session = session;
        harmony = new Harmony("CQF_CinematicTest_" + Guid.NewGuid().ToString("N"));
        active = this;
        try
        {
            harmony.Patch(AccessTools.Method(typeof(GameDataSaveLoader), nameof(GameDataSaveLoader.SaveGame)),
                prefix: new HarmonyMethod(typeof(CQFCinematicTestSaveGuard), nameof(SavePrefix)));
            harmony.Patch(AccessTools.Method(typeof(TickManager), nameof(TickManager.DoSingleTick)),
                prefix: new HarmonyMethod(typeof(CQFCinematicTestSaveGuard), nameof(TickPrefix)));
        }
        catch
        {
            Dispose();
            throw;
        }
    }

    public void Dispose()
    {
        if (disposed) return;
        disposed = true;
        if (ReferenceEquals(active, this)) active = null;
        try { harmony.Unpatch(AccessTools.Method(typeof(GameDataSaveLoader), nameof(GameDataSaveLoader.SaveGame)), HarmonyPatchType.Prefix, harmony.Id); }
        finally { harmony.Unpatch(AccessTools.Method(typeof(TickManager), nameof(TickManager.DoSingleTick)), HarmonyPatchType.Prefix, harmony.Id); }
    }

    private static bool SavePrefix()
    {
        if (active?.session.ShouldBlockSave() != true) return true;
        active.session.NotifySaveBlocked();
        return false;
    }

    private static bool TickPrefix() => active?.session.ShouldBlockLoadingTick() != true;

    private static CQFCinematicTestSaveGuard? active;
    private readonly CQFCinematicTestSession session;
    private readonly Harmony harmony;
    private bool disposed;
}
