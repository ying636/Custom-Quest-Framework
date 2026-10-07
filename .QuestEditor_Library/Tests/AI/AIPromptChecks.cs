using System.Xml.Linq;
using QuestEditor_Library;

internal static class AIPromptChecks
{
    public static void Run(CQFAIModel model, CQFAIResourceCatalog resources)
    {
        CQFAIConversation conversation = new(model, resources);
        conversation.Add("user", "CQF_Check_Request");
        AIFakeLiveMap map = new(model);
        IEnumerable<CQFAITarget> Discover()
        {
            yield return new CQFAITarget(map, "CQF_Check_PromptMap", typeof(CQFAILiveMapInfo).FullName!, "current_map", () => CQFAILiveMapContext.Create(map), () => map.IsValid);
        }
        string prompt = "CQF_Check_CustomPrompt\nCQF_Check_第二行";
        CQFAIHarness harness = new(model, resources, conversation, null, "", true, true, true, new CQFAITargetCatalog(Discover), prompt);
        Check(!harness.Instructions.Contains(prompt) && harness.RequestMessages.Count(message => message.Role == "system" && message.Content == prompt) == 1,
            "additional instructions are a separate request message instead of embedded base instructions");
        Check(conversation.Messages.All(message => message.Content != prompt), "additional instructions do not become stored conversation history");
        harness.Process(AIToolChecks.Response("CQF_Check_PromptDiscover", "cqf_list_targets"));
        string targetId = harness.LastResults.Single().Descendants("target").Single().Attribute("id")!.Value;
        harness.Process(AIToolChecks.Response("CQF_Check_PromptSelect", "cqf_select_target", ("target_id", targetId)));
        Check(harness.Instructions.Contains("0=North") && !harness.Instructions.Contains("central gathering space") && !harness.Instructions.Contains("For a wooden house"),
            "selecting a map retains tool orientation documentation without injecting settlement or house design preferences");
        Check(!harness.Instructions.Contains(prompt) && harness.RequestMessages.Count(message => message.Content == prompt) == 1,
            "additional instructions remain separate and appear once after target changes and tool rounds");
        CustomQuestFramework_ModSetting setting = CustomQuestFramework_ModSetting.setting;
        bool previousEnabled = setting.dialogAIAdditionalPromptEnabled;
        string previousPrompt = setting.dialogAIAdditionalPrompt;
        try
        {
            setting.dialogAIAdditionalPromptEnabled = false; setting.dialogAIAdditionalPrompt = prompt;
            CQFAIConversation disabledConversation = new(model, resources);
            disabledConversation.Add("user", "CQF_Check_NextRequest");
            CQFAIHarness disabled = new(model, resources, disabledConversation, null, "", true, true,
                additionalPrompt: setting.dialogAIAdditionalPromptEnabled ? setting.dialogAIAdditionalPrompt : string.Empty);
            Check(disabled.RequestMessages.All(message => message.Content != prompt) && !disabled.Instructions.Contains(prompt),
                "disabled additional instructions are absent from every part of the request even when their text is saved");
            Check(harness.RequestMessages.Count(message => message.Content == prompt) == 1,
                "a running task retains its instruction snapshot while later requests use changed settings");
            CQFAIHarness blank = new(model, resources, new CQFAIConversation(model, resources), null, "", false, false, additionalPrompt: " \n\t ");
            Check(!blank.RequestMessages.Any(), "blank enabled instructions do not add an empty system message");
            try
            {
                _ = new CQFAIHarness(model, resources, new CQFAIConversation(model, resources), null, "", false, false, additionalPrompt: new string('x', 16001));
                throw new InvalidOperationException("oversized prompt accepted");
            }
            catch (InvalidDataException error) when (error.Message == "CQF_AI_PromptTooLong") { Check(true, "oversized additional instructions report a specific error before a request starts"); }
        }
        finally { setting.dialogAIAdditionalPromptEnabled = previousEnabled; setting.dialogAIAdditionalPrompt = previousPrompt; }
    }

    private static void Check(bool value, string name)
    {
        if (!value) throw new InvalidOperationException(name);
        Console.WriteLine("PASS " + name);
    }
}
