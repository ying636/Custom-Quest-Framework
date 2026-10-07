using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using RimWorld;
using UnityEngine;
using Verse;
using static Verse.PathFinderJob;

namespace QuestEditor_Library
{
    public static class ZoneCondition_CoreTagEditor
    {
        public static void Draw_0(QuestEditor_Library.ZoneCondition_CoreTag cqfReceiver, ref float y, Rect inRect, float x)
        {
            using CQFUIScope cqfUIScope = new CQFUIScope(inRect.width);
            ZoneConditionEditor.Draw_0(cqfReceiver, ref y, inRect, x);
            CQFEditorTools.DrawEditableStringList(cqfReceiver.coreTags, ref y, "CoreTags".Translate(), null, true, x);
            Widgets.CheckboxLabeled(new Rect(x, y, 250f, 25f), "InvertResult".Translate(), ref cqfReceiver.invert);
        }
    }
}
