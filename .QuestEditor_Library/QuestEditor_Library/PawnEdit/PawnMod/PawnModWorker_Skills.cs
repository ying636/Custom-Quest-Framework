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
            object[] arguments = new object[]
            {
                pawnDef,
                y,
                inRect,
                x
            };
            CQFEditorBridge.Invoke("QuestEditor_Library.PawnModWorker_Skills.Draw(None:QuestEditor_Library.ComplexPawnDef,Ref:float,None:UnityEngine.Rect,None:float)", this, arguments);
            y = (float)arguments[1];
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
        internal SkillData DataFor(List<SkillData> list, SkillDef skill)
        {
            SkillData result = list.FirstOrDefault(data => data.def == skill);
            if (result == null)
            {
                result = new SkillData { def = skill };
                list.Add(result);
            }
            return result;
        }
        internal void DrawSkillRow(SkillDef skill, SkillData data, Rect row)

        {
            object[] arguments = new object[]
            {
                skill,
                data,
                row
            };
            CQFEditorBridge.Invoke("QuestEditor_Library.PawnModWorker_Skills.DrawSkillRow(None:RimWorld.SkillDef,None:QuestEditor_Library.SkillData,None:UnityEngine.Rect)", this, arguments);
        }
        internal void UpdateLevelByMouse(SkillData data, Rect barRect)
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
        internal string PassionLabel(Passion passion)
        {
            return ("Passion" + passion).Translate();
        }
        internal List<Passion> Passions => new List<Passion> { Passion.None, Passion.Minor, Passion.Major };
        internal readonly Texture2D SkillBarFillTex = SolidColorMaterials.NewSolidColorTexture(new Color(1f, 1f, 1f, 0.12f));
    }
}
