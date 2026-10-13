using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using UnityEngine;
using Verse;

namespace QuestEditor_Library
{
    public class PawnModWorker
    {
        public virtual bool CanAddFor(ComplexPawnDef pawnDef)
        {
            return true;
        }

        public virtual PawnModData CreateData()
        {
            return new PawnModData_Empty();
        }

        public virtual void Draw(ComplexPawnDef pawnDef, ref float y, Rect inRect, float x)
        {
            using CQFUIScope cqfUIScope = new CQFUIScope(inRect.width);
        }

        public virtual void ModifyGenerationRequest(ComplexPawnDef pawnDef, ref PawnGenerationRequest request)
        {
        }

        public virtual void ApplyToPawn(ComplexPawnDef pawnDef, Pawn pawn, bool preview)
        {
        }

        public virtual IEnumerable<string> GetPreviewApplyKeyParts(ComplexPawnDef pawnDef)
        {
            yield break;
        }

        public virtual void OnPawnSpawned(ComplexPawnDef pawnDef, Pawn pawn, Quest quest)
        {
        }

        protected internal Rect DrawRowLabel(ref float y, Rect inRect, float x, string label, float labelWidth = 150f, float height = 30f)
        {
            Rect labelRect = new Rect(x, y + 3f, labelWidth, 25f);
            Widgets.Label(labelRect, label.Colorize(CQFUIStyle.Accent));
            return new Rect(x + labelWidth + 8f, y, Mathf.Max(120f, inRect.width - x - labelWidth - 24f), height);
        }

        protected internal void EndRow(ref float y, float height = 30f)
        {
            y += height + 8f;
        }

        protected internal bool DrawTextButton(Rect rect, string label, TextAnchor anchor = TextAnchor.MiddleLeft)
        {
            Widgets.DrawHighlightIfMouseover(rect);
            return CQFUIStyle.ButtonText(rect, label, false, true, true, anchor);
        }

        protected internal bool DrawCommandText(Rect rect, string label)
        {
            return this.DrawTextButton(rect, label.Colorize(CQFUIStyle.Accent), TextAnchor.MiddleCenter);
        }

        protected internal bool DrawSelectRow(ref float y, Rect inRect, float x, string label, float height = 30f)
        {
            Rect rect = new Rect(x, y, inRect.width - x - 20f, height);
            bool result = this.DrawTextButton(rect, label);
            this.EndRow(ref y, height);
            return result;
        }

        protected internal void DrawColorRow(ref float y, Rect inRect, float x, string label, Color color, Action<Color> apply)
        {
            this.DrawColorRow(ref y, inRect, x, label, color, apply, null);
        }

        protected internal void DrawColorRow(ref float y, Rect inRect, float x, string label, Color? color, Action<Color> apply, Action clear)
        {
            Rect rect = new Rect(x, y, inRect.width - x - 20f, 30f);
            if (this.DrawTextButton(rect, label))
            {
                this.OpenColorDialog(label, color ?? Color.white, apply, clear);
            }

            if (color != null)
            {
                this.DrawColorSwatch(new Rect(rect.xMax - 32f, rect.y + 3f, 24f, 24f), color.Value);
            }

            this.EndRow(ref y);
        }

        protected internal string ValueOrNone(string value)
        {
            return value.NullOrEmpty() ? "CQF_PawnEditor_None".Translate().ToString() : value;
        }

        protected internal Color Opaque(Color color)
        {
            color.a = 1f;
            return color;
        }

        internal void OpenColorDialog(string label, Color color, Action<Color> apply, Action clear = null)
        {
            List<FloatMenuOption> options = new List<FloatMenuOption>
            {
                new FloatMenuOption("CQF_PawnEditor_ColorLibrary".Translate(), () => Find.WindowStack.Add(new Dialog_ChooseColor(label, color, DefDatabase<ColorDef>.AllDefsListForReading.Select(def => def.color).ToList(), apply))),
                new FloatMenuOption("CQF_PawnEditor_HexColor".Translate(), () => Find.WindowStack.Add(new Dialog_RGB(color, apply)))
            };
            if (clear != null)
            {
                options.Add(new FloatMenuOption("CQF_PawnEditor_UseDefaultSkinColor".Translate(), clear));
            }

            Find.WindowStack.Add(new FloatMenu(options));
        }

        internal void DrawColorSwatch(Rect rect, Color color)
        {
            Widgets.DrawBoxSolid(rect, color);
            CQFUIStyle.DrawBox(rect);
        }

        public PawnModDef def;
    }
}
