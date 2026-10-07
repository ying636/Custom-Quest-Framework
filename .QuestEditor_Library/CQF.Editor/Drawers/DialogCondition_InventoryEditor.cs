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
    public static class DialogCondition_InventoryEditor
    {
        public static void Draw_0(QuestEditor_Library.DialogCondition_Inventory cqfReceiver, ref float y, Rect inRect, float x)
        {
            using CQFUIScope cqfUIScope = new CQFUIScope(inRect.width);
            DialogCondition_TargetEditor.Draw_0(cqfReceiver, ref y, inRect, x);
            CQFEditorTools.DrawIDrawList(ref y, x, cqfReceiver.requirations, inRect, "RequiredThings".Translate(), () => CQFEditorTools.DrawFloatMenu(new List<Type>() { typeof(CQFThingDefCount) }, t =>
            {
                CQFThingData.OpenSelectWindow(t, d => cqfReceiver.requirations.Add(d));
            }, t => t.Name.Translate()), t => t.ToString(), (t, y2, rect, x2) =>
            {
                t.DrawWithSingleCount(ref y2, rect, x2);
                return y2;
            });
            y += 5f;
        }
    }
}
