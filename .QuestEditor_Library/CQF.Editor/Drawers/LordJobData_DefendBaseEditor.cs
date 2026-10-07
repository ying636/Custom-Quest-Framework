using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using UnityEngine;
using Verse;
using Verse.AI.Group;

namespace QuestEditor_Library
{
    public static class LordJobData_DefendBaseEditor
    {
        public static void Draw_0(QuestEditor_Library.LordJobData_DefendBase cqfReceiver, ref float y, Rect inRect, float x)
        {
            using CQFUIScope cqfUIScope = new CQFUIScope(inRect.width);
            LordJobDataEditor.Draw_0(cqfReceiver, ref y, inRect, x);
            CQFEditorTools.DrawLabelAndText_Line(y, "TargetPositionName".Translate(), ref cqfReceiver.targetPositionName, x, 150);
            y += 30f;
            CQFEditorTools.DrawLabelAndText_Line(y, "PawnDataFaction".Translate(), ref cqfReceiver.faction, x, 150);
            y += 30f;
        }
    }
}
