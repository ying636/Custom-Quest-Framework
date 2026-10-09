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
            this.defaultLabel = Designator_CQFTools.thing.label.Colorize(CQFUIStyle.Accent);
            this.icon = Designator_CQFTools.thing.GetUIIconForStuff(null);
            this.defaultDesc = Designator_CQFTools.thing.description.Colorize(CQFUIStyle.Accent);
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

        public override ThingDef? StuffDef => stuff;
        public override Color IconDrawColor => this.PlacingDef.MadeFromStuff && this.StuffDef != null ? this.PlacingDef?.GetColorForStuff(this.StuffDef) ?? base.IconDrawColor : base.IconDrawColor;
        public static IReadOnlyList<DesignatorThingSelection> RecentSelections => recentSelections;
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
            return IsSpecialBuilding(def)
                || ToolTypes.Exists(t => def.thingClass == t || def.thingClass.IsSubclassOf(t));
        }
        public static bool IsSpecialBuilding(ThingDef def)
        {
            return def.defName == "QF_MiracleWall";
        }
        public static string GetCQFToolTypeLabel(ThingDef def)
        {
            return IsSpecialBuilding(def) ? "CQFSpecialBuilding".Translate() : def.thingClass.Name.Translate();
        }
        public override IEnumerable<FloatMenuOption> RightClickFloatMenuOptions
        {
            get
            {
                yield return new FloatMenuOption("Select".Translate(), () =>
                 {
                     Find.WindowStack.Add(new Dialog_Select<ThingDef>(new TextureSelectDrawer<ThingDef>(Designator_CQFTools.Basespawnable, x => x.uiIcon, x => x.label + $"({Designator_CQFTools.GetCQFToolTypeLabel(x)})", x =>
            {
                this.SelectThing(x);
            }, t => t.graphic?.Color ?? Color.white, (t, r) => Widgets.DefIcon(r, t, null)), "Select".Translate()));
                 });
                yield return new FloatMenuOption("CQF_OpenFloatingPalette".Translate(), () =>
                {
                    Find.WindowStack.Add(new Window_DesignatorPalette<DesignatorThingSelection>(
                        this, Basespawnable.Select(def => new DesignatorThingSelection(def)), RecentSelections,
                        item => (item.Stuff == null ? "" : item.Stuff.LabelAsStuff + " ")
                            + (item.Thing.label ?? item.Thing.defName) + " (" + GetCQFToolTypeLabel(item.Thing) + ")",
                        (item, rect) => Widgets.DefIcon(rect, item.Thing, item.Stuff, drawPlaceholder: true),
                        item =>
                        {
                            if (item.Stuff == null) this.SelectThing(item.Thing);
                            else this.SelectThing(item.Thing, item.Stuff);
                        },
                        item => item.Thing == thing && (item.Stuff == null || item.Stuff == stuff),
                        item => (item.Stuff == null ? "" : item.Stuff.LabelAsStuff + " ")
                            + (item.Thing.label ?? item.Thing.defName) + " (" + GetCQFToolTypeLabel(item.Thing) + ")"
                            + (item.Thing.description.NullOrEmpty() ? "" : "\n\n" + item.Thing.description)));
                });
                yield break;
            }
        }

        public void SelectThing(ThingDef def)
        {
            if (def.MadeFromStuff)
            {
                Find.WindowStack.Add(new Dialog_Select<ThingDef>(
                    new TextureSelectDrawer<ThingDef>(
                        GenStuff.AllowedStuffsFor(def).ToList(),
                        selectedStuff => selectedStuff.uiIcon,
                        selectedStuff => selectedStuff.label,
                        selectedStuff => this.SelectThing(def, selectedStuff),
                        selectedStuff => selectedStuff.graphic?.Color ?? Color.white,
                        (selectedStuff, rect) => Widgets.DefIcon(rect, selectedStuff, null)),
                    "SelectStuff".Translate()));
                return;
            }
            this.SelectThing(def, null);
        }

        public void SelectThing(ThingDef def, ThingDef? stuffDef)
        {
            Designator_CQFTools.thing = def;
            Designator_CQFTools.stuff = stuffDef;
            this.defaultLabel = def.label.Colorize(CQFUIStyle.Accent);
            this.defaultDesc = def.description.Colorize(CQFUIStyle.Accent);
            if (stuffDef != null)
            {
                this.defaultLabel = stuffDef.LabelAsStuff.Colorize(CQFUIStyle.Accent) + this.defaultLabel;
            }
            if (def.drawerType != DrawerType.None && def.graphicData != null)
            {
                this.iconProportions = def.graphicData.drawSize.RotatedBy(def.defaultPlacingRot);
                if (def.graphicData.onGroundRandomRotateAngle > 0.01f)
                {
                    this.icon = Widgets.GetIconFor(def, stuffDef);
                }
                else
                {
                    this.icon = def.GetUIIconForStuff(stuffDef) ?? def.graphic?.MatSingle?.mainTexture ?? def.uiIcon;
                }
            }
            this.RecordRecentSelection(def, stuffDef);
            Find.DesignatorManager.Select(this);
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
        public override void RenderHighlight(List<IntVec3> dragCells)
        {
            DesignatorUtility.RenderHighlightOverSelectableCells(this, dragCells);
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

        private void RecordRecentSelection(ThingDef def, ThingDef? stuffDef)
        {
            recentSelections.RemoveAll(selection => selection.Thing == def && selection.Stuff == stuffDef);
            recentSelections.Insert(0, new DesignatorThingSelection(def, stuffDef));
            if (recentSelections.Count > RecentSelectionLimit)
            {
                recentSelections.RemoveRange(RecentSelectionLimit, recentSelections.Count - RecentSelectionLimit);
            }
        }

        public static ThingDef thing = QEDefOf.QE_Spawner_Editor;
        public static ThingDef? stuff = ThingDefOf.WoodLog;
        private const int RecentSelectionLimit = 5;
        private static readonly List<DesignatorThingSelection> recentSelections = new List<DesignatorThingSelection>();
        private static List<ThingDef> bespawnable = new List<ThingDef>();
    }
}
