using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;
using Verse;

namespace QuestEditor_Library;

internal static class CQFGroupBookCatalog
{
    internal static void Open(Action<GroupDataDef> selected) =>
        Find.WindowStack.Add(new Window_CQFGroupBook(selected));

    internal static void OpenTags(Action<string> selected)
    {
        Find.WindowStack.Add(new Window_CQFGroupBook(group =>
        {
            List<string> tags = group.lord.groupTags.Where(tag => !tag.NullOrEmpty()).Distinct().ToList();
            if (tags.Count == 1) selected(tags[0]);
            else CQFEditorTools.DrawFloatMenu(tags, selected, tag => tag);
        }, tagsOnly: true));
    }

    internal static GroupDataDef Copy(GroupDataDef source)
    {
        var xml = new XmlDocument();
        xml.LoadXml(source.SaveToXElement(typeof(GroupDataDef).FullName!).ToString());
        GroupDataDef draft = DirectXmlToObject.ObjectFromXml<GroupDataDef>(xml.DocumentElement, false);
        DirectXmlCrossRefLoader.ResolveAllWantedCrossReferences(FailMode.LogErrors);
        return draft;
    }

    internal static string Save(GroupDataDef draft)
    {
        if (draft.defName.NullOrEmpty() || !Regex.IsMatch(draft.defName, @"\A[A-Za-z_][A-Za-z0-9_]*\z"))
            throw new InvalidOperationException("[CQF] Invalid group template defName: " + draft.defName);
        if (draft.lord.groupTemplate != null)
            throw new InvalidOperationException("[CQF] A group template cannot link to a template.");
        GroupDataDef? existing = DefDatabase<GroupDataDef>.GetNamedSilentFail(draft.defName);
        if (existing?.modContentPack != null)
            throw new InvalidOperationException("[CQF] Group template belongs to a loaded mod: " + draft.defName);
        GroupDataDef saved = Copy(draft);
        string directory = Path.Combine(CQFContentPaths.Quests, "Group");
        string path = Path.Combine(directory, draft.defName + ".xml");
        string temporary = Path.Combine(directory, Guid.NewGuid().ToString("N") + ".tmp");
        Directory.CreateDirectory(directory);
        try
        {
            new XElement("Defs", draft.SaveToXElement(typeof(GroupDataDef).FullName!)).Save(temporary);
            if (File.Exists(path)) File.Replace(temporary, path, null);
            else File.Move(temporary, path);
        }
        finally
        {
            if (File.Exists(temporary)) File.Delete(temporary);
        }
        if (existing == null)
            DefDatabase<GroupDataDef>.Add(saved);
        else
        {
            existing.label = saved.label;
            existing.lord = saved.lord;
            existing.pawns = saved.pawns;
        }
        return path;
    }
}
