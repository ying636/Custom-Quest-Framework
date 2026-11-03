using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;
using HarmonyLib;
using System.Reflection;
using RimWorld;
using RimWorld.Planet;

namespace QuestEditor_Library
{
	[StaticConstructorOnStartup]
	public static class PatchMain
	{
		static PatchMain()
		{
#if RIMWORLD_1_7
            foreach (WorldObjectDef definition in DefDatabase<WorldObjectDef>.AllDefsListForReading)
            {
                if (definition.worldObjectClass == null || !typeof(Caravan).IsAssignableFrom(definition.worldObjectClass)) continue;
                definition.comps ??= new List<WorldObjectCompProperties>();
                if (!definition.comps.Any(properties => properties?.compClass == typeof(WorldObjectComp_CaravanActions)))
                    definition.comps.Add(new WorldObjectCompProperties { compClass = typeof(WorldObjectComp_CaravanActions) });
            }
#endif
			Harmony harmony = new Harmony("QF_Patch");
			harmony.PatchAll(Assembly.GetExecutingAssembly());
		}
	}
}
