using Verse;

namespace QuestEditor_Library;

public sealed class CQFLayoutPlan
{
    public List<CQFLayoutPlacement> Parts { get; } = new List<CQFLayoutPlacement>();
    public HashSet<IntVec3> Roads { get; } = new HashSet<IntVec3>();
    public List<string> Notices { get; } = new List<string>();
    public HashSet<IntVec3> Protected { get; } = new HashSet<IntVec3>();
    public HashSet<IntVec3> Allowed { get; } = new HashSet<IntVec3>();
    public CQFEcologyPlan? Ecology { get; set; }

    public bool Matches(CQFLayoutPlan other) => Parts.Count == other.Parts.Count && Parts.Zip(other.Parts, (a, b) =>
        a.Source == b.Source && a.Origin == b.Origin && a.Rotation == b.Rotation && a.Role == b.Role && a.ReferenceId == b.ReferenceId).All(equal => equal)
        && Roads.SetEquals(other.Roads) && Protected.SetEquals(other.Protected) && Allowed.SetEquals(other.Allowed)
        && (Ecology == null ? other.Ecology == null : other.Ecology != null && Ecology.Matches(other.Ecology));
}
