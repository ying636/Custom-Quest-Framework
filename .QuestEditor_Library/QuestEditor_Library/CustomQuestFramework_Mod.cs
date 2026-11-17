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
            this.alwaysLoadedModulesAtStartup = new HashSet<string>(this.setting.alwaysLoadedModules, StringComparer.Ordinal);
            CQFContentPaths.Initialize(content.RootDir);
            this.editorEnabledAtStartup = this.setting.enableEditor;
            CQFConditionalLoadFolders folders = new CQFConditionalLoadFolders(content, this.setting);
            CQFModuleLoader.Load(content, LoadedModManager.RunningModsListForReading, this.setting.alwaysLoadedModules);
            CQFEditorLoader.Load(content, folders.GetFolders(nameof(CustomQuestFramework_ModSetting.enableEditor)));
            if (!CQFEditorBridge.IsLoaded) folders.Exclude(nameof(CustomQuestFramework_ModSetting.enableEditor));
        }
        public override string SettingsCategory()
        {
            return "Custom Quest Framework";
        }
        public override void DoSettingsWindowContents(Rect inRect)
        {
            Rect view = new Rect(0f, 0f, inRect.width - 18f, Mathf.Max(inRect.height, this.settingsContentHeight));
            Widgets.BeginScrollView(inRect, ref this.settingsScrollPosition, view);
            this.DrawSettings(view);
            Widgets.EndScrollView();
        }

        private void DrawSettings(Rect inRect)
        {
            using CQFUIScope cqfUIScope = new CQFUIScope(inRect.width, inRect.height);
            bool previous = this.setting.enableEditor;
            float y = inRect.y;
            Widgets.CheckboxLabeled(new Rect(inRect.x, y, inRect.width, 30f), "CQF_Editor_Enable".Translate(), ref this.setting.enableEditor);
            if (previous != this.setting.enableEditor) this.setting.Write();
            y += 38f;
            string hint = "CQF_Editor_EnableHint".Translate();
            float hintHeight = Text.CalcHeight(hint, inRect.width);
            Widgets.Label(new Rect(inRect.x, y, inRect.width, hintHeight), hint);
            y += hintHeight + 12f;
            if (this.setting.enableEditor != this.editorEnabledAtStartup
                || !this.alwaysLoadedModulesAtStartup.SetEquals(this.setting.alwaysLoadedModules))
            {
                string restart = "CQF_Editor_RestartRequired".Translate();
                float restartHeight = Text.CalcHeight(restart, inRect.width);
                Widgets.Label(new Rect(inRect.x, y, inRect.width, restartHeight), restart.Colorize(ColorLibrary.Yellow));
                y += restartHeight + 12f;
            }
            Widgets.Label(new Rect(inRect.x, y, inRect.width, 30f), "CQF_Settings_AlwaysLoadSystems".Translate().Colorize(CQFUIStyle.Accent));
            y += 38f;
            foreach (string module in CQFModuleLoader.AvailableModules)
            {
                string key = "CQF_Module_" + module;
                string label = key.CanTranslate() ? key.Translate().ToString() : module;
                bool alwaysLoad = this.setting.alwaysLoadedModules.Contains(module);
                bool wasAlwaysLoaded = alwaysLoad;
                Rect row = new Rect(inRect.x, y, inRect.width, 30f);
                Widgets.CheckboxLabeled(row, "CQF_Settings_AlwaysLoadSystem".Translate(label), ref alwaysLoad);
                TooltipHandler.TipRegion(row, "CQF_Editor_RestartRequired".Translate());
                if (alwaysLoad != wasAlwaysLoaded)
                {
                    if (alwaysLoad) this.setting.alwaysLoadedModules.Add(module);
                    else this.setting.alwaysLoadedModules.RemoveAll(name => name == module);
                    this.setting.Write();
                }
                y += 38f;
            }
            Rect intervalRow = new Rect(inRect.x, y, inRect.width, 30f);
            float inputWidth = Mathf.Min(160f, inRect.width * 0.4f);
            Widgets.Label(new Rect(intervalRow.x, intervalRow.y, intervalRow.width - inputWidth - 12f, 30f), "CQF_Settings_QuestBookCheckInterval".Translate());
            int previousInterval = this.setting.questBookCheckInterval;
            Widgets.TextFieldNumeric<int>(new Rect(intervalRow.xMax - inputWidth, intervalRow.y, inputWidth, 30f),
                ref this.setting.questBookCheckInterval, ref this.questBookCheckIntervalBuffer, 1f, int.MaxValue);
            if (previousInterval != this.setting.questBookCheckInterval) this.setting.Write();
            y += 42f;
            if (CQFEditorLoader.LoadError != null)
            {
                string error = "CQF_Editor_LoadFailed".Translate();
                float errorHeight = Text.CalcHeight(error, inRect.width);
                Widgets.Label(new Rect(inRect.x, y, inRect.width, errorHeight), error.Colorize(ColorLibrary.RedReadable));
                y += errorHeight + 12f;
            }
            this.settingsContentHeight = y;
            if (!CQFEditorBridge.IsLoaded) return;
            if (CQFUIStyle.ButtonText(new Rect(inRect.x, y, Mathf.Min(260f, inRect.width), 32f), "CQF_AI_Settings".Translate())) CQFEditorBridge.Module!.OpenSettings();
            y += 40f;
            Widgets.CheckboxLabeled(new Rect(inRect.x, y, inRect.width, 30f), "AutoCompileDialogTextKey".Translate(), ref this.setting.autoCompileDialogTextKey);
            this.settingsContentHeight = y + 38f;
        }
        public CustomQuestFramework_ModSetting setting = null;
        private readonly bool editorEnabledAtStartup;
        private readonly HashSet<string> alwaysLoadedModulesAtStartup;
        private Vector2 settingsScrollPosition;
        private float settingsContentHeight;
        private string questBookCheckIntervalBuffer = string.Empty;
    }

    public class CustomQuestFramework_ModSetting : ModSettings
    {
        public CustomQuestFramework_ModSetting()
        {
            CustomQuestFramework_ModSetting.setting = this;
        }
        public int QuestBookCheckInterval => Math.Max(1, this.questBookCheckInterval);

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref this.enableEditor, "enableEditor", false);
            Scribe_Collections.Look(ref this.alwaysLoadedModules, "alwaysLoadedModules", LookMode.Value);
            Scribe_Values.Look(ref this.questBookCheckInterval, "questBookCheckInterval", GenDate.TicksPerDay);
            this.alwaysLoadedModules ??= new List<string>();
            this.questBookCheckInterval = Math.Max(1, this.questBookCheckInterval);
            Scribe_Values.Look(ref this.autoCompileDialogTextKey, "autoCompileDialogTextKey", true);
            Scribe_Values.Look(ref this.dialogAIEnabled, "dialogAIEnabled", false);
            Scribe_Values.Look(ref this.dialogAIAllowEditing, "dialogAIAllowEditing", true);
            Scribe_Values.Look(ref this.dialogAIAllowTextGeneration, "dialogAIAllowTextGeneration", true);
            Scribe_Values.Look(ref this.dialogAIUseTools, "dialogAIUseTools", true);
            Scribe_Values.Look(ref this.dialogAIPlanning, "dialogAIPlanning", true);
            Scribe_Values.Look(ref this.dialogAIAgents, "dialogAIAgents", true);
            Scribe_Values.Look(ref this.dialogAIExecutionSpeed, "dialogAIExecutionSpeed", 1);
            Scribe_Values.Look(ref this.dialogAITokenBudget, "dialogAITokenBudget", 100000);
            Scribe_Values.Look(ref this.dialogAIRequestBudget, "dialogAIRequestBudget", 24);
            Scribe_Values.Look(ref this.dialogAIParallelAgents, "dialogAIParallelAgents", 2);
            Scribe_Values.Look(ref this.dialogAIAgentModel, "dialogAIAgentModel", string.Empty);
            Scribe_Values.Look(ref this.dialogAIAgentPrompt, "dialogAIAgentPrompt", string.Empty);
            Scribe_Values.Look(ref this.dialogAIAdditionalPromptEnabled, "dialogAIAdditionalPromptEnabled", false);
            Scribe_Values.Look(ref this.dialogAIAdditionalPrompt, "dialogAIAdditionalPrompt", string.Empty);
            Scribe_Values.Look(ref this.dialogAIEndpoint, "dialogAIEndpoint", string.Empty);
            Scribe_Values.Look(ref this.dialogAIModel, "dialogAIModel", string.Empty);
            Scribe_Values.Look(ref this.dialogAIKey, "dialogAIKey", string.Empty);
            Scribe_Values.Look(ref this.dialogAITimeout, "dialogAITimeout", 120);
            Scribe_Collections.Look(ref this.recentSpawnThings, "CQF_recentSpawnThings", LookMode.Deep);
            Scribe_Collections.Look(ref this.recentCQFTools, "CQF_recentCQFTools", LookMode.Deep);
            Scribe_Collections.Look(ref this.recentTerrains, "CQF_recentTerrains", LookMode.Value);
            this.recentSpawnThings ??= new List<CQFPaletteSelection>();
            this.recentCQFTools ??= new List<CQFPaletteSelection>();
            this.recentTerrains ??= new List<string>();
        }

        public bool enableEditor;
        public List<string> alwaysLoadedModules = new List<string>();
        public int questBookCheckInterval = GenDate.TicksPerDay;
        public bool autoCompileDialogTextKey = true;
        public bool dialogAIEnabled;
        public bool dialogAIAllowEditing = true;
        public bool dialogAIAllowTextGeneration = true;
        public bool dialogAIUseTools = true;
        public bool dialogAIPlanning = true;
        public bool dialogAIAgents = true;
        public int dialogAIExecutionSpeed = 1;
        public int dialogAITokenBudget = 100000;
        public int dialogAIRequestBudget = 24;
        public int dialogAIParallelAgents = 2;
        public string dialogAIAgentModel = string.Empty;
        public string dialogAIAgentPrompt = string.Empty;
        public bool dialogAIAdditionalPromptEnabled;
        public string dialogAIAdditionalPrompt = string.Empty;
        public string dialogAIEndpoint = string.Empty;
        public string dialogAIModel = string.Empty;
        public string dialogAIKey = string.Empty;
        public int dialogAITimeout = 120;
        public List<CQFPaletteSelection> recentSpawnThings = new List<CQFPaletteSelection>();
        public List<CQFPaletteSelection> recentCQFTools = new List<CQFPaletteSelection>();
        public List<string> recentTerrains = new List<string>();
        public static CustomQuestFramework_ModSetting setting;
    }

}
