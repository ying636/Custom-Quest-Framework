using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;
using HarmonyLib;
using System.Reflection;

namespace QuestEditor_Library
{
	[StaticConstructorOnStartup]
	public static class PatchMain
	{
		static PatchMain()
		{
			Harmony harmony = new Harmony("QF_Patch");
			harmony.PatchAll(Assembly.GetExecutingAssembly());
		}
	}
}
