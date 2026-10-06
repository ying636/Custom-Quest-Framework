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
    public static class CQFAction_SpawnCustomThingEditor
    {
        public static void Draw_0(QuestEditor_Library.CQFAction_SpawnCustomThing cqfReceiver, ref float y, Rect inRect, float x)
        {
            CQFAction_TargetEditor.Draw_0(cqfReceiver, ref y, inRect, x);
            if (cqfReceiver.data != null && cqfReceiver.customThing == null)
            {
                cqfReceiver.customThing = cqfReceiver.data.SpawnThing(null, null, out List<Thing> ts, null, true);
                ;
            }

            if (Widgets.ButtonText(new Rect(x, y, 250f, 25f), "CQFSpawnThing".Translate(cqfReceiver.customThing == null ? "" : ((Thing)cqfReceiver.customThing).Label), false))
            {
                Find.WindowStack.Add(new Dialog_Select<ThingDef>(new TextureSelectDrawer<ThingDef>(Designator_CQFTools.Basespawnable.FindAll(sp => sp != QEDefOf.QE_Spawner_Editor && sp != QEDefOf.QE_ZoneCore), t => t.uiIcon, t => t.label.Colorize(ColorLibrary.SkyBlue) + "(" + t.thingClass.Name.Translate() + ")", t =>
                {
                    if (t.MadeFromStuff)
                    {
                        Find.WindowStack.Add(new Dialog_Select<ThingDef>(new TextureSelectDrawer<ThingDef>(GenStuff.AllowedStuffsFor(t).ToList(), s => s.uiIcon, s => s.label, s => cqfReceiver.customThing = GameTools.MakeThingWithoutID(t, s), s => s.graphic?.Color ?? Color.white, (s, r) => Widgets.DefIcon(r, s, null)), "SelectStuff".Translate()));
                    }
                    else
                    {
                        cqfReceiver.customThing = GameTools.MakeThingWithoutID(t);
                    }
                }, t => t.graphic?.Color ?? Color.white, (t, r) => Widgets.DefIcon(r, t, null)), "Select".Translate()));
            }

            y += 30f;
            if (cqfReceiver.customThing != null)
            {
                if (Widgets.ButtonText(new Rect(x, y, 250f, 25f), "EditCustomThing".Translate(), false))
                {
                    Find.WindowStack.Add(new Dialog_EditIDrawTabable((IDrawTabable)cqfReceiver.customThing));
                }

                y += 30f;
                if (Widgets.ButtonText(new Rect(x, y, 250f, 25f), "EditActionAndText".Translate(), false))
                {
                    Find.WindowStack.Add(new Dialog_EditIActionAndText((Thing)cqfReceiver.customThing));
                }

                y += 30f;
            }

            CQFEditorTools.DrawLabelAndText_Line(y, "RecordKeyOfData".Translate(), ref cqfReceiver.key, x, 150f);
            y += 30f;
        }
    }
}
