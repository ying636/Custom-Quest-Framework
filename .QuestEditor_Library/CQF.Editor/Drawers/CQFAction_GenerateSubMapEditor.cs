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
    public static class CQFAction_GenerateSubMapEditor
    {
        public static void Draw_0(QuestEditor_Library.CQFAction_GenerateSubMap cqfReceiver, ref float y, Rect inRect, float x)
        {
            using CQFUIScope cqfUIScope = new CQFUIScope(inRect.width);
            CQFAction_TargetEditor.Draw_0(cqfReceiver, ref y, inRect, x);
            CQFEditorTools.DrawIntVector(ref y, "StartPosition".Translate(), ref cqfReceiver.pos, ref cqfReceiver.p_X, ref cqfReceiver.p_Z, ref cqfReceiver.p_Y, x, 60f);
            y += 30f;
            if (CQFUIStyle.ButtonText(new Rect(x, y + 5f, 100f, 25f), "EditCustomMapGenerationSet".Translate(), false))
            {
                Find.WindowStack.Add(new Dialog_EditIDrawable(cqfReceiver.set));
            }

            y += 30f;
        }
    }
}
