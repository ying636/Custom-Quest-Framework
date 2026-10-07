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
    public static class ZoneCondition_SizeEditor
    {
        public static void Draw_0(QuestEditor_Library.ZoneCondition_Size cqfReceiver, ref float y, Rect inRect, float x)
        {
            using CQFUIScope cqfUIScope = new CQFUIScope(inRect.width);
            ZoneConditionEditor.Draw_0(cqfReceiver, ref y, inRect, x);
            CQFEditorTools.DrawIntRange(ref y, "RangeOfX".Translate(), ref cqfReceiver.x, ref cqfReceiver.buffer, ref cqfReceiver.buffer1, x, 150f);
            CQFEditorTools.DrawIntRange(ref y, "RangeOfZ".Translate(), ref cqfReceiver.z, ref cqfReceiver.buffer2, ref cqfReceiver.buffer3, x, 150f);
        }
    }
}
