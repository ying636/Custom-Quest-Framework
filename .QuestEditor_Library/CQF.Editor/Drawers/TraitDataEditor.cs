using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace QuestEditor_Library
{
    public static class TraitDataEditor
    {
        public static void DrawList_0(List<TraitData> list, ref float y, string title = null, string tip = null, bool needBox = false, float x = 10f, float defaultWidth = 180f)
        {
            using CQFUIScope cqfUIScope = new CQFUIScope();
            List<KeyValuePair<TraitDef, TraitDegreeData>> stagets = new List<KeyValuePair<TraitDef, TraitDegreeData>>();
            DefDatabase<TraitDef>.AllDefsListForReading.ForEach(t =>
            {
                t.degreeDatas.ForEach(s =>
                {
                    stagets.Add(new KeyValuePair<TraitDef, TraitDegreeData>(t, s));
                });
            });
            float initY = y;
            float width = defaultWidth;
            if (title != null)
            {
                y += 5f;
                Text.Font = GameFont.Medium;
                Rect rectTitle = new Rect(x + 10f, y, Mathf.Max(40f, CQFUIScope.ContentWidth - x - 24f), 35f);
                Widgets.Label(rectTitle, title);
                if (tip != null)
                {
                    TooltipHandler.TipRegion(rectTitle, tip);
                }

                Text.Font = GameFont.Small;
                y += 40f;
                float textWidth = Text.CalcSize(title).x + 20f;
                width = Mathf.Min(Mathf.Max(textWidth, width), Mathf.Max(40f, CQFUIScope.ContentWidth - x - 12f));
            }

            for (int i = 0; i < list.Count; i++)
            {
                TraitData data = list[i];
                CQFEditorTools.DrawSelectablePercent(y, data.def?.DataAtDegree(data.degree)?.label, ref data.chance, ref data.buffer, () => Find.WindowStack.Add(new Dialog_Select<KeyValuePair<TraitDef, TraitDegreeData>>(new TextSelectDrawer<KeyValuePair<TraitDef, TraitDegreeData>>(stagets, t => t.Value.label, t =>
                {
                    data.def = t.Key;
                    data.degree = t.Value.degree;
                }, null, null, null, null, null, null), "CQF_PawnEditor_Select".Translate())), x + 5f);
                y += 30f;
            }

            y += 5f;
            if (needBox)
            {
                CQFUIStyle.DrawBox(new Rect(x, initY, width, y - initY), 1, QuestEditor_Dialog.blueTex);
            }

            y += 10f;
            CQFEditorTools.DrawButtonForList(ref y, list, t => t.def?.DataAtDegree(t.degree)?.label, () => Find.WindowStack.Add(new Dialog_Select<KeyValuePair<TraitDef, TraitDegreeData>>(new TextSelectDrawer<KeyValuePair<TraitDef, TraitDegreeData>>(stagets, t => t.Value.label, t =>
            {
                list.Add(new TraitData() { def = t.Key, degree = t.Value.degree, chance = 1f });
            }, null, null, null, null, null, null), "CQF_PawnEditor_Select".Translate())), x - 5f, width - 70f, new Vector2(70f, 25f));
        }
    }
}
