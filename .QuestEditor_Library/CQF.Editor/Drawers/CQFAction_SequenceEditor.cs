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
    public static class CQFAction_SequenceEditor
    {
        public static void Draw_0(QuestEditor_Library.CQFAction_Sequence cqfReceiver, ref float y, Rect inRect, float x)
        {
            using CQFUIScope cqfUIScope = new CQFUIScope(inRect.width);
            CQFActionEditor.Draw_0(cqfReceiver, ref y, inRect, x);
            CQFEditorTools.DrawActionList_UseWindow(ref y, x, cqfReceiver.actions, inRect, "TriggerActions".Translate(), a => a.GetType().Name.Translate());
            y += 30f;
        }
    }
}
