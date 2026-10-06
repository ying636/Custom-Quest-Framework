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
    public static class CQFAction_AddExtraOpterationEditor
    {
        public static void Draw_0(QuestEditor_Library.CQFAction_AddExtraOpteration cqfReceiver, ref float y, Rect inRect, float x)
        {
            CQFAction_TargetEditor.Draw_0(cqfReceiver, ref y, inRect, x);
            if (Widgets.ButtonText(new Rect(x, y, 250f, 25f), cqfReceiver.option.interactionText, false))
            {
                Find.WindowStack.Add(new Dialog_InteractionOption(cqfReceiver.option));
            }

            y += 30f;
        }

        public static void RealWork_1(QuestEditor_Library.CQFAction_AddExtraOpteration cqfReceiver, Dictionary<string, TargetInfo> targets, Quest quest)
        {
            targets.ToList().ForEach(t =>
            {
                if (t.Value.Thing is Thing thing && thing.Map?.GetComponent<MapComponent_CustomMapData>()is MapComponent_CustomMapData component)
                {
                    if (component.ExtraOperations.TryGetValue(thing, out List<InteractionOperation> os))
                    {
                        os.Add(cqfReceiver.option);
                    }
                    else
                    {
                        CQFEditorTools.AddOrSetObjectToListFromDictionary(component.ExtraOperations, thing, cqfReceiver.option);
                    }
                }
            });
        }
    }
}
