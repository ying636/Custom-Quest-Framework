using System;
using RimWorld;
using Verse;

namespace QuestEditor_Library;

public static class CQFGroupBookBridge
{
    public static void Register(Action<Action<GroupDataDef>> open) => opener = open;

    public static void Open(Action<GroupDataDef> selected)
    {
        if (opener != null)
            opener(selected);
        else
            CQFEditorTools.DrawFloatMenu(DefDatabase<GroupDataDef>.AllDefsListForReading, selected, group => group.DisplayName);
    }

    private static Action<Action<GroupDataDef>>? opener;
}
