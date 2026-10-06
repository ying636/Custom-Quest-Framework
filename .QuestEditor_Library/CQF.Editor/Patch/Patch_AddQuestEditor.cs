using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using HarmonyLib;

namespace QuestEditor_Library
{
    [HarmonyPatch(typeof(OptionListingUtility), "DrawOptionListing")]
    public class Patch_AddQuestEditor
    {
        [HarmonyPrefix]
        static bool PreFix(ref List<ListableOption> optList)
        {
            if (optList.Find((x) => x is ListableOption_WebLink) == null && CQFEditorBridge.IsLoaded)
            {
                optList.Add(new ListableOption("QuestEditor".Translate(), () => Find.WindowStack.Add(new Page_QuestEditor())));
            }
            return true;
        }
    }
}
