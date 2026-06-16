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
    [HarmonyPatch(typeof(Caravan), "GetGizmos")]
    public class Patch_AddCaravanAction
    {
        [HarmonyPostfix]
        static void PostFix(ref IEnumerable<Gizmo> __result,Caravan __instance)
        {
            GameComponent_Editor comp = GameComponent_Editor.Instance;
            List<CaravanActionDef> defs = 
                DefDatabase<CaravanActionDef>.
                AllDefsListForReading.FindAll(a => !a.conditions.Exists(c =>!c.Satisfied(__instance)));
            if (defs.Any()) 
            {
                List<Gizmo> result = __result.ToList();
                foreach (var item in defs)
                {
                    Command_Action action = (new Command_Action()
                    {
                        defaultLabel = item.label,
                        defaultDesc = item.description,
                        icon = item.Icon,
                        action = () =>
                        {
                            comp.CACDS.Add(item, new CD() { curTick = Find.TickManager.TicksGame, time = Find.TickManager.TicksGame + item.CD });
                            foreach (var item1 in item.actions)
                            {
                                item1.Work(__instance);
                            }
                        }
                    });
                    if (!comp.IsAvailable(item))
                    {
                        action.Disable("CQFCooldown".Translate());
                    }
                    result.Add(action);
                }
                __result = result;
            }
        }
    }
}

