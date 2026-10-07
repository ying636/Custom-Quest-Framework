using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace QuestEditor_Library
{
    public static class DutyMapTransitionEditor
    {
        public static void Draw_0(QuestEditor_Library.DutyMapTransition cqfReceiver, ref float y, Rect inRect, float x)
        {
            using CQFUIScope cqfUIScope = new CQFUIScope(inRect.width);
            Widgets.Label(new Rect(x, y, 260f, 25f), "CQF_DutyMapTransition".Translate().Colorize(CQFUIStyle.Accent));
            y += 30f;
            cqfReceiver.DrawNodeSelect(y, x, "CQF_DutyMapFromNode".Translate(), cqfReceiver.fromNodeId, true);
            y += 30f;
            cqfReceiver.DrawNodeSelect(y, x, "CQF_DutyMapToNode".Translate(), cqfReceiver.toNodeId, false);
            y += 30f;
            cqfReceiver.DrawSectionList(ref y, x, inRect.width - x - 10f, "CQF_DutyTransitionTriggers".Translate(), cqfReceiver.triggers, cqfReceiver.OpenTriggerSelect, () => CQFEditorTools.DrawFloatMenu(cqfReceiver.triggers, trigger => cqfReceiver.triggers.Remove(trigger), trigger => cqfReceiver.TriggerLabel(trigger.GetType())), trigger => cqfReceiver.TriggerLabel(trigger.GetType()));
            CQFEditorTools.DrawCQFConditionList(ref y, x, inRect.width - x - 10f, inRect, "CQF_DutyTransitionConditions".Translate(), cqfReceiver.conditions);
        }

        public static void DrawNodeSelect_1(QuestEditor_Library.DutyMapTransition cqfReceiver, float y, float x, string label, string nodeId, bool isFromNode)
        {
            float labelWidth = Mathf.Min(Text.CalcSize(label).x + 8f, 150f);
            Widgets.Label(new Rect(x, y, labelWidth, 25f), label);
            Rect buttonRect = new Rect(x + labelWidth + 8f, y, 180f, 25f);
            if (CQFUIStyle.ButtonText(buttonRect, nodeId.NullOrEmpty() ? "Null".Translate().ToString() : nodeId, false))
            {
                DutyMapDef dutyMap = QuestEditor_DutyMap.CurrentEditingDutyMap;
                if (dutyMap != null)
                {
                    Find.WindowStack.Add(new Dialog_Select<DutyMapNode>(new TextSelectDrawer<DutyMapNode>(dutyMap.nodes, node => node.nodeId, node =>
                    {
                        if (isFromNode)
                        {
                            cqfReceiver.fromNodeId = node.nodeId;
                        }
                        else
                        {
                            cqfReceiver.toNodeId = node.nodeId;
                        }
                    }, null, null, null, null, null, null), "CQF_DutySelectNode".Translate()));
                }
            }
        }

        public static void DrawSectionList_2<T>(QuestEditor_Library.DutyMapTransition cqfReceiver, ref float y, float x, float width, string label, List<T> list, Action addAction, Action removeAction, Func<T, string> getText)
            where T : class, IDrawable
        {
            cqfReceiver.DrawSectionHeader(ref y, x, width, label, addAction, removeAction);
            if (list.Any())
            {
                foreach (T item in list)
                {
                    Rect rowRect = new Rect(x + 6f, y, width - 12f, 28f);
                    if (CQFUIStyle.ButtonText(rowRect, getText(item), false))
                    {
                        Find.WindowStack.Add(new Dialog_EditIDrawable(item));
                    }

                    y += 32f;
                }
            }
            else
            {
                Widgets.Label(new Rect(x + 10f, y + 2f, width - 20f, 25f), "CQF_DutyMapNoOptions".Translate().Colorize(CQFUIStyle.Muted));
                y += 30f;
            }

            y += 8f;
        }

        public static void DrawSectionHeader_3(QuestEditor_Library.DutyMapTransition cqfReceiver, ref float y, float x, float width, string label, Action addAction, Action removeAction)
        {
            Widgets.DrawHighlight(new Rect(x - 4f, y - 2f, width + 8f, 32f));
            Text.Font = GameFont.Medium;
            Widgets.Label(new Rect(x, y, width - 90f, 30f), label.Colorize(CQFUIStyle.Accent));
            Text.Font = GameFont.Small;
            Rect buttonRect = new Rect(x + width - 60f, y + 2f, 25f, 25f);
            if (addAction != null && CQFUIStyle.ButtonImage(buttonRect, TexButton.Plus))
            {
                addAction();
            }

            buttonRect.x += 30f;
            if (removeAction != null && CQFUIStyle.ButtonImage(buttonRect, TexButton.Delete))
            {
                removeAction();
            }

            y += 38f;
        }
    }
}
