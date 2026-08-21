using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace QuestEditor_Library
{
    public class Designator_SetQuality : Designator_Cells
    {
        public Designator_SetQuality()
        {
            this.defaultDesc = "CQFSetQualityDesc".Translate();
            this.icon = TexButton.OpenStatsReport;
            this.useMouseIcon = true;
        }

        public override string Label
        {
            get
            {
                switch (this.mode)
                {
                    case QualitySettingMode.Upgrade:
                        return "CQFSetQuality".Translate("CQF_UpgradeQuality".Translate());
                    case QualitySettingMode.Downgrade:
                        return "CQFSetQuality".Translate("CQF_DowngradeQuality".Translate());
                    default:
                        return "CQFSetQuality".Translate(this.quality.GetLabel());
                }
            }
        }

        public override bool Visible => DebugSettings.godMode;

        public override DrawStyleCategoryDef DrawStyleCategory => QEDefOf.CQF_Areas;

        public override bool DragDrawMeasurements => true;

        public override IEnumerable<FloatMenuOption> RightClickFloatMenuOptions
        {
            get
            {
                yield return new FloatMenuOption("CQF_UpgradeQuality".Translate(),
                    () =>
                    {
                        this.mode = QualitySettingMode.Upgrade;
                        Find.DesignatorManager.Select(this);
                    });
                yield return new FloatMenuOption("CQF_DowngradeQuality".Translate(),
                    () =>
                    {
                        this.mode = QualitySettingMode.Downgrade;
                        Find.DesignatorManager.Select(this);
                    });
                foreach (QualityCategory category in QualityUtility.AllQualityCategories)
                {
                    QualityCategory selectedCategory = category;
                    yield return new FloatMenuOption(selectedCategory.GetLabel().CapitalizeFirst(),
                        () =>
                        {
                            this.mode = QualitySettingMode.Fixed;
                            this.quality = selectedCategory;
                            Find.DesignatorManager.Select(this);
                        });
                }
            }
        }

        public override void DesignateSingleCell(IntVec3 loc)
        {
            if (!loc.InBounds(Find.CurrentMap))
            {
                return;
            }
            foreach (Thing thing in loc.GetThingList(Find.CurrentMap))
            {
                if (thing.Destroyed || !thing.TryGetComp<CompQuality>(out CompQuality compQuality))
                {
                    continue;
                }
                QualityCategory targetQuality;
                switch (this.mode)
                {
                    case QualitySettingMode.Upgrade:
                        targetQuality = (QualityCategory)Mathf.Min((int)compQuality.Quality + 1,
                            (int)QualityCategory.Legendary);
                        break;
                    case QualitySettingMode.Downgrade:
                        targetQuality = (QualityCategory)Mathf.Max((int)compQuality.Quality - 1,
                            (int)QualityCategory.Awful);
                        break;
                    default:
                        targetQuality = this.quality;
                        break;
                }
                compQuality.SetQuality(targetQuality, null);
                Find.CurrentMap.mapDrawer.MapMeshDirty(thing.Position, MapMeshFlagDefOf.Things);
            }
        }

        public override AcceptanceReport CanDesignateCell(IntVec3 loc)
        {
            if (!loc.InBounds(Find.CurrentMap))
            {
                return false;
            }
            return loc.GetThingList(Find.CurrentMap).Exists(thing =>
                !thing.Destroyed && thing.TryGetComp<CompQuality>() != null)
                ? AcceptanceReport.WasAccepted
                : "CQF_SetQualityNoTarget".Translate();
        }

        private QualityCategory quality = QualityCategory.Normal;
        private QualitySettingMode mode;
    }

    internal enum QualitySettingMode
    {
        Fixed,
        Upgrade,
        Downgrade
    }
}
