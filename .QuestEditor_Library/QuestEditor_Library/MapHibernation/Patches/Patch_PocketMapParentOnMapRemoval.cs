using HarmonyLib;
using RimWorld.Planet;
using Verse;

namespace QuestEditor_Library
{
    /// <summary>
    /// Vanilla removes every pocket map whose source map is gone and assumes in that loop that a
    /// <see cref="PocketMapParent"/> listed in <c>Find.World.pocketMaps</c> still owns a live map:
    /// <c>item.Map.generatorDef...</c> is dereferenced without a null check, and <c>MapParent.Map</c>
    /// only looks at <c>Game.maps</c>. CQF hibernation detaches maps from that list, so a stale entry
    /// left behind by an older save (or by another mod) turns an ordinary map removal into a
    /// NullReferenceException. CQF itself keeps the list in sync now, so this prefix only repairs
    /// lists that were already damaged.
    /// </summary>
    [HarmonyPatch(typeof(Game), nameof(Game.DeinitAndRemoveMap))]
    public static class Patch_PocketMapParentOnMapRemoval
    {
        public static void Prefix()
        {
            if (Current.Game == null) return;
            List<PocketMapParent>? pocketMaps = Find.World?.pocketMaps;
            if (pocketMaps == null) return;
            int removed = pocketMaps.RemoveAll(parent => parent == null || parent.Map == null);
            if (removed > 0)
                Log.Warning("[CQF] Dropped " + removed + " pocket map entr" + (removed == 1 ? "y" : "ies")
                    + " without a live map before removing a map. Those maps are gone and vanilla would have crashed on them.");
        }
    }
}
