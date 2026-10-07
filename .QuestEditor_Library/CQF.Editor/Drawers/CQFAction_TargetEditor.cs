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
    public static class CQFAction_TargetEditor
    {
        public static void Draw_0(QuestEditor_Library.CQFAction_Target cqfReceiver, ref float y, Rect inRect, float x)
        {
            using CQFUIScope cqfUIScope = new CQFUIScope(inRect.width);
            CQFActionEditor.Draw_0(cqfReceiver, ref y, inRect, x);
            cqfReceiver.targetsText ??= new List<string>();
            for (int index = 0; index < cqfReceiver.targetsText.Count; index++)
            {
                int capturedIndex = index;
                Rect targetRect = new Rect(inRect.x, inRect.y, inRect.width - 78f, inRect.height);
                float startY = y;
                CQFTargetSelectionSession.DrawField(ref y, targetRect, x, cqfReceiver.targetsText[index], value =>
                {
                    if (capturedIndex < cqfReceiver.targetsText.Count)
                    {
                        cqfReceiver.targetsText[capturedIndex] = value;
                    }
                });
                if (CQFUIStyle.ButtonText(new Rect(inRect.width - 78f, startY, 70f, 25f), "Remove".Translate()))
                {
                    cqfReceiver.targetsText.RemoveAt(index);
                    break;
                }
            }

            if (CQFUIStyle.ButtonText(new Rect(x, y, 140f, 26f), "Add".Translate()))
            {
                cqfReceiver.targetsText.Add(string.Empty);
            }

            y += 36f;
        }
    }
}
