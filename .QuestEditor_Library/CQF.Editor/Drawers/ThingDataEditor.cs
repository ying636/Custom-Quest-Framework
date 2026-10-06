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
    public static class ThingDataEditor
    {
        public static void OpenSelectDialog_0(QuestEditor_Library.ThingData cqfReceiver)
        {
            Find.WindowStack.Add(new Dialog_Select<ThingDef>(new TextureSelectDrawer<ThingDef>(Designator_SpawnThing.Bespawnable, t => t.uiIcon, t => t.label, t =>
            {
                cqfReceiver.def = t;
                cqfReceiver.hitPoint = t.BaseMaxHitPoints;
                if (t.MadeFromStuff)
                {
                    Find.WindowStack.Add(new Dialog_Select<ThingDef>(new TextureSelectDrawer<ThingDef>(GenStuff.AllowedStuffsFor(t).ToList(), s => s.uiIcon, s => s.label, s =>
                    {
                        cqfReceiver.stuff = s;
                        cqfReceiver.hitPoint = (int)(t.BaseMaxHitPoints * (s.stuffProps.statFactors.Find(s2 => s2.stat == StatDefOf.MaxHitPoints)is StatModifier stat ? stat.value : 1f));
                    }, t2 => t2.graphic?.Color ?? Color.white), "SelectStuff".Translate()));
                }
            }, t => t.graphic?.Color ?? Color.white), "Select".Translate()));
        }
    }
}
