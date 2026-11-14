using System.Reflection;
using System.Runtime.CompilerServices;
using System.Xml;
using HarmonyLib;
using QuestEditor_Library;
using RimWorld;
using Verse;

internal static class NotificationDefaultDefChecks
{
    public static void Run()
    {
        FieldInfo prefs = AccessTools.Field(typeof(Prefs), "data");
        prefs.SetValue(null, RuntimeHelpers.GetUninitializedObject(prefs.FieldType));
        ModContentPack content = (ModContentPack)RuntimeHelpers.GetUninitializedObject(typeof(ModContentPack));
        content.assemblies = new ModAssemblyHandler(content);
        content.assemblies.loadedAssemblies.Add(typeof(CQFAction_Letter).Assembly);
        LoadedModManager.RunningModsListForReading.Add(content);
        GenTypes.ClearCache();
        Harmony harmony = new("CQF.Checks.NotificationDefaults");
        MethodInfo initialization = AccessTools.Method(typeof(DefOfHelper), nameof(DefOfHelper.EnsureInitializedInCtor));
        harmony.Patch(initialization, prefix: new HarmonyMethod(typeof(NotificationDefaultDefChecks), nameof(RecordDefOfInitialization)));
        try
        {
            CQFAction_Letter letter = new();
            CQFAction_Message message = new();
            Check(defOfInitializations == 0, "notification constructors leave DefOf types uninitialized");
            XmlDocument document = new();
            document.LoadXml("<li><actions><li Class=\"QuestEditor_Library.CQFAction_Letter\" /><li Class=\"QuestEditor_Library.CQFAction_Message\" /></actions></li>");
            InteractionResult result = DirectXmlToObject.ObjectFromXml<InteractionResult>(document.DocumentElement!, false);
            Check(result.actions.Count == 2 && result.actions[0] is CQFAction_Letter && result.actions[1] is CQFAction_Message,
                "native XML creates both notification actions inside interaction results");
            Check(defOfInitializations == 0, "nested XML loading does not access notification DefOf types");
            CQFAction_Letter nestedLetter = (CQFAction_Letter)result.actions[0];
            CQFAction_Message nestedMessage = (CQFAction_Message)result.actions[1];
            Check(letter.letterDef == null && nestedLetter.letterDef == null && message.type == null && nestedMessage.type == null,
                "omitted notification types remain unresolved during XML loading");

            LetterDef explicitLetter = new() { defName = "CQF_Check_ExplicitLetter", letterClass = typeof(StandardLetter) };
            MessageTypeDef explicitMessage = new() { defName = "CQF_Check_ExplicitMessage" };
            DefDatabase<LetterDef>.Add(explicitLetter);
            DefDatabase<MessageTypeDef>.Add(explicitMessage);
            document.LoadXml("<li><actions><li Class=\"QuestEditor_Library.CQFAction_Letter\"><letterDef>CQF_Check_ExplicitLetter</letterDef></li><li Class=\"QuestEditor_Library.CQFAction_Message\"><type>CQF_Check_ExplicitMessage</type></li></actions></li>");
            InteractionResult configured = DirectXmlToObject.ObjectFromXml<InteractionResult>(document.DocumentElement!, false);
            DirectXmlCrossRefLoader.ResolveAllWantedCrossReferences(FailMode.LogErrors);
            CQFAction_Letter configuredLetter = (CQFAction_Letter)configured.actions[0];
            CQFAction_Message configuredMessage = (CQFAction_Message)configured.actions[1];
            Check(configuredLetter.letterDef == explicitLetter && configuredMessage.type == explicitMessage && defOfInitializations == 0,
                "explicit XML types resolve without consulting notification DefOf types");

            LetterDef neutral = new() { defName = "NeutralEvent", letterClass = typeof(StandardLetter) };
            MessageTypeDef positive = new() { defName = "PositiveEvent" };
            DefDatabase<LetterDef>.Add(neutral);
            DefDatabase<MessageTypeDef>.Add(positive);
            FieldInfo binding = AccessTools.Field(typeof(DefOfHelper), "bindingNow");
            bool previousBinding = (bool)binding.GetValue(null)!;
            binding.SetValue(null, true);
            try
            {
                typeof(LetterDefOf).GetField(nameof(LetterDefOf.NeutralEvent))!.SetValue(null, neutral);
                typeof(MessageTypeDefOf).GetField(nameof(MessageTypeDefOf.PositiveEvent))!.SetValue(null, positive);
            }
            finally
            {
                binding.SetValue(null, previousBinding);
            }

            Check(letter.LetterDef == neutral && nestedLetter.LetterDef == neutral, "letters obtain NeutralEvent after binding");
            Check(message.MessageType == positive && nestedMessage.MessageType == positive, "messages obtain PositiveEvent after binding");
            Check(configuredLetter.LetterDef == explicitLetter && configuredMessage.MessageType == explicitMessage,
                "configured notification types override the runtime defaults");
            Check(letter.SaveToXElement("li").Element("letterDef")?.Value == neutral.defName &&
                message.SaveToXElement("li").Element("type")?.Value == positive.defName, "XML export preserves effective notification defaults");
            Check(((CQFAction_Letter)letter.Copy()).letterDef == neutral && ((CQFAction_Message)message.Copy()).type == positive,
                "default notification types survive native XML copy and cross-reference resolution");
            Check(((CQFAction_Letter)configuredLetter.Copy()).letterDef == explicitLetter && ((CQFAction_Message)configuredMessage.Copy()).type == explicitMessage,
                "explicit notification types survive native XML copy and cross-reference resolution");
            configuredLetter.letterDef = null;
            configuredMessage.type = null;
            Check(configuredLetter.LetterDef == neutral && configuredMessage.MessageType == positive, "clearing notification types restores the defaults");
            Console.WriteLine("Notification default-def checks passed.");
        }
        finally
        {
            harmony.Unpatch(initialization, HarmonyPatchType.Prefix, harmony.Id);
            LoadedModManager.RunningModsListForReading.Remove(content);
            GenTypes.ClearCache();
        }
    }

    private static void RecordDefOfInitialization(Type defOf)
    {
        if (defOf == typeof(LetterDefOf) || defOf == typeof(MessageTypeDefOf)) defOfInitializations++;
    }

    private static void Check(bool passed, string name)
    {
        if (!passed) throw new InvalidOperationException(name);
        Console.WriteLine("PASS " + name);
    }

    private static int defOfInitializations;
}
