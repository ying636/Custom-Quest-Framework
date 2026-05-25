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
            if (optList.Find((x) => x is ListableOption_WebLink) == null && Prefs.DevMode
                && (CustomQuestFramework_ModSetting.setting == null ||
                CustomQuestFramework_ModSetting.setting.showCQF)) 
            {
                optList.Add(new ListableOption("QuestEditor".Translate(), () => Find.WindowStack.Add(new Page_QuestEditor())));
            }
            return true;
        }
    }
    [HarmonyPatch(typeof(DebugWindowsOpener), "DrawButtons")]
    public class Patch_AddQuestEditorInDebug
    {
        [HarmonyPrefix]
        static bool PreFix(WidgetRow ___widgetRow)
        {
            if ((CustomQuestFramework_ModSetting.setting == null ||
                CustomQuestFramework_ModSetting.setting.showCQF) && ___widgetRow.ButtonIcon(TexButton.NewFile, "SaveMapToFile".Translate(), null, null, null, true, -1f))
            {
                Find.WindowStack.Add(new QuestEditor_SaveMapToFile());
            }
            return true;
        }
    }
}
