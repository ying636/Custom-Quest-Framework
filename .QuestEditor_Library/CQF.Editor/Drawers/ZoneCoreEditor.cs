using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using RimWorld;
using UnityEngine;
using Verse;
using static Verse.PathFinderJob;

namespace QuestEditor_Library
{
    public static class ZoneCoreEditor
    {
        public static void DrawTab_0(QuestEditor_Library.ZoneCore cqfReceiver)
        {
            using CQFUIScope scope = new CQFUIScope();
            Rect viewport = new Rect(8f, 36f, Mathf.Min(490f, CQFUIScope.ContentWidth - 16f), Mathf.Max(40f, CQFUIScope.ContentHeight - 44f));
            Rect content = new Rect(0f, 0f, viewport.width - 20f, Mathf.Max(viewport.height, cqfReceiver.height));
            Widgets.BeginScrollView(viewport, ref cqfReceiver.scrollPos, content);
            using CQFUIScope contentScope = new CQFUIScope(content.width);
            float y = 10f;
            float x = 7f;
            Rect rect = new Rect(x, y, Mathf.Min(250f, content.width - x - 52f), 25f);
            Func<Rot4, string> GetText = r => r == Rot4.Invalid ? "Rot_Invalid".Translate().ToString() : r.ToStringHuman().Translate().ToString();
            if (CQFUIStyle.ButtonText(rect, "CoreZoneRotation".Translate(cqfReceiver.isCenter ? "Rot_Invalid".Translate().ToString() : GetText(cqfReceiver.coreRotation)), false))
            {
                CQFEditorTools.DrawFloatMenu(new List<Rot4>() { Rot4.West, Rot4.East, Rot4.North, Rot4.South, Rot4.Invalid }, (r) =>
                {
                    cqfReceiver.coreRotation = r;
                    cqfReceiver.isCenter = !r.IsValid;
                }, (r) => GetText(r));
            }

            TooltipHandler.TipRegion(rect, "CoreZoneRotationTip".Translate());
            Rect rectCP = new Rect(content.width - 40f, y, 25f, 25f);
            if (CQFUIStyle.ButtonImage(rectCP, TexButton.Copy))
            {
                cqfReceiver.CopyData();
            }

            TooltipHandler.TipRegion(rectCP, "Copy".Translate());
            y += 30f;
            Rect reserveRect = new Rect(x, y, content.width - x - 88f, 25f);
            if (CQFUIStyle.ButtonText(reserveRect, "ReserveGenerationThing".Translate(cqfReceiver.reserveThing == null ? "NoGenerate".Translate().ToString() : cqfReceiver.reserveThing?.stuff?.label + cqfReceiver.reserveThing?.def?.label), false))
            {
                List<FloatMenuOption> options = new List<FloatMenuOption>();
                options.Add(new FloatMenuOption("NoGenerate".Translate(), () => cqfReceiver.reserveThing = null));
                options.Add(new FloatMenuOption("Select".Translate(), () =>
                {
                    cqfReceiver.reserveThing = new ThingData();
                    Find.WindowStack.Add(new Dialog_Select<ThingDef>(new TextureSelectDrawer<ThingDef>(Designator_SpawnThing.Bespawnable, t => t.uiIcon, t => t.label, t =>
                    {
                        cqfReceiver.reserveThing.def = t;
                        cqfReceiver.reserveThing.hitPoint = t.BaseMaxHitPoints;
                        if (t.MadeFromStuff)
                        {
                            Find.WindowStack.Add(new Dialog_Select<ThingDef>(new TextureSelectDrawer<ThingDef>(GenStuff.AllowedStuffsFor(t).ToList(), s => s.uiIcon, s => s.label, s =>
                            {
                                cqfReceiver.reserveThing.stuff = s;
                                cqfReceiver.reserveThing.hitPoint = (int)(t.BaseMaxHitPoints * (s.stuffProps.statFactors.Find(s2 => s2.stat == StatDefOf.MaxHitPoints)is StatModifier stat ? stat.value : 1f));
                            }, t2 => t2.graphic?.Color ?? Color.white, (d, r) => Widgets.DefIcon(r, d, null)), "SelectStuff".Translate()));
                        }
                    }, t => t.graphic?.Color ?? Color.white), "Select".Translate()));
                }));
                Find.WindowStack.Add(new FloatMenu(options));
            }

            TooltipHandler.TipRegion(reserveRect, "ReserveGenerationThingTip".Translate());
            Rect copy = new Rect(content.width - 70f, y, 25f, 25f);
            if (CQFUIStyle.ButtonImage(copy, TexButton.Copy))
            {
                CQFEditorTools.thingData = cqfReceiver.reserveThing;
            }

            copy.x += 30f;
            if (CQFUIStyle.ButtonImage(copy, TexButton.Paste))
            {
                cqfReceiver.reserveThing = CQFEditorTools.thingData;
            }

            y += 30f;
            Rect generationKeyRect = new Rect(x, y, 300f, 25f);
            CQFEditorTools.DrawLabelAndText_Line(y, "GenerationKey".Translate(), ref cqfReceiver.generationKey, x, 150f);
            TooltipHandler.TipRegion(generationKeyRect, "GenerationKeyTip".Translate());
            y += 30f;
            Rect destroyThingsRect = new Rect(x, y, 300f, 25f);
            Widgets.CheckboxLabeled(destroyThingsRect, "DestroyThingsWhenGeneration".Translate(), ref cqfReceiver.destroyThings);
            TooltipHandler.TipRegion(destroyThingsRect, "DestroyThingsWhenGenerationTip".Translate());
            y += 30f;
            Rect prohibitRotatingRect = new Rect(x, y, 300f, 25f);
            Widgets.CheckboxLabeled(prohibitRotatingRect, "ProhibitRotatingDocking".Translate(), ref cqfReceiver.prohibitRotatingDocking);
            TooltipHandler.TipRegion(prohibitRotatingRect, "ProhibitRotatingDockingTip".Translate());
            y += 30f;
            Rect prohibitFlippingRect = new Rect(x, y, 300f, 25f);
            Widgets.CheckboxLabeled(prohibitFlippingRect, "ProhibitFlippingDocking".Translate(), ref cqfReceiver.prohibitFlippingDocking);
            TooltipHandler.TipRegion(prohibitFlippingRect, "ProhibitFlippingDockingTip".Translate());
            y += 30f;
            Rect coreTagsRect = new Rect(x, y, content.width - x - 12f, 30f);
            CQFEditorTools.DrawEditableStringList(cqfReceiver.coreTags, ref y, "CoreTags".Translate(), null, true, x, 360f);
            TooltipHandler.TipRegion(coreTagsRect, "CoreTagsTip".Translate());
            y += 5f;
            Rect conditionsRect = new Rect(x, y, content.width - x - 12f, 30f);
            CQFEditorTools.DrawIDrawList(ref y, x, cqfReceiver.conditions, content, "ZoneGenerationConditions".Translate());
            TooltipHandler.TipRegion(conditionsRect, "ZoneGenerationConditionsTip".Translate());
            y += 40;
            Widgets.EndScrollView();
            cqfReceiver.height = y;
        }

