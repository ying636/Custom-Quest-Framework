using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace QuestEditor_Library
{
    public class Designator_SpawnThing : Designator_Place
    {
        public Designator_SpawnThing()
        {
            this.defaultLabel = Designator_SpawnThing.thing.label;
            this.icon = Designator_SpawnThing.thing.GetUIIconForStuff(this.StuffDef);
            this.defaultDesc = Designator_SpawnThing.thing.description;
            this.useMouseIcon = true;
        }
        public override bool Visible => DebugSettings.godMode;
        public override DrawStyleCategoryDef DrawStyleCategory
        {
            get
            {
                return QEDefOf.CQF_Areas;
            }
        }
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
                if (Designator_SpawnThing.bespawnable.NullOrEmpty())
                {
                    List<ThingCategory> disable = new List<ThingCategory>() { ThingCategory.Gas, ThingCategory.Mote, ThingCategory.Projectile, ThingCategory.Pawn, ThingCategory.Attachment, ThingCategory.Ethereal };
                    foreach (ThingDef def in DefDatabase<ThingDef>.AllDefsListForReading)
                    {
                        if (!disable.Contains(def.category) && !def.IsCorpse && !Designator_CQFTools.IsCQFTool(def))
                        {
                            Designator_SpawnThing.bespawnable.Add(def);
                        }
                    }
                }
                yield return new FloatMenuOption("Select".Translate(), () =>
                 {
                     Find.WindowStack.Add(new Dialog_Select<ThingDef>(new TextureSelectDrawer<ThingDef>(Designator_SpawnThing.bespawnable, x => x.uiIcon, x => x.label, x =>
            {
                this.SelectThing(x);
            }, t => t.graphic?.Color ?? Color.white, (t, r) => Widgets.DefIcon(r, t, null)), "Select".Translate()));
                 });
                yield return new FloatMenuOption("CQF_OpenFloatingPalette".Translate(), () =>
                {
                    Find.WindowStack.Add(new Window_DesignatorThingPalette(this));
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
                this.defaultLabel = stuffDef.LabelAsStuff.Colorize(ColorLibrary.SkyBlue) + this.defaultLabel;
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
        protected override void DrawGhost(Color ghostCol)
        {
            if (!(this.PlacingDef.graphic is Graphic_Cluster) && (!((ThingDef)this.PlacingDef).graphicData.Linked || this.PlacingDef.uiIconPath != null) && ((ThingDef)this.PlacingDef).graphicData.onGroundRandomRotateAngle <  0.01f)
            {
                base.DrawGhost(ghostCol);
            } 
        }
        public override void DesignateSingleCell(IntVec3 loc)
        {
            if (loc.InBounds(Find.CurrentMap))
            {
                ThingDef def = Designator_SpawnThing.thing;
                if (loc.GetFirstThing(Find.CurrentMap, def) is Thing thing && thing.stackCount < thing.def.stackLimit) 
                {
                    thing.stackCount++;
                    return;
                }
                Thing newThing = GenSpawn.Spawn(ThingMaker.MakeThing(def,def.MadeFromStuff ? this.StuffDef : null), loc,Find.CurrentMap,this.placingRot,WipeMode.VanishOrMoveAside);
            }
        }
        public override AcceptanceReport CanDesignateCell(IntVec3 loc)
        {
            return true;
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
