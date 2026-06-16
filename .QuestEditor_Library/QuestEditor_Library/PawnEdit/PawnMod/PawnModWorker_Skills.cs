using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace QuestEditor_Library
{
    public class PawnModWorker_Skills : PawnModWorker
    {
        public override bool CanAddFor(ComplexPawnDef pawnDef)
        {
            return pawnDef.KindDef?.race?.race?.Humanlike ?? false;
        }

        public override PawnModData CreateData()
        {
            return new PawnModData_Skills();
        }

        public override void Draw(ComplexPawnDef pawnDef, ref float y, Rect inRect, float x)
        {
            PawnModData_Skills modData = pawnDef.DataFor<PawnModData_Skills>();
            foreach (SkillDef skill in DefDatabase<SkillDef>.AllDefsListForReading.OrderBy(def => def.listOrder))
            {
                SkillData data = this.DataFor(modData.skills, skill);
                Rect row = new Rect(x, y, Mathf.Min(360f, inRect.width - x - 20f), 26f);
                this.DrawSkillRow(skill, data, row);
                y += 30f;
            }
        }

        public override void ApplyToPawn(ComplexPawnDef pawnDef, Pawn pawn, bool preview)
        {
            if (pawn.skills == null)
            {
                return;
            }
            foreach (SkillData data in pawnDef.DataFor<PawnModData_Skills>().skills)
            {
                if (data?.def == null || pawn.skills.GetSkill(data.def) is not SkillRecord record)
                {
                    continue;
                }
                record.Level = Mathf.Clamp(data.level, 0, 20);
                record.passion = data.passion;
            }
        }

        public override void LoadData(ComplexPawnDef pawnDef, System.Xml.XmlNode node)
        {
            if (node["skills"] != null)
            {
                pawnDef.DataFor<PawnModData_Skills>().skills = this.LoadSaveableList<SkillData>(node["skills"]);
            }
        }

        private SkillData DataFor(List<SkillData> list, SkillDef skill)
        {
            SkillData result = list.FirstOrDefault(data => data.def == skill);
            if (result == null)
            {
                result = new SkillData { def = skill };
                list.Add(result);
            }
            return result;
        }

        private void DrawSkillRow(SkillDef skill, SkillData data, Rect row)
        {
            if (Mouse.IsOver(row))
            {
                GUI.DrawTexture(row, TexUI.HighlightTex);
            }
            Text.Anchor = TextAnchor.MiddleLeft;
            Rect labelRect = new Rect(row.x + 6f, row.y, 100f, row.height);
            Widgets.Label(labelRect, skill.skillLabel.CapitalizeFirst().Colorize(ColorLibrary.PaleBlue));

            Rect passionRect = new Rect(labelRect.xMax, row.y + 1f, 24f, 24f);
            Widgets.DrawLightHighlight(passionRect);
            Widgets.DrawBox(passionRect, 1);
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
                CQFEditorTools.DrawFloatMenu(this.Passions, passion => data.passion = passion, this.PassionLabel);
            }
            TooltipHandler.TipRegion(passionRect, "CQF_PawnEditor_SkillPassion".Translate(this.PassionLabel(data.passion)));

            Rect barRect = new Rect(passionRect.xMax + 4f, row.y + 1f, row.xMax - passionRect.xMax - 10f, 24f);
            if (Mouse.IsOver(barRect))
            {
                this.UpdateLevelByMouse(data, barRect);
            }
            Widgets.FillableBar(barRect, Mathf.Max(0.01f, Mathf.Clamp(data.level, 0, 20) / 20f), this.SkillBarFillTex, null, false);
            Widgets.Label(new Rect(barRect.x + 6f, barRect.y + 2f, 40f, 20f), Mathf.Clamp(data.level, 0, 20).ToStringCached());
            TooltipHandler.TipRegion(barRect, "CQF_PawnEditor_SkillLevel".Translate());
            Text.Anchor = TextAnchor.UpperLeft;
        }

        private void UpdateLevelByMouse(SkillData data, Rect barRect)
        {
            UnityEngine.Event current = UnityEngine.Event.current;
            if (current.type != EventType.MouseDown && current.type != EventType.MouseDrag)
            {
                return;
            }
            if (current.button != 0)
            {
                return;
            }
            float percent = Mathf.Clamp01((current.mousePosition.x - barRect.x) / barRect.width);
            data.level = Mathf.Clamp(Mathf.RoundToInt(percent * 20f), 0, 20);
            data.levelBuffer = data.level.ToString();
            current.Use();
        }

        private string PassionLabel(Passion passion)
        {
            return ("Passion" + passion).Translate();
        }

        private List<Passion> Passions => new List<Passion> { Passion.None, Passion.Minor, Passion.Major };

        private readonly Texture2D SkillBarFillTex = SolidColorMaterials.NewSolidColorTexture(new Color(1f, 1f, 1f, 0.12f));
    }
}
