using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using UnityEvent = UnityEngine.Event;

namespace QuestEditor_Library
{
    public sealed class CQFDialogResultList
    {
        public void Draw(DialogOption option, Rect rect, Func<DialogResult, string> label, Action<DialogResult> select, Action<DialogResult>? menu, Action changed)
        {
            GUI.BeginGroup(rect);
            try
            {
                if (UnityEvent.current.type == EventType.Repaint)
                {
                    List<DialogResult> results = option.results;
                    DialogResult[] snapshot = results.ToArray();
                    this.group = ReorderableWidget.NewGroup((from, to) =>
                    {
                        if (!ReferenceEquals(option.results, results) || !snapshot.SequenceEqual(results)) return;
                        DialogResult result = results[from];
                        results.Insert(to, result);
                        results.RemoveAt(from < to ? from : from + 1);
                        changed();
                    }, ReorderableDirection.Vertical, new Rect(0f, 0f, rect.width, rect.height), 6f, playSoundOnStartReorder: false);
                }
                for (int index = 0; index < option.results.Count; index++)
                {
                    DialogResult result = option.results[index];
                    Rect row = new Rect(0f, index * RowHeight, rect.width, RowHeight - 6f);
                    Rect grip = new Rect(4f, row.y + 5f, 24f, 24f);
                    CQFAIIconButton.DrawBackground(row);
                    CQFAIIconButton.DrawGlyph(grip, TexButton.DragHash);
                    TooltipHandler.TipRegion(grip, "CQF_DialogGraph_ResultOrder".Translate());
                    UnityEvent input = UnityEvent.current;
                    if (input.type != EventType.MouseDown || grip.Contains(input.mousePosition)) ReorderableWidget.Reorderable(this.group, row);
                    if (input.type == EventType.MouseDown && input.button == 0 && grip.Contains(input.mousePosition)) input.Use();
                    string text = label(result);
                    string tip = text + "\n" + "CQF_DialogGraph_ResultCounts".Translate(result.conditions.Count, result.actions.Count);
                    if (CQFAIIconButton.DrawText(new Rect(32f, row.y, rect.width - 32f, row.height), text.Replace('\n', ' '), tip: tip, drawBackground: false)) select(result);
                    if (menu != null && input.type == EventType.MouseDown && input.button == 1 && row.Contains(input.mousePosition))
                    { menu(result); input.Use(); }
                }
            }
            finally { GUI.EndGroup(); }
        }

        public const float RowHeight = 40f;
        private int group = -1;
    }
}
