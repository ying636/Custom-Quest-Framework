#if RIMWORLD_1_7
using HarmonyLib;
using Verse;

namespace QuestEditor_Library;

[HarmonyPatch(typeof(Window), "InnerWindowOnGUI")]
public static class Patch_CQFProjectionWindow
{
    public static void Prefix(out CQFMapProjectionContext? __state)
    {
        __state = CQFMapProjectionContext.Current == null && CQFMapProjectionContext.Focused is { CanControl: true } projection
            ? new CQFMapProjectionContext(projection, Current.Camera) : null;
    }

    public static Exception? Finalizer(Exception? __exception, CQFMapProjectionContext? __state)
    {
        __state?.Dispose();
        return __exception;
    }
}
#endif
