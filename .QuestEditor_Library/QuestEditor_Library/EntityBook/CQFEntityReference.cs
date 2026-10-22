using System.Globalization;
using RimWorld;
using Verse;

namespace QuestEditor_Library;

public sealed class CQFEntityReference
{
    private CQFEntityReference(string sourceId, int? memberIndex, bool group)
    {
        SourceId = sourceId;
        MemberIndex = memberIndex;
        Group = group;
    }

    public string SourceId { get; }
    public int? MemberIndex { get; }
    public bool Group { get; }

    public static string Encode(string sourceId, int? memberIndex = null, bool group = false)
    {
        if (string.IsNullOrWhiteSpace(sourceId)) throw new ArgumentException("CQF_EntityBook_SourceRequired".Translate(), nameof(sourceId));
        if (memberIndex < 0 || memberIndex.HasValue && group) throw new ArgumentException("CQF_EntityBook_InvalidReference".Translate());
        return Prefix + Uri.EscapeDataString(sourceId) + ":" + (group ? "group" : memberIndex?.ToString(CultureInfo.InvariantCulture) ?? "single");
    }

    public static bool IsReference(string? token) => token?.StartsWith(Prefix, StringComparison.Ordinal) == true;

    public static bool TryParse(string? token, out CQFEntityReference? reference)
    {
        reference = null;
        if (!IsReference(token)) return false;
        int separator = token!.LastIndexOf(':');
        if (separator <= Prefix.Length || separator == token.Length - 1) return false;
        string source;
        try { source = Uri.UnescapeDataString(token.Substring(Prefix.Length, separator - Prefix.Length)); }
        catch (UriFormatException) { return false; }
        if (string.IsNullOrWhiteSpace(source)) return false;
        string selection = token.Substring(separator + 1);
        int? member = null;
        if (selection != "group" && selection != "single")
        {
            if (!int.TryParse(selection, NumberStyles.None, CultureInfo.InvariantCulture, out int index) || index < 0) return false;
            member = index;
        }
        reference = new CQFEntityReference(source, member, selection == "group");
        return true;
    }

    public static IReadOnlyList<Pawn> Resolve(string token, Quest? quest = null, Map? map = null)
    {
        if (!TryParse(token, out CQFEntityReference? reference)) throw new InvalidOperationException("CQF_EntityBook_InvalidReference".Translate());
        GameComponent_EntityBook? book = Current.Game?.GetComponent<GameComponent_EntityBook>();
        if (book == null) throw new InvalidOperationException("CQF_EntityBook_Unavailable".Translate());
        IReadOnlyList<CQFEntityRecord> records = book.GetRecords(reference!.SourceId, quest);
        string label = records.FirstOrDefault(record => !string.IsNullOrWhiteSpace(record.SourceLabel))?.SourceLabel
            ?? "CQF_EntityBook_Entity".Translate().ToString();
        if (reference.Group)
        {
            Pawn[] available = records.Where(record => record.IsAvailable).Select(record => record.Pawn!).Distinct().ToArray();
            if (available.Length == 0) throw new InvalidOperationException("CQF_EntityBook_NotGenerated".Translate(label));
            return available;
        }
        CQFEntityRecord? selected;
        if (reference.MemberIndex.HasValue) selected = records.SingleOrDefault(record => record.MemberIndex == reference.MemberIndex.Value);
        else
        {
            if (records.Count > 1) throw new InvalidOperationException("CQF_EntityBook_MemberRequired".Translate(label, records.Count));
            selected = records.SingleOrDefault();
        }
        if (selected == null) throw new InvalidOperationException("CQF_EntityBook_NotGenerated".Translate(label));
        if (!selected.IsAvailable) throw new InvalidOperationException("CQF_EntityBook_MemberUnavailable".Translate(label, selected.MemberIndex + 1));
        return new[] { selected.Pawn! };
    }

    public static Pawn ResolveSingle(string token, Quest? quest = null, Map? map = null)
    {
        IReadOnlyList<Pawn> pawns = Resolve(token, quest, map);
        if (pawns.Count != 1) throw new InvalidOperationException("CQF_EntityBook_MemberRequired".Translate("CQF_EntityBook_Entity".Translate(), pawns.Count));
        return pawns[0];
    }

    private const string Prefix = "cqf-entity:";
}
