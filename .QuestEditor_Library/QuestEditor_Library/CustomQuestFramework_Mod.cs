using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace QuestEditor_Library
{
    public class CustomQuestFramework_Mod : Mod
    {
        public CustomQuestFramework_Mod(ModContentPack content) : base(content)
        {
            this.setting = this.GetSettings<CustomQuestFramework_ModSetting>();
            LongEventHandler.ExecuteWhenFinished(ApplySpecialBuildingTranslations);
        }
        public override string SettingsCategory()
        {
            return "Custom Quest Framework";
        }
        public override void DoSettingsWindowContents(Rect inRect)
        {
            Widgets.CheckboxLabeled(new Rect(inRect.x, inRect.y, inRect.width, 30f), "ShowCQF".Translate(), ref this.setting.showCQF);
            Widgets.CheckboxLabeled(new Rect(inRect.x, inRect.y + 35f, inRect.width, 30f), "AutoCompileDialogTextKey".Translate(), ref this.setting.autoCompileDialogTextKey);
        }
        private static void ApplySpecialBuildingTranslations()
        {
            ThingDef fixedWall = DefDatabase<ThingDef>.GetNamed("QF_MiracleWall");
            fixedWall.label = "CQFFixedWallLabel".Translate();
            fixedWall.description = "CQFFixedWallDescription".Translate();

            ThingDef fixedDoor = DefDatabase<ThingDef>.GetNamed("QF_MiracleDoor");
            fixedDoor.label = "CQFFixedDoorLabel".Translate();
            fixedDoor.description = "CQFFixedDoorDescription".Translate();
        }
        public CustomQuestFramework_ModSetting setting = null;
    }

    public class CustomQuestFramework_ModSetting : ModSettings
    {
        public CustomQuestFramework_ModSetting()
        {
            CustomQuestFramework_ModSetting.setting = this;
        }
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref this.showCQF, "showCQF");
            Scribe_Values.Look(ref this.autoCompileDialogTextKey, "autoCompileDialogTextKey", true);
        }

        public bool showCQF = true;
        public bool autoCompileDialogTextKey = true;
        public static CustomQuestFramework_ModSetting setting;
    }
 
}
