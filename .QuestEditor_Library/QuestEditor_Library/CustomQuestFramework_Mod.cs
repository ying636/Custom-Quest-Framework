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
            CQFContentPaths.Initialize(content.RootDir);
            this.editorEnabledAtStartup = this.setting.enableEditor;
            CQFConditionalLoadFolders folders = new CQFConditionalLoadFolders(content, this.setting);
            CQFEditorLoader.Load(content, folders.GetFolders(nameof(CustomQuestFramework_ModSetting.enableEditor)));
            if (!CQFEditorBridge.IsLoaded) folders.Exclude(nameof(CustomQuestFramework_ModSetting.enableEditor));
            LongEventHandler.ExecuteWhenFinished(ApplySpecialBuildingTranslations);
        }
        public override string SettingsCategory()
        {
            return "Custom Quest Framework";
        }
        public override void DoSettingsWindowContents(Rect inRect)
        {
            bool previous = this.setting.enableEditor;
            Widgets.CheckboxLabeled(new Rect(inRect.x, inRect.y, inRect.width, 30f), "CQF_Editor_Enable".Translate(), ref this.setting.enableEditor);
            if (previous != this.setting.enableEditor) this.setting.Write();
            Widgets.Label(new Rect(inRect.x, inRect.y + 38f, inRect.width, 64f), "CQF_Editor_EnableHint".Translate());
            if (this.setting.enableEditor != this.editorEnabledAtStartup)
                Widgets.Label(new Rect(inRect.x, inRect.y + 105f, inRect.width, 32f), "CQF_Editor_RestartRequired".Translate().Colorize(ColorLibrary.Yellow));
            if (CQFEditorLoader.LoadError != null)
                Widgets.Label(new Rect(inRect.x, inRect.y + 142f, inRect.width, 60f), "CQF_Editor_LoadFailed".Translate().Colorize(ColorLibrary.RedReadable));
            if (!CQFEditorBridge.IsLoaded) return;
            if (Widgets.ButtonText(new Rect(inRect.x, inRect.y + 208f, 260f, 32f), "CQF_AI_Settings".Translate())) CQFEditorBridge.Module!.OpenSettings();
            Widgets.CheckboxLabeled(new Rect(inRect.x, inRect.y + 250f, inRect.width, 30f), "AutoCompileDialogTextKey".Translate(), ref this.setting.autoCompileDialogTextKey);
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
        private readonly bool editorEnabledAtStartup;
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
            Scribe_Values.Look(ref this.enableEditor, "enableEditor", false);
            Scribe_Values.Look(ref this.autoCompileDialogTextKey, "autoCompileDialogTextKey", true);
            Scribe_Values.Look(ref this.dialogAIEnabled, "dialogAIEnabled", false);
            Scribe_Values.Look(ref this.dialogAIAllowEditing, "dialogAIAllowEditing", true);
            Scribe_Values.Look(ref this.dialogAIAllowTextGeneration, "dialogAIAllowTextGeneration", true);
            Scribe_Values.Look(ref this.dialogAIUseTools, "dialogAIUseTools", true);
            Scribe_Values.Look(ref this.dialogAIPlanning, "dialogAIPlanning", true);
            Scribe_Values.Look(ref this.dialogAIAgents, "dialogAIAgents", true);
            Scribe_Values.Look(ref this.dialogAIParallelAgents, "dialogAIParallelAgents", 2);
            Scribe_Values.Look(ref this.dialogAIAgentModel, "dialogAIAgentModel", string.Empty);
            Scribe_Values.Look(ref this.dialogAIAgentPrompt, "dialogAIAgentPrompt", string.Empty);
            Scribe_Values.Look(ref this.dialogAIAdditionalPromptEnabled, "dialogAIAdditionalPromptEnabled", false);
            Scribe_Values.Look(ref this.dialogAIAdditionalPrompt, "dialogAIAdditionalPrompt", string.Empty);
            Scribe_Values.Look(ref this.dialogAIEndpoint, "dialogAIEndpoint", string.Empty);
            Scribe_Values.Look(ref this.dialogAIModel, "dialogAIModel", string.Empty);
            Scribe_Values.Look(ref this.dialogAIKey, "dialogAIKey", string.Empty);
            Scribe_Values.Look(ref this.dialogAITimeout, "dialogAITimeout", 120);
        }

        public bool enableEditor;
        public bool autoCompileDialogTextKey = true;
        public bool dialogAIEnabled;
        public bool dialogAIAllowEditing = true;
        public bool dialogAIAllowTextGeneration = true;
        public bool dialogAIUseTools = true;
        public bool dialogAIPlanning = true;
        public bool dialogAIAgents = true;
        public int dialogAIParallelAgents = 2;
        public string dialogAIAgentModel = string.Empty;
        public string dialogAIAgentPrompt = string.Empty;
        public bool dialogAIAdditionalPromptEnabled;
        public string dialogAIAdditionalPrompt = string.Empty;
        public string dialogAIEndpoint = string.Empty;
        public string dialogAIModel = string.Empty;
        public string dialogAIKey = string.Empty;
        public int dialogAITimeout = 120;
        public static CustomQuestFramework_ModSetting setting;
    }
 
}
