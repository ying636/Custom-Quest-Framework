using System.Reflection;
using System.Runtime.CompilerServices;
using System.Xml.Linq;
using QuestEditor_Library;

internal static class Program
{
    private static void Main(string[] args)
    {
        string game = args[0];
        AppDomain.CurrentDomain.AssemblyResolve += (_, request) =>
        {
            string path = Path.Combine(game, "RimWorldWin64_Data", "Managed", new AssemblyName(request.Name).Name + ".dll");
            return File.Exists(path) ? Assembly.LoadFrom(path) : null;
        };
        try { Run(args[1]); }
        catch (Exception error) { Console.WriteLine(error); Environment.ExitCode = 1; }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void Run(string output)
    {
        Type? Resolve(string name) => typeof(CustomMapDataDef).Assembly.GetType(name);
        string? Translate(string key) => key == "CQF_ExistingText" ? "existing text" : null;
        CQFContentTextExport export = new CQFContentTextExport("CQF_TextChecks", Translate, Resolve);
        InteractionOperation operation = new InteractionOperation { interactionText = "Original body", outputSignal = "Signal.Original" };
        CQFSignalEditor.RenameInteraction(operation, "Changed body\nwith more than twenty eight characters");
        Require(operation.OutputSignal == "Signal.Original", "Editing text renamed the output signal.");
        XElement operationXml = operation.SaveToXElement("li");
        Require(operationXml.Element("interactionText")!.Value == operation.interactionText && operationXml.Element("outputSignal")!.Value == "Signal.Original", "Operation serialization lost the independent signal.");
        InteractionOperation implicitSignal = new InteractionOperation { interactionText = "Original signal" };
        CQFSignalEditor.RenameInteraction(implicitSignal, "New body");
        Require(implicitSignal.OutputSignal == "Original signal", "Editing implicit signal text lost existing references.");
        Console.WriteLine("PASS interaction body edits and serialization preserve output signals, long text and newlines");
        XElement source = XElement.Parse("""
            <QuestEditor_Library.CustomMapDataDef>
              <defName>CQF_TextChecks</defName><label>Map label</label><description>Map description</description>
              <customThings>
                <li Class="QuestEditor_Library.CustomThingData_InteractableThing">
                  <def>QE_InteractableThing</def><customName>Object name</customName>
                  <customDescription>Line one
            Line two &amp; &lt;tag&gt;</customDescription><customInspectText>Inspect {PAWN_label}</customInspectText>
                  <targetKeys><li>Target.One</li></targetKeys>
                  <operations><li><interactionText>Interact {0}</interactionText><outputSignal>Signal.Original</outputSignal>
                    <conditions><li Class="QuestEditor_Library.DialogCondition_And"><failReason>Outer failure</failReason>
                      <condition><li Class="QuestEditor_Library.DialogCondition_Bool"><boolName>Bool.ID</boolName><failReason>Inner failure</failReason></li></condition>
                    </li></conditions>
                    <results><li><resultName>Result.ID</resultName><actions>
                      <li Class="QuestEditor_Library.CQFAction_Message"><message>Message {PAWN_label} &amp; {0}</message></li>
                      <li Class="QuestEditor_Library.CQFAction_SetCustomName"><targetText>Target.One</targetText><text>New name</text></li>
                      <li Class="QuestEditor_Library.CQFAction_UpgradeTrait"><initMessage>Initial {0}</initMessage><message>Upgrade {1}</message></li>
                      <li Class="QuestEditor_Library.CQFAction_SetCustomHediff"><label>Effect name</label><desc>Effect description</desc></li>
                      <li Class="QuestEditor_Library.CQFAction_AddQuestDescription"><description>Quest detail</description></li>
                      <li Class="QuestEditor_Library.CQFAction_EndGame"><message>Ending</message></li>
                      <li Class="QuestEditor_Library.CQFAction_SentSignal"><signal>Signal.Original</signal></li>
                    </actions></li></results>
                  </li><li><interactionText>Unspecified signal body</interactionText></li><li><interactionText>Empty signal body</interactionText><outputSignal /></li></operations>
                </li>
                <li Class="QuestEditor_Library.CustomThingData_LootBox"><lootBoxName>Loot.ID</lootBoxName><openReport>Opening</openReport>
                  <loots><li><dataName>Reward.ID</dataName><message>Loot {0}</message><pawnDatas>
                    <li Class="QuestEditor_Library.PawnSpawnData"><spawnMessage>Pawn appeared</spawnMessage><lordDataName>Lord.ID</lordDataName></li>
                  </pawnDatas></li><li><message>   </message></li></loots>
                </li>
                <li Class="QuestEditor_Library.CustomThingData_CustomTrap"><trapName>Trap.ID</trapName><disarmReport>Disarm {0}</disarmReport></li>
                <li Class="QuestEditor_Library.CustomThingData_CustomMapExit"><exitName>Exit.ID</exitName></li>
              </customThings>
              <pawns><li><key>(1, 0, 2)</key><value><li Class="QuestEditor_Library.PawnSpawnData_Faction"><spawnMessage>Faction pawn</spawnMessage></li></value></li></pawns>
              <generationActions><li><actions><li Class="QuestEditor_Library.CQFAction_Message"><message>CQF_ExistingText</message></li></actions></li></generationActions>
            </QuestEditor_Library.CustomMapDataDef>
            """, LoadOptions.PreserveWhitespace);
        string original = source.ToString();
        XElement compiled = export.Compile(source, typeof(CustomMapDataDef));
        Require(source.ToString() == original, "Export mutated the editor source.");
        string[] texts = { "Object name", "Inspect {PAWN_label}", "Interact {0}", "Outer failure", "Inner failure",
            "Message {PAWN_label} & {0}", "New name", "Initial {0}", "Upgrade {1}", "Effect name", "Effect description",
            "Quest detail", "Ending", "Opening", "Loot {0}", "Pawn appeared", "Faction pawn", "Disarm {0}", "Unspecified signal body", "Empty signal body" };
        foreach (string text in texts)
            Require(export.Keyed.Elements().Any(entry => entry.Value == text), "Missing exported text: " + text);
        Require(export.Keyed.Elements().All(entry => entry.Name.LocalName.StartsWith("CQF_")), "Missing CQF key prefix.");
        Require(export.Keyed.Elements().Select(entry => entry.Name.LocalName).Distinct().Count() == export.Keyed.Elements().Count(), "Duplicate keys.");
        foreach (string field in new[] { "def", "defName", "targetText", "boolName", "resultName", "dataName", "lootBoxName", "trapName", "exitName", "lordDataName", "signal", "targetKeys", "key" })
            Require(source.Descendants(field).Select(entry => entry.Value).SequenceEqual(compiled.Descendants(field).Select(entry => entry.Value)), "Identifier changed: " + field);
        Require(compiled.Descendants("outputSignal").Select(entry => entry.Value).SequenceEqual(new[] { "Signal.Original", "Unspecified signal body", "Empty signal body" }), "Interaction signal changed during text compilation.");
        Require(compiled.Element("label")!.Value == "Map label" && export.DefInjected.Element("CQF_TextChecks.label")!.Value == "Map label", "Native Def label was replaced with a Keyed key.");
        Require(compiled.Descendants("message").Any(entry => entry.Value == "CQF_ExistingText"), "Existing key was rewritten.");
        Require(!export.Keyed.Elements().Any(entry => entry.Name.LocalName == "CQF_ExistingText"), "External translation was duplicated.");
        Require(compiled.Descendants("message").Any(entry => entry.Value == "   "), "Blank text was changed.");
        Console.WriteLine("PASS nested map texts, identifiers, inherited fields, dictionaries, native Def texts and editor source preservation");

        string languageBefore = export.Keyed.ToString();
        Require(export.Compile(source, typeof(CustomMapDataDef)).ToString() == compiled.ToString() && export.Keyed.ToString() == languageBefore, "Repeated compilation changed keys.");
        Console.WriteLine("PASS stable repeated compilation");

        Directory.CreateDirectory(output);
        string mapPath = Path.Combine(output, "CQF_TextChecks.xml");
        export.Save(source, typeof(CustomMapDataDef), mapPath, true);
        XElement language = XElement.Load(Path.Combine(output, "CQF_TextChecks_Text.xml"));
        Require(language.Elements().Any(entry => entry.Value.Contains("Line two & <tag>")), "XML escaping or multiline text changed.");
        XElement reloaded = XElement.Load(mapPath).Elements().Single();
        new CQFContentTextExport("CQF_TextChecks", Translate, Resolve).Save(reloaded, typeof(CustomMapDataDef), mapPath, true);
        Require(XNode.DeepEquals(language, XElement.Load(Path.Combine(output, "CQF_TextChecks_Text.xml"))), "Re-export erased existing translations.");
        Console.WriteLine("PASS file export, XML escaping, multiline text and re-exported translation preservation");

        XElement interaction = XElement.Parse("<QuestEditor_Library.InteractionDataDef><defName>CQF_InteractionCheck</defName><interactions><li><interactionText>Interaction body</interactionText></li></interactions></QuestEditor_Library.InteractionDataDef>");
        CQFContentTextExport interactionExport = new CQFContentTextExport("CQF_InteractionCheck", Translate, Resolve);
        interactionExport.Save(interaction, typeof(InteractionDataDef), Path.Combine(output, "CQF_InteractionCheck.xml"), false);
        Require(interactionExport.Keyed.Elements().Single().Value == "Interaction body", "Interaction Def text missing.");
        Require(!File.Exists(Path.Combine(output, "CQF_InteractionCheck_DefInjected.xml")), "English export created DefInjected.");
        XElement loot = XElement.Parse("<QuestEditor_Library.LootDataDef><defName>CQF_LootCheck</defName><loots><li><message>Reward {0}</message><dataName>Reward.ID</dataName></li></loots></QuestEditor_Library.LootDataDef>");
        CQFContentTextExport lootExport = new CQFContentTextExport("CQF_LootCheck", Translate, Resolve);
        lootExport.Save(loot, typeof(LootDataDef), Path.Combine(output, "CQF_LootCheck.xml"), false);
        Require(lootExport.Keyed.Elements().Single().Value == "Reward {0}", "Loot Def text missing.");
        Console.WriteLine("PASS standalone interaction/loot Def exports and English DefInjected handling");

        XElement dialog = XElement.Parse("""
            <QuestEditor_Library.DialogTreeDef><defName>CQF_DialogCheck</defName><title>CQF_Dialog_CQF_DialogCheck_Title</title>
              <optionMoulds><li><key>4</key><value Class="QuestEditor_Library.DialogOption"><text>CQF_Dialog_CQF_DialogCheck_Option_4_Text</text>
                <conditions><li Class="QuestEditor_Library.DialogCondition_Bool"><failReason>Dialogue failure</failReason><boolName>Bool.ID</boolName></li></conditions>
                <results><li><resultName>Result.ID</resultName><actions><li Class="QuestEditor_Library.CQFAction_Message"><message>Dialogue action</message></li></actions></li></results>
              </value></li></optionMoulds>
            </QuestEditor_Library.DialogTreeDef>
            """);
        XElement dialogueLanguage = XElement.Parse("<LanguageData><CQF_Dialog_CQF_DialogCheck_Title>Dialogue title</CQF_Dialog_CQF_DialogCheck_Title><CQF_Dialog_CQF_DialogCheck_Option_4_Text>Dialogue option</CQF_Dialog_CQF_DialogCheck_Option_4_Text></LanguageData>");
        string dialogPath = Path.Combine(output, "CQF_DialogCheck.xml");
        CQFContentTextExport dialogExport = new CQFContentTextExport("CQF_DialogCheck", Translate, Resolve);
        dialogExport.Save(dialog, typeof(DialogTreeDef), dialogPath, false, dialogueLanguage);
        Require(dialogExport.Keyed.Elements().Count() == 4 && dialogExport.Keyed.Elements().Any(entry => entry.Value == "Dialogue failure") && dialogExport.Keyed.Elements().Any(entry => entry.Value == "Dialogue action"), "Dialogue embedded texts or existing language entries missing.");
        Require(XElement.Load(dialogPath).Descendants("key").Single().Value == "4", "Dialogue option ID changed.");
        XElement dialogueDocument = XElement.Load(dialogPath).Elements().Single();
        new CQFContentTextExport("CQF_DialogCheck", Translate, Resolve).Save(dialogueDocument, typeof(DialogTreeDef), dialogPath, false, new XElement("LanguageData"));
        Require(XNode.DeepEquals(dialogExport.Keyed, XElement.Load(Path.Combine(output, "CQF_DialogCheck_Text.xml"))), "Re-export lost referenced dialogue translations.");

        XElement book = XElement.Parse("""
            <QuestEditor_Library.QuestBookDef><defName>CQF_BookCheck</defName><label>Book label</label>
              <chapters><li><id>chapter_1</id><labelKey>CQF_QuestBook_CQF_BookCheck_Chapter_chapter_1_Label</labelKey>
                <steps><li><id>step_1</id><rewardInfos><li><labelKey>Reward label</labelKey><descriptionKey>Reward detail</descriptionKey><iconPath>UI/Reward</iconPath></li></rewardInfos>
                  <onCompleteActions><li Class="QuestEditor_Library.CQFAction_Message"><message>Book action</message></li></onCompleteActions>
                  <objectives><li Class="QuestEditor_Library.QuestBookObjective_Signal"><signal>Signal.Original</signal></li></objectives>
                </li></steps>
              </li></chapters>
            </QuestEditor_Library.QuestBookDef>
            """);
        XElement bookLanguage = XElement.Parse("<LanguageData><CQF_QuestBook_CQF_BookCheck_Chapter_chapter_1_Label>Chapter label</CQF_QuestBook_CQF_BookCheck_Chapter_chapter_1_Label></LanguageData>");
        CQFContentTextExport bookExport = new CQFContentTextExport("CQF_BookCheck", Translate, Resolve);
        string bookPath = Path.Combine(output, "CQF_BookCheck.xml");
        bookExport.Save(book, typeof(QuestBookDef), bookPath, false, bookLanguage);
        XElement bookDocument = XElement.Load(bookPath).Elements().Single();
        Require(bookExport.Keyed.Elements().Count() == 4 && bookExport.Keyed.Elements().Any(entry => entry.Value == "Reward label") && bookExport.Keyed.Elements().Any(entry => entry.Value == "Reward detail") && bookExport.Keyed.Elements().Any(entry => entry.Value == "Book action"), "Book reward display or embedded action text missing.");
        foreach (string field in new[] { "id", "signal", "iconPath" })
            Require(book.Descendants(field).Select(entry => entry.Value).SequenceEqual(bookDocument.Descendants(field).Select(entry => entry.Value)), "Book identifier changed: " + field);
        XElement bookKeyReferences = new XElement(bookLanguage);
        foreach (XElement entry in bookKeyReferences.Elements()) entry.Value = entry.Name.LocalName;
        new CQFContentTextExport("CQF_BookCheck", Translate, Resolve).Save(bookDocument, typeof(QuestBookDef), bookPath, false, bookKeyReferences);
        Require(bookExport.Keyed.ToString() == XElement.Load(Path.Combine(output, "CQF_BookCheck_Text.xml")).ToString(), "Re-export lost book reward translations.");
        Console.WriteLine("PASS dialogue/quest book embedded texts, reward display, existing language merge and identifier preservation");

        XElement invalid = new XElement(source);
        invalid.Descendants("li").First().SetAttributeValue("Class", "QuestEditor_Library.MissingExportClass");
        try { export.Compile(invalid, typeof(CustomMapDataDef)); throw new InvalidOperationException("Unknown class was hidden."); }
        catch (InvalidDataException) { }
        string badPath = Path.Combine(output, "CQF_Bad.xml");
        File.WriteAllText(Path.Combine(output, "CQF_Bad_Text.xml"), "<LanguageData><Duplicate>A</Duplicate><Duplicate>B</Duplicate></LanguageData>");
        try { export.Save(source, typeof(CustomMapDataDef), badPath, false); throw new InvalidOperationException("Duplicate translations were hidden."); }
        catch (InvalidDataException) { }
        Require(!File.Exists(badPath), "Failed export wrote a definition.");
        Console.WriteLine("PASS unknown classes and invalid translation files fail visibly before definition writes");

        Console.WriteLine("All content text export checks passed.");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
