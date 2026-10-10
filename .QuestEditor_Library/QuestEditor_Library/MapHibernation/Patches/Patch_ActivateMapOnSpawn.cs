using HarmonyLib;
using Verse;

namespace QuestEditor_Library
{
    [HarmonyPatch(typeof(Thing), nameof(Thing.SpawnSetup))]
    public static class Patch_ActivateMapOnSpawn
    {
        public static void Prefix(Map map)
        {
            if (GameComponent_MapHibernation.Instance is { } component && component.IsHibernating(map))
            {
                component.Activate(map);
            }
        }
    }
}
