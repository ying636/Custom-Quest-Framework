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
    public static class CQFAction_LinkEntranceAndExitEditor
    {
        public static void Draw_0(QuestEditor_Library.CQFAction_LinkEntranceAndExit cqfReceiver, ref float y, Rect inRect, float x)
        {
            CQFActionEditor.Draw_0(cqfReceiver, ref y, inRect, x);
            CQFTargetKeyEditor.DrawBookField(y, "EntranceKey".Translate(), cqfReceiver.entranceText, value => cqfReceiver.entranceText = value, x, 150f, inRect.width - x - 20f);
            y += 30f;
            CQFTargetKeyEditor.DrawBookField(y, "ExitKey".Translate(), cqfReceiver.exitText, value => cqfReceiver.exitText = value, x, 150f, inRect.width - x - 20f);
            y += 30f;
        }
    }
}
