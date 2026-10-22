using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace QuestEditor_Library;

internal static class CQFCinematicTextReview
{
    public static string Merge(string path, IEnumerable<CQFCinematicSubtitleText> subtitles, Func<string, string> text)
    {
        string[] headers = new[] { "Key", "Purpose", "Chinese", "English", "ChineseSource", "EnglishSource", "Status", "ChineseSuggestion", "EnglishSuggestion", "Comments" }
            .Select(name => text("CQF_Cinematic_Review_" + name)).ToArray();
        List<string[]> rows = File.Exists(path) ? Parse(File.ReadAllText(path, Encoding.UTF8)) : new List<string[]> { headers };
        if (rows.Count == 0 || rows.Any(row => row.Length != 10) || !rows[0].SequenceEqual(headers))
            throw new InvalidDataException(text("CQF_Cinematic_Review_InvalidCsv"));
        Dictionary<string, string[]> indexed = new(StringComparer.Ordinal);
        foreach (string[] row in rows.Skip(1))
            if (row[0].Length == 0 || indexed.ContainsKey(row[0])) throw new InvalidDataException(text("CQF_Cinematic_Review_InvalidCsv"));
            else indexed.Add(row[0], row);
        string source = text("CQF_Cinematic_Review_UserProvided");
        string pending = text("CQF_Cinematic_Review_Pending");
        foreach (CQFCinematicSubtitleText subtitle in subtitles)
        {
            if (!indexed.TryGetValue(subtitle.Key, out string[]? row))
            {
                row = new[] { subtitle.Key, text("CQF_Cinematic_Review_SubtitlePurpose"), "", "", "", "", pending, "", "", "" };
                indexed.Add(subtitle.Key, row);
                rows.Add(row);
            }
            if (row[2] != subtitle.Chinese || row[3] != subtitle.English || row[4] != source || row[5] != source) row[6] = pending;
            row[2] = subtitle.Chinese;
            row[3] = subtitle.English;
            row[4] = source;
            row[5] = source;
        }
        return "\uFEFF" + string.Join("\r\n", rows.Select(row => string.Join(",", row.Select(value => "\"" + value.Replace("\"", "\"\"") + "\"")))) + "\r\n";
    }

    private static List<string[]> Parse(string csv)
    {
        List<string[]> rows = new();
        List<string> row = new();
        StringBuilder cell = new();
        bool quoted = false;
        bool closed = false;
        for (int index = csv.Length > 0 && csv[0] == '\uFEFF' ? 1 : 0; index < csv.Length; index++)
        {
            char value = csv[index];
            if (quoted)
            {
                if (value == '"')
                {
                    if (index + 1 < csv.Length && csv[index + 1] == '"') { cell.Append('"'); index++; }
                    else { quoted = false; closed = true; }
                }
                else cell.Append(value);
            }
            else if (value == '"' && cell.Length == 0 && !closed) quoted = true;
            else if (value == ',') { row.Add(cell.ToString()); cell.Clear(); closed = false; }
            else if (value is '\r' or '\n')
            {
                if (value == '\r' && index + 1 < csv.Length && csv[index + 1] == '\n') index++;
                row.Add(cell.ToString());
                rows.Add(row.ToArray());
                row.Clear();
                cell.Clear();
                closed = false;
            }
            else if (closed || value == '"') throw new InvalidDataException("CQF_Cinematic_Review_InvalidCsv");
            else cell.Append(value);
        }
        if (quoted) throw new InvalidDataException("CQF_Cinematic_Review_InvalidCsv");
        if (cell.Length > 0 || row.Count > 0 || closed) { row.Add(cell.ToString()); rows.Add(row.ToArray()); }
        return rows;
    }
}
