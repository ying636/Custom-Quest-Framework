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
    public static class ZoneCondition_TagEditor
    {
        public static void Draw_0(QuestEditor_Library.ZoneCondition_Tag cqfReceiver, ref float y, Rect inRect, float x)
        {
            ZoneConditionEditor.Draw_0(cqfReceiver, ref y, inRect, x);
            CQFEditorTools.DrawEditableStringList(cqfReceiver.tags, ref y, "CustomMapTags".Translate(), null, true, x);
        }
    }
}
