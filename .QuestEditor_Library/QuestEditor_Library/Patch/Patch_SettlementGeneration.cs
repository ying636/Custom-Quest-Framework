using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace QuestEditor_Library
{
    [HarmonyPatch(typeof(Settlement), "MapGeneratorDef", MethodType.Getter)]
    public class Patch_SettlementGeneration
    {
        [HarmonyPostfix]
        public static void postfix(Settlement __instance, ref MapGeneratorDef __result)
        {
            if (__instance.Faction != null)
            {
                if (__instance.Faction.IsPlayer && Find.TickManager.TicksGame < 10f && Find.Scenario.AllParts.ToList().Exists(p => p is ScenPart_GenerateCustomMap))
                {
                    __result = QEDefOf.CQF_Base_Player;
                    return;
                }
                if (!cache.ContainsKey(__instance.Faction))
                {
                    cache.SetOrAdd(__instance.Faction, DefDatabase<SpecialMapGenerationDef>.AllDefsListForReading.Exists(d =>
                    {
                        return d.factionOfReplacedSettlement == __instance.Faction.def && d.replaceSettlement;
                    }
                    ));
                }
                if (cache.TryGetValue(__instance.Faction, out bool replace))
                {
                    if (replace)
                    {
                        __result = QEDefOf.CQF_SpecialMapGenerator;
                    }
                }
            }
        }


        public static Dictionary<Faction,bool> cache = new Dictionary<Faction, bool>(); 
    }

    [HarmonyPatch(typeof(GenStep_Outpost), "Generate")]
    public class Patch_OutpostGeneration
    {
        [HarmonyPrefix]
        public static bool prefix(Map map, GenStepParams parms)
        {
            if (map.ParentFaction is Faction f)
            {
                if (!cache.ContainsKey(f))
                {
                    cache.SetOrAdd(f, DefDatabase<SpecialMapGenerationDef>.AllDefsListForReading.Exists(d =>
                    {
                        return d.factionOfReplacedSettlement == f.def && d.replaceOutpost;
                    }
                    ));
                }
                if (cache.TryGetValue(f, out bool replace))
                {
                    if (replace)
                    {
                        Dictionary<CustomMapDataDef, float> datas = new Dictionary<CustomMapDataDef, float>();
                        DefDatabase<SpecialMapGenerationDef>.AllDefsListForReading.ForEach(d =>
                        {
                            if (d.factionOfReplacedSettlement == f.def)
                            {
                                d.customMapDataTagsToReplace.ForEach(t => DefDatabase<CustomMapDataDef>.AllDefsListForReading.ForEach(mapData =>
                                {
                                    if (mapData.tags.Contains(t.tag))
                                    {
                                        datas.SetOrAdd(mapData, t.weight);
                                    }
                                }));
                                d.customMapDatasToReplace.ForEach(data => datas.SetOrAdd(data.data, data.weight));
                            }
                        });
                        GenStep_CustomMap.SpawnCustomMap(map, parms, datas.RandomElementByWeight(d => d.Value).Key, null, false, null, false, false, false, true);
                        return false;
                    }
                }
            }
            return true;
        }


        public static Dictionary<Faction, bool> cache = new Dictionary<Faction, bool>();
    }
}
