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
            foreach (bool editing in new[] { false, true })
                foreach (bool text in new[] { false, true })
                foreach (bool tools in new[] { false, true })
                {
                    CustomQuestFramework_ModSetting setting = new CustomQuestFramework_ModSetting
                    {
                        dialogAIAllowEditing = editing,
                        dialogAIAllowTextGeneration = text,
                        dialogAIUseTools = tools
                    };
                    Scribe.saver.InitSaving(path, "settings");
                    setting.ExposeData();
                    Scribe.saver.FinalizeSaving();
                    CustomQuestFramework_ModSetting loaded = new CustomQuestFramework_ModSetting
                    {
                        dialogAIAllowEditing = !editing,
                        dialogAIAllowTextGeneration = !text,
                        dialogAIUseTools = !tools
                    };
                    Scribe.loader.InitLoading(path);
                    loaded.ExposeData();
                    Scribe.loader.FinalizeLoading();
                    Check(loaded.dialogAIAllowEditing == editing, "editing preference persists through native Scribe: " + editing + "/" + text);
                    Check(loaded.dialogAIAllowTextGeneration == text, "text permission persists through native Scribe: " + editing + "/" + text);
                    Check(loaded.dialogAIUseTools == tools, "tool transport preference persists through native Scribe: " + tools);
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
