using System;
using System.Collections.Generic;

namespace QuestEditor_Library;

public sealed class CQFCinematicEditHistory
{
    public CQFCinematicEditHistory(string snapshot, string selectionPath = "")
    {
        entries.Add((snapshot, selectionPath));
    }

    public bool CanUndo => cursor > 0;
    public bool CanRedo => cursor + 1 < entries.Count;
    public string Snapshot => entries[cursor].Snapshot;
    public string SelectionPath => entries[cursor].SelectionPath;

    public bool Observe(string snapshot, string selectionPath, double now, bool merge = true)
    {
        if (string.Equals(snapshot, Snapshot, StringComparison.Ordinal))
        {
            entries[cursor] = (snapshot, selectionPath);
            return false;
        }
        if (CanRedo) entries.RemoveRange(cursor + 1, entries.Count - cursor - 1);
        if (merge && mergeAllowed && cursor > 0 && now - lastChange <= 0.8d)
            entries[cursor] = (snapshot, selectionPath);
        else
        {
            entries.Add((snapshot, selectionPath));
            cursor++;
            if (entries.Count > 101)
            {
                entries.RemoveAt(0);
                cursor--;
            }
        }
        lastChange = now;
        mergeAllowed = merge;
        return true;
    }

    public bool Undo()
    {
        if (!CanUndo) return false;
        cursor--;
        mergeAllowed = false;
        return true;
    }

    public bool Redo()
    {
        if (!CanRedo) return false;
        cursor++;
        mergeAllowed = false;
        return true;
    }

    public void EndEdit()
    {
        mergeAllowed = false;
    }

    private readonly List<(string Snapshot, string SelectionPath)> entries = new();
    private int cursor;
    private double lastChange = double.NegativeInfinity;
    private bool mergeAllowed;
}
