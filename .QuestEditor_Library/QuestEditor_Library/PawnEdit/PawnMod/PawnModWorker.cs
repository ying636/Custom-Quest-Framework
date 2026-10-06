using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using System.Xml;
using RimWorld;
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
            object[] arguments = new object[]
            {
                pawnDef,
                y,
                inRect,
                x
            };
            CQFEditorBridge.Invoke("QuestEditor_Library.PawnModWorker.Draw(None:QuestEditor_Library.ComplexPawnDef,Ref:float,None:UnityEngine.Rect,None:float)", this, arguments);
            y = (float)arguments[1];
        }
        public virtual void ModifyGenerationRequest(ComplexPawnDef pawnDef, ref PawnGenerationRequest request)
        {
        }

        public virtual void ApplyToPawn(ComplexPawnDef pawnDef, Pawn pawn, bool preview)
        {
        }

        public virtual void SaveData(ComplexPawnDef pawnDef, XElement root)
        {
        }

        public virtual void LoadData(ComplexPawnDef pawnDef, XmlNode node)
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
            object[] arguments = new object[]
            {
                y,
                inRect,
                x,
                label,
                labelWidth,
                height
            };
            object result = CQFEditorBridge.Invoke("QuestEditor_Library.PawnModWorker.DrawRowLabel(Ref:float,None:UnityEngine.Rect,None:float,None:string,None:float,None:float)", this, arguments);
            y = (float)arguments[0];
            return (UnityEngine.Rect)result;
        }
        protected internal void EndRow(ref float y, float height = 30f)
        {
            y += height + 8f;
        }

        protected internal bool DrawTextButton(Rect rect, string label, TextAnchor anchor = TextAnchor.MiddleLeft)
        {
            Widgets.DrawHighlightIfMouseover(rect);
            return Widgets.ButtonText(rect, label, false, true, true, anchor);
        }

        protected internal bool DrawCommandText(Rect rect, string label)
        {
            return this.DrawTextButton(rect, label.Colorize(ColorLibrary.PaleBlue), TextAnchor.MiddleCenter);
        }

        protected internal bool DrawSelectRow(ref float y, Rect inRect, float x, string label, float height = 30f)

        {
            object[] arguments = new object[]
            {
                y,
                inRect,
                x,
                label,
                height
            };
            object result = CQFEditorBridge.Invoke("QuestEditor_Library.PawnModWorker.DrawSelectRow(Ref:float,None:UnityEngine.Rect,None:float,None:string,None:float)", this, arguments);
            y = (float)arguments[0];
            return (bool)result;
        }
        protected internal void DrawColorRow(ref float y, Rect inRect, float x, string label, Color color, Action<Color> apply)

        {
            object[] arguments = new object[]
            {
                y,
                inRect,
                x,
                label,
                color,
                apply
            };
            CQFEditorBridge.Invoke("QuestEditor_Library.PawnModWorker.DrawColorRow(Ref:float,None:UnityEngine.Rect,None:float,None:string,None:UnityEngine.Color,None:System.Action<UnityEngine.Color>)", this, arguments);
            y = (float)arguments[0];
        }
        protected internal void DrawColorRow(ref float y, Rect inRect, float x, string label, Color? color, Action<Color> apply, Action clear)

        {
            object[] arguments = new object[]
            {
                y,
                inRect,
                x,
                label,
                color,
                apply,
                clear
            };
            CQFEditorBridge.Invoke("QuestEditor_Library.PawnModWorker.DrawColorRow(Ref:float,None:UnityEngine.Rect,None:float,None:string,None:UnityEngine.Color?,None:System.Action<UnityEngine.Color>,None:System.Action)", this, arguments);
            y = (float)arguments[0];
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

        protected void AddText(XElement root, string name, string value)
        {
            if (!value.NullOrEmpty())
            {
                root.Add(new XElement(name, value));
            }
        }

        protected void AddDef(XElement root, string name, Def value)
        {
            if (value != null)
            {
                root.Add(new XElement(name, value.defName));
            }
        }

        protected void AddColor(XElement root, string name, Color? value)
        {
            if (value != null)
            {
                Color color = value.Value;
                root.Add(new XElement(name, $"({color.r}, {color.g}, {color.b}, {color.a})"));
            }
        }

        protected List<T> LoadSaveableList<T>(XmlNode node)
        {
            List<T> result = new List<T>();
            if (node == null)
            {
                return result;
            }
            foreach (XmlNode li in node.SelectNodes("li"))
            {
                result.Add(DirectXmlToObject.ObjectFromXml<T>(li, false));
            }
            return result;
        }
        internal void OpenColorDialog(string label, Color color, Action<Color> apply, Action clear = null)

        {
            object[] arguments = new object[]
            {
                label,
                color,
                apply,
                clear
            };
            CQFEditorBridge.Invoke("QuestEditor_Library.PawnModWorker.OpenColorDialog(None:string,None:UnityEngine.Color,None:System.Action<UnityEngine.Color>,None:System.Action)", this, arguments);
        }
        internal void DrawColorSwatch(Rect rect, Color color)
        {
            Widgets.DrawBoxSolid(rect, color);
            Widgets.DrawBox(rect);
        }

        public PawnModDef def;
    }
}
