using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace QuestEditor_Library
{
    public class Designator_CQFTools : Designator_Place
    {
        public Designator_CQFTools()
        {
            this.defaultLabel = Designator_CQFTools.thing.label.Colorize(ColorLibrary.SkyBlue);
            this.icon = Designator_CQFTools.thing.GetUIIconForStuff(null);
            this.defaultDesc = Designator_CQFTools.thing.description.Colorize(ColorLibrary.SkyBlue);
            this.useMouseIcon = true;
        }
        public override string Desc => base.Desc + "\n" + "CQFToolsTip".Translate();
        public override bool Visible => DebugSettings.godMode;
        public override DrawStyleCategoryDef DrawStyleCategory
        {
            get
            {
                return QEDefOf.CQF_Areas;
            }
        }
        public override BuildableDef PlacingDef => thing;

        public override ThingStyleDef ThingStyleDefForPreview => null;

        public override ThingDef StuffDef => stuff;
        public override Color IconDrawColor => this.PlacingDef.MadeFromStuff && this.StuffDef != null ? this.PlacingDef?.GetColorForStuff(this.StuffDef) ?? base.IconDrawColor : base.IconDrawColor;
        public static List<ThingDef> Basespawnable
        {
            get
            {
                if (Designator_CQFTools.bespawnable.NullOrEmpty())
                {
                    foreach (ThingDef def in DefDatabase<ThingDef>.AllDefsListForReading)
                    {
                        if (Designator_CQFTools.IsCQFTool(def))
                        {
                            Designator_CQFTools.bespawnable.Add(def);
                        }
                    }
                }
                return Designator_CQFTools.bespawnable;
            }
        }
        public static List<Type> ToolTypes => new List<Type>() {typeof(GenerationActionWorker), typeof(LootBox),typeof(CustomContainer), typeof(CustomMapEnterSpot),
            typeof(Spawner), typeof(InteractableThing),typeof(CustomDoor), typeof(CustomMapEntrance), typeof(CustomMapExit) ,typeof(ZoneCore)};
        public static bool IsCQFTool(ThingDef def) 
        {
            return ToolTypes.Exists(t => def.thingClass == t || def.thingClass.IsSubclassOf(t));
        }
        public override IEnumerable<FloatMenuOption> RightClickFloatMenuOptions
        {
            get
            {
                yield return new FloatMenuOption("Select".Translate(), () =>
                 {
                     Find.WindowStack.Add(new Dialog_Select<ThingDef>(new TextureSelectDrawer<ThingDef>(Designator_CQFTools.Basespawnable, x => x.uiIcon, x => x.label + $"({x.thingClass.Name.Translate()})", x =>
            {
                Designator_CQFTools.thing = x;
                this.iconProportions = x.graphicData.drawSize.RotatedBy(x.defaultPlacingRot);
                string label = x.label;
                if (Designator_CQFTools.IsCQFTool(x))
                {
                    label = label.Colorize(ColorLibrary.SkyBlue);
                }
                this.defaultLabel = label;
                stuff = null;
                this.defaultDesc = x.description.Colorize(ColorLibrary.SkyBlue);
                if (x.graphicData.onGroundRandomRotateAngle > 0.01f)
                {
                    this.icon = Widgets.GetIconFor(x);
                }
                else
                {
                    this.icon = x.GetUIIconForStuff(this.StuffDef) ?? x.graphic.MatSingle.mainTexture;
                }
                if (x.MadeFromStuff) 
                {
                    Find.WindowStack.Add(new Dialog_Select<ThingDef>(
                        new TextureSelectDrawer<ThingDef>(
                            GenStuff.AllowedStuffsFor(x).ToList(),
                            s => s.uiIcon,
                            s => s.label,
                            s =>
                            {
                                stuff = s;
                                this.defaultLabel = s.LabelAsStuff.Colorize(ColorLibrary.SkyBlue) + this.defaultLabel;
                                if (x.graphicData.onGroundRandomRotateAngle > 0.01f)
                                {
                                    this.icon = Widgets.GetIconFor(x, s);
                                }
                                else
                                {
                                    this.icon = x.GetUIIconForStuff(s);
                                }
                            },
                            t => t.graphic?.Color ?? Color.white,
                            (t, r) => Widgets.DefIcon(r, t, null)),
                        "SelectStuff".Translate()));
                }
            }, t => t.graphic?.Color ?? Color.white, (t, r) => Widgets.DefIcon(r, t, null)), "Select".Translate()));
                 });
                yield break;
            }
        }

        public override void DesignateSingleCell(IntVec3 loc)
        {
            if (loc.InBounds(Find.CurrentMap))
            {
                ThingDef def = Designator_CQFTools.thing;
                if (loc.GetFirstThing(Find.CurrentMap, def) is Thing thing && thing.stackCount < thing.def.stackLimit)
                {
                    thing.stackCount++;
                    return;
                }
                Thing newThing = GenSpawn.Spawn(ThingMaker.MakeThing(def,def.MadeFromStuff ? this.StuffDef : null), loc, Find.CurrentMap, this.placingRot);
            }
        }
        protected override void DrawGhost(Color ghostCol)
        {
            if (!(this.PlacingDef.graphic is Graphic_Cluster) && (!((ThingDef)this.PlacingDef).graphicData.Linked || this.PlacingDef.uiIconPath != null) && ((ThingDef)this.PlacingDef).graphicData.onGroundRandomRotateAngle < 0.01f)
            {
                base.DrawGhost(ghostCol);
            }
        }
        public override AcceptanceReport CanDesignateCell(IntVec3 loc)
        {
            return true;
        }

        public static ThingDef thing = QEDefOf.QE_Spawner_Editor;
        public static ThingDef stuff = ThingDefOf.WoodLog;
        private static List<ThingDef> bespawnable = new List<ThingDef>();
    }
}
