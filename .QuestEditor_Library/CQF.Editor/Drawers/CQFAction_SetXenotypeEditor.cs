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
    public static class CQFAction_SetXenotypeEditor
    {
        public static void Draw_0(QuestEditor_Library.CQFAction_SetXenotype cqfReceiver, ref float y, Rect inRect, float x)
        {
            CQFAction_TargetEditor.Draw_0(cqfReceiver, ref y, inRect, x);
            Rect rect = new Rect(x, y, 150f, 25f);
            if (Widgets.ButtonText(rect, "CQFXenotypeDef".Translate(cqfReceiver.xenotype?.label), false))
            {
                CQFEditorTools.DrawFloatMenu<XenotypeDef>(DefDatabase<XenotypeDef>.AllDefsListForReading, (d) => cqfReceiver.xenotype = d, (d) => d.label.Translate());
            }

            y += 30f;
        }
    }
}
