using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace QuestEditor_Library
{
    public static class PawnModWorker_SkillsEditor
    {
        public static void Draw_0(QuestEditor_Library.PawnModWorker_Skills cqfReceiver, ComplexPawnDef pawnDef, ref float y, Rect inRect, float x)
        {
            using CQFUIScope cqfUIScope = new CQFUIScope(inRect.width);
            PawnModData_Skills modData = pawnDef.DataFor<PawnModData_Skills>();
            foreach (SkillDef skill in DefDatabase<SkillDef>.AllDefsListForReading.OrderBy(def => def.listOrder))
            {
                SkillData data = cqfReceiver.DataFor(modData.skills, skill);
                Rect row = new Rect(x, y, Mathf.Min(360f, inRect.width - x - 20f), 26f);
                cqfReceiver.DrawSkillRow(skill, data, row);
                y += 30f;
            }
        }

        public static void DrawSkillRow_1(QuestEditor_Library.PawnModWorker_Skills cqfReceiver, SkillDef skill, SkillData data, Rect row)
        {
            if (Mouse.IsOver(row))
            {
                GUI.DrawTexture(row, TexUI.HighlightTex);
            }

            Text.Anchor = TextAnchor.MiddleLeft;
            Rect labelRect = new Rect(row.x + 6f, row.y, 100f, row.height);
            Widgets.Label(labelRect, skill.skillLabel.CapitalizeFirst().Colorize(CQFUIStyle.Accent));
            Rect passionRect = new Rect(labelRect.xMax, row.y + 1f, 24f, 24f);
            Widgets.DrawLightHighlight(passionRect);
            CQFUIStyle.DrawBox(passionRect, 1);
            Widgets.DrawHighlightIfMouseover(passionRect);
            if (data.passion == Passion.Minor)
            {
                GUI.DrawTexture(passionRect, SkillUI.PassionMinorIcon);
            }
            else if (data.passion == Passion.Major)
            {
                GUI.DrawTexture(passionRect, SkillUI.PassionMajorIcon);
            }

            if (Widgets.ButtonInvisible(passionRect))
            {
                CQFEditorTools.DrawFloatMenu(cqfReceiver.Passions, passion => data.passion = passion, cqfReceiver.PassionLabel);
            }

            TooltipHandler.TipRegion(passionRect, "CQF_PawnEditor_SkillPassion".Translate(cqfReceiver.PassionLabel(data.passion)));
            Rect barRect = new Rect(passionRect.xMax + 4f, row.y + 1f, row.xMax - passionRect.xMax - 10f, 24f);
            if (Mouse.IsOver(barRect))
            {
                cqfReceiver.UpdateLevelByMouse(data, barRect);
            }

            Widgets.FillableBar(barRect, Mathf.Max(0.01f, Mathf.Clamp(data.level, 0, 20) / 20f), cqfReceiver.SkillBarFillTex, null, false);
            Widgets.Label(new Rect(barRect.x + 6f, barRect.y + 2f, 40f, 20f), Mathf.Clamp(data.level, 0, 20).ToStringCached());
            TooltipHandler.TipRegion(barRect, "CQF_PawnEditor_SkillLevel".Translate());
            Text.Anchor = TextAnchor.UpperLeft;
        }
    }
}
