using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using System.Xml;
using System.Xml.Linq;
using RimWorld.QuestGen;
using Verse.Grammar;
using System.Reflection;
using UnityEngine;
using System.Collections;
using Verse.AI;
using Verse.AI.Group;
using System.IO;
using Unity.Collections;
using RimWorld.Planet;
using System.Net.NetworkInformation;
using System.Text;

namespace QuestEditor_Library
{
    public static class CQFAction_MessageEditor
    {
        public static void Draw_0(QuestEditor_Library.CQFAction_Message cqfReceiver, ref float y, Rect inRect, float x)
        {
            using CQFUIScope cqfUIScope = new CQFUIScope(inRect.width);
            CQFActionEditor.Draw_0(cqfReceiver, ref y, inRect, x);
            CQFEditorTools.DrawLabelAndText_Line(y, "CQFMessage".Translate(), ref cqfReceiver.message, x, 240f);
            y += 30f;
            if (CQFUIStyle.ButtonText(new Rect(x, y, Mathf.Max(40f, inRect.width - x - 12f), 25f), "CQFMessageType".Translate(cqfReceiver.type?.defName.Translate().ToString()), false))
            {
                CQFEditorTools.DrawFloatMenu(DefDatabase<MessageTypeDef>.AllDefsListForReading, (d) => cqfReceiver.type = d, (d) => d.defName.Translate());
            }

            y += 30f;
        }
    }
}
