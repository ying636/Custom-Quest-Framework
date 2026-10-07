using System;
using UnityEngine;
using Verse;

namespace QuestEditor_Library
{
    public static class DialogManagerDefEditor
    {
        public static void Draw_0(DialogManagerDef manager, ref float y)
            => DrawEntries(manager, ref y, 620f);

        public static void DrawEntries(DialogManagerDef manager, ref float y, float width)
        {
            using CQFUIScope scope = new CQFUIScope();
            for (int i = 0; i < manager.trees.Count; i++)
            {
                DialogTreeAndConditions entry = manager.trees[i];
                float start = y;
                float cardHeight = 92f + entry.conditions.Count * 38f;
                Rect card = new Rect(0f, y, width, cardHeight);
                Widgets.DrawBoxSolid(card, CQFUIStyle.Card);
                CQFUIStyle.DrawBox(card);
                Rect tree = new Rect(12f, y + 10f, width - 94f, 32f);
                if (CQFUIStyle.ButtonText(tree, "DialogTree".Translate(entry.tree?.defName), overrideTextAnchor: TextAnchor.MiddleLeft))
                    CQFEditorTools.DrawFloatMenu(DefDatabase<DialogTreeDef>.AllDefsListForReading, selected => entry.tree = selected, selected => selected.defName);
                if (CQFUIStyle.ButtonImage(new Rect(width - 74f, y + 12f, 28f, 28f), TexButton.Play, tooltip: "DialogEditor".Translate()) && entry.tree != null)
                    Find.WindowStack.Add(new QuestEditor_Dialog { CurTree = entry.tree });
                if (CQFUIStyle.ButtonImage(new Rect(width - 38f, y + 12f, 28f, 28f), TexButton.Delete, tooltip: "Delete".Translate()))
                {
                    manager.trees.RemoveAt(i--);
                    continue;
                }
                y += 50f;
                Widgets.Label(new Rect(12f, y, width - 62f, 28f), "Conditions".Translate().Colorize(CQFUIStyle.Accent));
                if (CQFUIStyle.ButtonImage(new Rect(width - 38f, y, 28f, 28f), TexButton.Plus, tooltip: "Add".Translate()))
                    CQFEditorTools.DrawFloatMenu(typeof(DialogCondition).AllSubclassesNonAbstract(), type => entry.conditions.Add((DialogCondition)Activator.CreateInstance(type)), type => type.Name.Translate());
                y += 32f;
                for (int j = 0; j < entry.conditions.Count; j++)
                {
                    DialogCondition condition = entry.conditions[j];
                    if (CQFUIStyle.ButtonText(new Rect(12f, y, width - 62f, 32f), condition.GetType().Name.Translate(), overrideTextAnchor: TextAnchor.MiddleLeft))
                        Find.WindowStack.Add(new Dialog_EditIDrawable(condition));
                    if (CQFUIStyle.ButtonImage(new Rect(width - 38f, y + 2f, 28f, 28f), TexButton.Delete, tooltip: "Delete".Translate()))
                        entry.conditions.RemoveAt(j--);
                    y += 38f;
                }
                y = start + cardHeight + 12f;
            }
        }
    }
}
