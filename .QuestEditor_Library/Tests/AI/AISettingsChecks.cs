using QuestEditor_Library;
using Verse;

internal static class AISettingsChecks
{
    public static void Run()
    {
        CustomQuestFramework_ModSetting previous = CustomQuestFramework_ModSetting.setting;
        string path = Path.GetTempFileName();
        int errors = ChecksLogHandler.ErrorCount;
        try
        {
            Check(!new CustomQuestFramework_ModSetting().dialogAIAdditionalPromptEnabled && string.IsNullOrEmpty(CustomQuestFramework_ModSetting.setting.dialogAIAdditionalPrompt),
                "additional instructions default disabled and empty");
            Check(CustomQuestFramework_ModSetting.setting.dialogAIPlanning && CustomQuestFramework_ModSetting.setting.dialogAIAgents && CustomQuestFramework_ModSetting.setting.dialogAIParallelAgents == 2
                && CustomQuestFramework_ModSetting.setting.dialogAIAgentModel.Length == 0 && CustomQuestFramework_ModSetting.setting.dialogAIAgentPrompt.Length == 0,
                "goals and workers default enabled with two concurrent workers and no extra model or role preferences");
            foreach (bool editing in new[] { false, true })
                foreach (bool text in new[] { false, true })
                foreach (bool tools in new[] { false, true })
                {
                    CustomQuestFramework_ModSetting setting = new CustomQuestFramework_ModSetting
                    {
                        dialogAIAllowEditing = editing,
                        dialogAIAllowTextGeneration = text,
                        dialogAIUseTools = tools,
                        dialogAIPlanning = editing,
                        dialogAIAgents = tools,
                        dialogAIParallelAgents = 3,
                        dialogAITokenBudget = 80000,
                        dialogAIRequestBudget = 18,
                        dialogAIExecutionSpeed = 2,
                        dialogAIAgentModel = "CQF_Check_WorkerModel",
                        dialogAIAgentPrompt = "CQF_Check_分工\nCQF_Check_<worker>&",
                        dialogAIAdditionalPromptEnabled = tools,
                        dialogAIAdditionalPrompt = "CQF_Check_附加提示词\nCQF_Check_<tag>&text"
                    };
                    Scribe.saver.InitSaving(path, "settings");
                    setting.ExposeData();
                    Scribe.saver.FinalizeSaving();
                    CustomQuestFramework_ModSetting loaded = new CustomQuestFramework_ModSetting
                    {
                        dialogAIAllowEditing = !editing,
                        dialogAIAllowTextGeneration = !text,
                        dialogAIUseTools = !tools,
                        dialogAIAdditionalPromptEnabled = !tools
                    };
                    Scribe.loader.InitLoading(path);
                    loaded.ExposeData();
                    Scribe.loader.FinalizeLoading();
                    Check(loaded.dialogAIAllowEditing == editing, "editing preference persists through native Scribe: " + editing + "/" + text);
                    Check(loaded.dialogAIAllowTextGeneration == text, "text permission persists through native Scribe: " + editing + "/" + text);
                    Check(loaded.dialogAIUseTools == tools, "tool transport preference persists through native Scribe: " + tools);
                    Check(loaded.dialogAITokenBudget == 80000 && loaded.dialogAIRequestBudget == 18 && loaded.dialogAIExecutionSpeed == 2,
                        "shared cost limits and execution speed persist through native Scribe");
                    Check(loaded.dialogAIPlanning == editing && loaded.dialogAIAgents == tools && loaded.dialogAIParallelAgents == 3 && loaded.dialogAIAgentModel == setting.dialogAIAgentModel
                        && loaded.dialogAIAgentPrompt.Replace("\r\n", "\n") == setting.dialogAIAgentPrompt, "orchestration preferences and worker instructions persist through native Scribe: " + tools + "/" + editing);
                    Check(loaded.dialogAIAdditionalPromptEnabled == tools && loaded.dialogAIAdditionalPrompt.Replace("\r\n", "\n") == setting.dialogAIAdditionalPrompt,
                        "additional instructions preserve enable state, Unicode, line content and XML characters through native Scribe: " + tools);
                }
            Check(ChecksLogHandler.ErrorCount == errors, "AI settings serialization reports no errors");
        }
        finally
        {
            Scribe.ForceStop();
            CustomQuestFramework_ModSetting.setting = previous;
            File.Delete(path);
        }
    }

    private static void Check(bool value, string name)
    {
        if (!value) throw new InvalidOperationException(name);
        Console.WriteLine("PASS " + name);
    }
}