        public static void PasteData_1(QuestEditor_Library.ZoneCore cqfReceiver)
        {
            cqfReceiver.coreRotation = CQFEditorTools.coreRotation;
            cqfReceiver.generationKey = CQFEditorTools.generationKey;
            cqfReceiver.isCenter = CQFEditorTools.isCenter;
            cqfReceiver.reserveThing = CQFEditorTools.reserveThing;
            cqfReceiver.prohibitRotatingDocking = CQFEditorTools.prohibitRotatingDocking;
            cqfReceiver.prohibitFlippingDocking = CQFEditorTools.prohibitFlippingDocking;
            cqfReceiver.conditions.Clear();
            CQFEditorTools.conditions.ForEach(c => cqfReceiver.conditions.Add(c.Copy()));
            cqfReceiver.coreTags = CQFEditorTools.coreTags.ListFullCopy();
        }

        public static void CopyData_2(QuestEditor_Library.ZoneCore cqfReceiver)
        {
            CQFEditorTools.coreRotation = cqfReceiver.coreRotation;
            CQFEditorTools.isCenter = cqfReceiver.isCenter;
            CQFEditorTools.reserveThing = cqfReceiver.reserveThing;
            CQFEditorTools.generationKey = cqfReceiver.generationKey;
            CQFEditorTools.prohibitRotatingDocking = cqfReceiver.prohibitRotatingDocking;
            CQFEditorTools.prohibitFlippingDocking = cqfReceiver.prohibitFlippingDocking;
            CQFEditorTools.destroyThings = cqfReceiver.destroyThings;
            CQFEditorTools.conditions = cqfReceiver.conditions.ListFullCopy();
            CQFEditorTools.coreTags = cqfReceiver.coreTags.ListFullCopy();
        }
    }
}
