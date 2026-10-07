using System.Xml;
using System.Xml.Linq;
using QuestEditor_Library;
using Verse;

internal static class DialogSharedOptionsChecks
{
    public static void Run(CQFAIModel model)
    {
        DialogTreeDef tree = new() { defName = "CQF_Check_Shared" };
        DialogNode first = tree.nodeMoulds[0];
        DialogNode second = tree.CreateNewNode(null);
        DialogNode third = tree.CreateNewNode(null);
        DialogOption close = new() { text = "CQF_Check_Close", editorX = 370f, editorY = 25f, editorPositionSet = true };
        tree.LinkOption(first, close); tree.LinkOption(second, close); tree.LinkOption(third, close);
        int id = tree.optionMoulds.Single().Key;
        Check(tree.optionMoulds.Count == 1 && first.options.Single() == second.options.Single() && third.optionIds.Single() == id,
            "three dialogue references share one canonical choice");
        tree.LinkOption(first, close);
        Check(first.options.Count == 1, "linking an existing reference does not duplicate its choice");
        close.text = "CQF_Check_Shared_Edit";
        close.conditions.Add(new DialogCondition_Chance());
        close.results[0].actions.Add(new CQFAction_Loop());
        close.results[0].nextIndex = second.index;
        tree.Update();
        Check(third.options[0].text == close.text && second.options[0].conditions.Count == 1 && first.options[0].results[0].actions.Count == 1,
            "shared text, conditions, actions and destinations update together");
        XElement xml = tree.SaveToXElement("QuestEditor_Library.DialogTreeDef");
        Check(xml.Element("optionMoulds")!.Elements("li").Count() == 1 && xml.Element("nodeMoulds")!.Descendants("optionIds").Count() == 3
            && !xml.Element("nodeMoulds")!.Descendants("options").Any(), "export stores one choice definition and ordered references");
        int errors = ChecksLogHandler.ErrorCount;
        DialogTreeDef restored = Parse(xml); restored.ResolveReferences();
        Check(restored.nodeMoulds[0].options[0] == restored.nodeMoulds[1].options[0] && restored.nodeMoulds[2].options[0] == restored.optionMoulds[id],
            "native RimWorld XML loading rebuilds shared object identity");
        Check(restored.optionMoulds[id].editorX == 370f && restored.optionMoulds[id].editorPositionSet && restored.optionMoulds[id].conditions.Count == 1
            && restored.optionMoulds[id].results[0].actions.Count == 1, "native loading retains choice positions and behavior");
        Check(errors == ChecksLogHandler.ErrorCount, "shared choice XML roundtrip logs no errors");
        DialogTreeDef copied = (DialogTreeDef)model.Copy(tree);
        Check(copied.nodeMoulds[0].options[0] == copied.nodeMoulds[1].options[0] && copied.nodeMoulds[0].options[0] != close,
            "AI snapshots preserve sharing while isolating the source");
        Check(!model.Fields(typeof(DialogNode)).Any(field => field.Name is "options" or "optionsResolved")
            && model.Schema(typeof(DialogTreeDef)).Descendants("field").Any(field => (string?)field.Attribute("name") == "optionIds" && field.Attribute("description") != null),
            "AI schema describes canonical choice references and omits runtime caches");
        CQFAIChanges changes = new(model);
        DialogTreeDef edited = (DialogTreeDef)changes.Build(tree, XElement.Parse("<changes><set path='/optionMoulds/@" + id + "/text'><value>CQF_Check_AI_Shared</value></set></changes>"), "", false);
        Check(edited.nodeMoulds.Values.All(node => node.options[0].text == "CQF_Check_AI_Shared") && close.text == "CQF_Check_Shared_Edit",
            "AI editing one choice updates every reference without mutating the source");
        DialogTreeDef unlinked = (DialogTreeDef)changes.Build(tree, XElement.Parse("<changes><remove path='/nodeMoulds/@1/optionIds/0'/></changes>"), "", false);
        Check(unlinked.nodeMoulds[1].options.Count == 0 && unlinked.nodeMoulds[0].options.Count == 1 && unlinked.optionMoulds.ContainsKey(id),
            "AI removes one reference without deleting shared behavior");
        Reject(() => changes.Build(tree, XElement.Parse("<changes><remove path='/optionMoulds/@" + id + "'/></changes>"), "", false), "deleting a referenced choice requires removing its references");
        Reject(() => changes.Build(tree, XElement.Parse("<changes><append path='/nodeMoulds/@0/optionIds'><value>9999</value></append></changes>"), "", false), "missing shared choice references are rejected");
        Reject(() => changes.Build(tree, XElement.Parse("<changes><append path='/nodeMoulds/@0/optionIds'><value>" + id + "</value></append></changes>"), "", false), "duplicate shared choice references are rejected");
        CQFDialogEditSession session = new(); session.Reset(tree);
        close.text = "CQF_Check_Undo_Shared"; session.Observe(tree);
        DialogTreeDef undo = session.Undo(tree);
        Check(undo.nodeMoulds[0].options[0].text == "CQF_Check_Shared_Edit" && undo.nodeMoulds[0].options[0] == undo.nodeMoulds[2].options[0],
            "undo restores both shared content and reference identity");
        DialogTreeDef redo = session.Redo(undo);
        Check(redo.nodeMoulds[1].options[0].text == "CQF_Check_Undo_Shared" && redo.nodeMoulds[0].options[0] == redo.optionMoulds[id],
            "redo preserves canonical shared identity");
        DialogOption local = session.Copy(tree).optionMoulds[id];
        tree.RegisterOption(local); second.options[0] = local; tree.Update();
        local.text = "CQF_Check_Local";
        Check(first.options[0] == third.options[0] && second.options[0] != first.options[0] && first.options[0].text != local.text,
            "a separate local copy leaves other shared references unchanged");
        DialogOption extra = new() { text = "CQF_Check_Extra" };
        tree.LinkOption(first, extra);
        first.options.Reverse(); tree.Update();
        Check(first.optionIds[0] == tree.optionMoulds.Single(pair => pair.Value == extra).Key && third.options[0] == close,
            "choice order is local to each dialogue and independent of canvas positions");
        tree.UnlinkOption(third, close);
        Check(third.options.Count == 0 && tree.optionMoulds.ContainsValue(close) && first.options.Contains(close),
            "removing one reference preserves the choice and its other owners");
        tree.DeleteOption(close);
        Check(tree.nodeMoulds.Values.All(node => !node.options.Contains(close)) && !tree.optionMoulds.ContainsValue(close) && second.options[0] == local,
            "deleting a choice removes every reference without deleting independent copies");
        tree.UnlinkOption(second, local);
        DialogTreeDef freeReload = Parse(tree.SaveToXElement("QuestEditor_Library.DialogTreeDef")); freeReload.Update();
        Check(freeReload.optionMoulds.Count == 2 && freeReload.nodeMoulds[1].options.Count == 0,
            "unreferenced choice cards survive saving and loading");
        DialogTreeDef legacy = Parse(XElement.Parse("<QuestEditor_Library.DialogTreeDef><defName>CQF_Check_Inline</defName><nodeMoulds><li><key>0</key><value><index>0</index><text>CQF_Check_Text</text><options><li><text>CQF_Check_Inline_Choice</text></li></options></value></li></nodeMoulds></QuestEditor_Library.DialogTreeDef>"));
        legacy.ResolveReferences();
        Check(legacy.nodeMoulds[0].options.Single().text == "CQF_Check_Inline_Choice" && legacy.optionMoulds.Count == 1,
            "existing inline Mod definitions still provide their choices at runtime");
    }

    private static DialogTreeDef Parse(XElement xml)
    {
        XmlDocument document = new(); document.LoadXml(xml.ToString());
        return DirectXmlToObject.ObjectFromXml<DialogTreeDef>(document.DocumentElement!, false);
    }

    private static void Reject(Action action, string name)
    {
        try { action(); } catch (InvalidDataException) { Console.WriteLine("PASS " + name); return; }
        throw new InvalidOperationException(name);
    }

    private static void Check(bool passed, string name)
    {
        if (!passed) throw new InvalidOperationException(name);
        Console.WriteLine("PASS " + name);
    }
}
