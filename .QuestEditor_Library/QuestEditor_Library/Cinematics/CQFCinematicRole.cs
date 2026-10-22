using RimWorld;
using System.Xml.Linq;
using Verse;

namespace QuestEditor_Library;

public sealed class CQFCinematicRole : ISaveable
{
    public string DisplayName => string.IsNullOrWhiteSpace(label) ? "CQF_Cinematic_UnassignedRole".Translate().ToString() : label;

    public IEnumerable<string> ConfigErrors()
    {
        if (string.IsNullOrWhiteSpace(key)) yield return "CQF_Cinematic_RoleKeyRequired".Translate(DisplayName);
        if (!Enum.IsDefined(typeof(CQFCinematicRoleSource), source)) yield return "CQF_Cinematic_RoleSourceInvalid".Translate(DisplayName);
        else if (source != CQFCinematicRoleSource.Recorded && string.IsNullOrWhiteSpace(sourceKey))
            yield return "CQF_Cinematic_RoleSourceKeyRequired".Translate(DisplayName);
    }

    public TargetInfo Resolve(Map fallbackMap, IEnumerable<CQFCinematicBinding> recorded, IDictionary<string, TargetInfo>? context = null, Quest? quest = null)
    {
        if (context != null && context.TryGetValue(key, out TargetInfo explicitTarget)) return RequirePawn(explicitTarget);
        if (source == CQFCinematicRoleSource.Recorded)
        {
            CQFCinematicBinding[] matches = recorded.Where(binding => binding.key == key).ToArray();
            if (matches.Length == 0) throw new InvalidOperationException("CQF_Cinematic_RoleMissing".Translate(DisplayName));
            if (matches.Length != 1) throw new InvalidOperationException("CQF_Cinematic_RoleAmbiguous".Translate(DisplayName));
            return RequirePawn(matches[0].Resolve(fallbackMap));
        }
        if (string.IsNullOrWhiteSpace(sourceKey)) throw new InvalidOperationException("CQF_Cinematic_RoleSourceKeyRequired".Translate(DisplayName));
        if (source == CQFCinematicRoleSource.Context)
        {
            if (context == null || !context.TryGetValue(sourceKey, out TargetInfo target))
                throw new InvalidOperationException("CQF_Cinematic_RoleMissing".Translate(DisplayName));
            return RequirePawn(target);
        }
        if (source == CQFCinematicRoleSource.QuestDatabase && quest == null)
            throw new InvalidOperationException("CQF_Cinematic_RoleNoQuest".Translate(DisplayName));
        GameComponent_Editor? editor = Current.Game?.GetComponent<GameComponent_Editor>();
        QuestData? database = source switch
        {
            CQFCinematicRoleSource.QuestDatabase => editor?.GetQuestData(quest!),
            CQFCinematicRoleSource.TemporaryDatabase => editor?.TemporaryDatabase,
            CQFCinematicRoleSource.GlobalDatabase => editor?.GlobalDatabase,
            _ => throw new InvalidOperationException("CQF_Cinematic_RoleSourceInvalid".Translate(DisplayName))
        };
        if (database == null) throw new InvalidOperationException("CQF_Cinematic_RoleMissing".Translate(DisplayName));
        List<TargetInfo> candidates = database.TargetDatas.Where(entry => entry != null && entry.key == sourceKey).Select(entry => entry.target).ToList();
        List<Thing>? group = database.GetGroup(sourceKey);
        if (group != null) candidates.AddRange(group.Select(thing => thing == null ? TargetInfo.Invalid : new TargetInfo(thing)));
        if (candidates.Count == 0) throw new InvalidOperationException("CQF_Cinematic_RoleMissing".Translate(DisplayName));
        if (candidates.Count != 1) throw new InvalidOperationException("CQF_Cinematic_RoleAmbiguous".Translate(DisplayName));
        return RequirePawn(candidates[0]);
    }

    public XElement SaveToXElement(string nodeName)
    {
        return new XElement(nodeName, new XElement("key", key), new XElement("label", label),
            new XElement("source", source), new XElement("sourceKey", sourceKey));
    }

    private TargetInfo RequirePawn(TargetInfo target)
    {
        if (!target.IsValid || target.Thing is not Pawn pawn || pawn.Destroyed)
            throw new InvalidOperationException("CQF_Cinematic_RoleNotPawn".Translate(DisplayName));
        return target;
    }

    public string key = "";
    public string label = "";
    public CQFCinematicRoleSource source;
    public string sourceKey = "";
}
