using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace QuestEditor_Library
{
    public class Designator_SpawnThing : Designator_CQFPlace
    {
        public Designator_SpawnThing()
        {
            this.defaultLabel = Designator_SpawnThing.thing.label;
            this.icon = Designator_SpawnThing.thing.GetUIIconForStuff(this.StuffDef);
            this.defaultDesc = Designator_SpawnThing.thing.description;
            this.useMouseIcon = true;
        }
        public override bool Visible => DebugSettings.godMode;
        public override string Label => "CQFSpawnThing".Translate(base.Label);
        public override string Desc => base.Desc + "\n" + "CQFSpawnThingTip".Translate();
        public override BuildableDef PlacingDef => Designator_SpawnThing.thing;
        public override ThingStyleDef ThingStyleDefForPreview => Designator_SpawnThing.style;
        public override ThingDef StuffDef => stuff;
        public override Color IconDrawColor => this.PlacingDef.MadeFromStuff && this.StuffDef != null ? this.PlacingDef?.GetColorForStuff(this.StuffDef) ?? this.PlacingDef.graphic?.Color ?? base.IconDrawColor : this.PlacingDef.graphic?.Color ?? base.IconDrawColor;
        public static IReadOnlyList<DesignatorThingSelection> RecentSelections => recentSelections;
        public static List<ThingDef> Bespawnable
        {
            get
            {
                if (Designator_SpawnThing.bespawnable.NullOrEmpty())
                {
                    List<ThingCategory> disable = new List<ThingCategory>()
                    {
                        ThingCategory.Gas, ThingCategory.Mote, ThingCategory.Projectile,
                        ThingCategory.Pawn, ThingCategory.Attachment };
                    foreach (ThingDef def in DefDatabase<ThingDef>.AllDefsListForReading)
                    {
                        if (!disable.Contains(def.category)
                            && !def.IsCorpse && !def.IsFrame && !Designator_CQFTools.IsCQFTool(def))
                        {
                            Designator_SpawnThing.bespawnable.Add(def);
                        }
                    }
                }
                return bespawnable;
            }
        }
        public override IEnumerable<FloatMenuOption> RightClickFloatMenuOptions
        {
            get
            {
                yield return new FloatMenuOption("Select".Translate(), () =>
                 {
                     Find.WindowStack.Add(new Dialog_Select<ThingDef>(new TextureSelectDrawer<ThingDef>(Bespawnable, x => x.uiIcon, x => x.label, x =>
            {
                this.SelectThing(x);
            }, t => t.graphic?.Color ?? Color.white, (t, r) => Widgets.DefIcon(r, t, null),
                getRawText: t => t.defName, typeFilters: GetCategoryFilters()), "Select".Translate()));
                 });
                yield return new FloatMenuOption("CQF_OpenFloatingPalette".Translate(), () =>
                {
                    Find.WindowStack.Add(new Window_DesignatorPalette<DesignatorThingSelection>(
                        this, Bespawnable.Select(def => new DesignatorThingSelection(def)), RecentSelections,
                        item => item.Stuff == null
                            ? item.Thing.label ?? item.Thing.defName
                            : item.Stuff.LabelAsStuff + " " + (item.Thing.label ?? item.Thing.defName),
                        (item, rect) => Widgets.DefIcon(rect, item.Thing, item.Stuff, drawPlaceholder: true),
                        item =>
                        {
                            if (item.Stuff == null) this.SelectThing(item.Thing);
                            else this.SelectThing(item.Thing, item.Stuff);
                        },
                        item => item.Thing == thing && (item.Stuff == null || item.Stuff == stuff),
                        item => (item.Stuff == null ? "" : item.Stuff.LabelAsStuff + " ")
                            + (item.Thing.label ?? item.Thing.defName)
                            + (item.Thing.description.NullOrEmpty() ? "" : "\n\n" + item.Thing.description),
                        GetCategoryFilters().ToDictionary(filter => filter.Key,
                            filter => new Func<DesignatorThingSelection, bool>(item => filter.Value(item.Thing)))));
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
        public void SelectThing(ThingDef def, ThingDef stuffDef)
        {
            Designator_SpawnThing.thing = def;
            Designator_SpawnThing.stuff = stuffDef;
            this.defaultLabel = def.label;
            this.defaultDesc = def.description;
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
        public override void RenderHighlight(List<IntVec3> dragCells)
        {
            DesignatorUtility.RenderHighlightOverSelectableCells(this, dragCells);
        }
        protected override void DrawGhost(Color ghostCol)
        {
            if (!(this.PlacingDef.graphic is Graphic_Cluster) && (!((ThingDef)this.PlacingDef).graphicData.Linked || this.PlacingDef.uiIconPath != null) && ((ThingDef)this.PlacingDef).graphicData.onGroundRandomRotateAngle <  0.01f)
            {
                base.DrawGhost(ghostCol);
            }
        }
        protected override void PlaceAt(IntVec3 loc, Rot4 rotation)
        {
            if (loc.InBounds(Find.CurrentMap))
            {
                ThingDef def = Designator_SpawnThing.thing;
                if (loc.GetThingList(Find.CurrentMap).Find(target => target.def == def && target.Stuff == (def.MadeFromStuff ? this.StuffDef : null)) is Thing thing && thing.stackCount < thing.def.stackLimit)
                {
                    thing.stackCount++;
                    return;
                }
                Thing newThing = GenSpawn.Spawn(ThingMaker.MakeThing(def,def.MadeFromStuff ? this.StuffDef : null), loc,Find.CurrentMap,rotation,WipeMode.VanishOrMoveAside);
            }
        }
        public override AcceptanceReport CanDesignateCell(IntVec3 loc)
        {
            return true;
        }

        private static Dictionary<string, Func<ThingDef, bool>> GetCategoryFilters()
        {
            List<ThingDef> things = Bespawnable;
            Dictionary<string, Func<ThingDef, bool>> filters = new Dictionary<string, Func<ThingDef, bool>>
            {
                { "CQF_SpawnCategory_Buildings".Translate(), def => def.category == ThingCategory.Building },
                { "CQF_SpawnCategory_Items".Translate(), def => def.category == ThingCategory.Item },
                { "CQF_SpawnCategory_Plants".Translate(), def => def.category == ThingCategory.Plant },
                { "CQF_SpawnCategory_Filth".Translate(), def => def.category == ThingCategory.Filth },
                { "CQF_SpawnCategory_Other".Translate(), def => def.category != ThingCategory.Building
                    && def.category != ThingCategory.Item && def.category != ThingCategory.Plant && def.category != ThingCategory.Filth }
            };
            filters = filters.Where(filter => things.Any(filter.Value)).ToDictionary(filter => filter.Key, filter => filter.Value);
            foreach (DesignationCategoryDef category in things.Where(def => def.designationCategory != null)
                .Select(def => def.designationCategory).Distinct().OrderBy(category => category.label))
            {
                string label = "CQF_SpawnCategory_BuildMenu".Translate(category.LabelCap);
                if (filters.ContainsKey(label)) label += " (" + category.defName + ")";
                filters.Add(label, def => def.designationCategory == category);
            }
            HashSet<ThingCategoryDef> categories = new HashSet<ThingCategoryDef>();
            foreach (ThingDef def in things)
            {
                if (def.thingCategories == null) continue;
                foreach (ThingCategoryDef leaf in def.thingCategories)
                {
                    for (ThingCategoryDef category = leaf; category?.parent != null; category = category.parent)
                    {
                        categories.Add(category);
                    }
                }
            }
            Dictionary<string, Func<ThingDef, bool>> itemFilters = new Dictionary<string, Func<ThingDef, bool>>();
            foreach (ThingCategoryDef category in categories.OrderBy(category => category.defName))
            {
                List<string> path = new List<string>();
                for (ThingCategoryDef parent = category; parent?.parent != null; parent = parent.parent)
                {
                    path.Add(parent.LabelCap);
                }
                path.Reverse();
                string label = "CQF_SpawnCategory_ThingCategory".Translate(string.Join(" / ", path));
                if (itemFilters.ContainsKey(label)) label += " (" + category.defName + ")";
                itemFilters.Add(label, def => def.IsWithinCategory(category));
            }
            foreach (KeyValuePair<string, Func<ThingDef, bool>> filter in itemFilters.OrderBy(filter => filter.Key))
            {
                filters.Add(filter.Key, filter.Value);
            }
            return filters;
        }

        private void RecordRecentSelection(ThingDef def, ThingDef stuffDef)
        {
            recentSelections.RemoveAll(selection => selection.Thing == def && selection.Stuff == stuffDef);
            recentSelections.Insert(0, new DesignatorThingSelection(def, stuffDef));
            if (recentSelections.Count > RecentSelectionLimit)
            {
                recentSelections.RemoveRange(RecentSelectionLimit, recentSelections.Count - RecentSelectionLimit);
            }
        }

        public static ThingDef stuff = ThingDefOf.WoodLog;
        public static ThingDef thing = ThingDefOf.WoodLog;
        public static ThingStyleDef style = null;
        public static List<ThingDef> bespawnable = new List<ThingDef>();

        private const int RecentSelectionLimit = 5;

        private static readonly List<DesignatorThingSelection> recentSelections = new List<DesignatorThingSelection>();
    }
}
