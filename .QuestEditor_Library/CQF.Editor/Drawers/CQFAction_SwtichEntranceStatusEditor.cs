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
    public static class CQFAction_SwtichEntranceStatusEditor
    {
        public static void Draw_0(QuestEditor_Library.CQFAction_SwtichEntranceStatus cqfReceiver, ref float y, Rect inRect, float x)
        {
            CQFAction_TargetEditor.Draw_0(cqfReceiver, ref y, inRect, x);
            Widgets.CheckboxLabeled(new Rect(15f, y, 350f, 25f), "OpeningStatus".Translate(), ref cqfReceiver.value);
            y += 35f;
            Widgets.CheckboxLabeled(new Rect(15f, y, 350f, 25f), "AlwaysIsOpposite".Translate(), ref cqfReceiver.alwaysIsOpposite);
            y += 30f;
        }
    }
}
