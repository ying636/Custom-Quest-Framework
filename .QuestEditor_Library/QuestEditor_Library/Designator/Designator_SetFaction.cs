using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace QuestEditor_Library
{
    public class Designator_SetFaction : Designator_Cells
    {
        public Designator_SetFaction()
        {
            this.defaultDesc = "CQFSetFactionDesc".Translate();
            this.useMouseIcon = true;
        }
        public Faction Faction 
        {
            get 
            {
                return faction;
            }
        }
        public override string Label => "CQFSetFaction".Translate(this.Faction?.GetCallLabel() ?? "Null".Translate());
        public override void DrawIcon(Rect rect, Material buttonMat, GizmoRenderParms parms)
        {
            if (this.icon == null) 
            {
                this.icon = this.Faction?.def.FactionIcon;
            }
            base.DrawIcon(rect, buttonMat, parms);
        }
        public override bool Visible => DebugSettings.godMode;
        public override DrawStyleCategoryDef DrawStyleCategory
        {
            get
            {
                return DrawStyleCategoryDefOf.Areas;
            }
        }
        public override bool DragDrawMeasurements => true;
        public override Color IconDrawColor => this.Faction?.Color ?? base.IconDrawColor;
        public override IEnumerable<FloatMenuOption> RightClickFloatMenuOptions
        {
            get
            {
                foreach(Faction faction in Find.FactionManager.AllFactionsListForReading)
                {
                    yield return new FloatMenuOption(faction.GetCallLabel() + $"({faction.def.label})", () => 
                    {
                        Designator_SetFaction.faction = faction;
                        this.icon = faction.def.FactionIcon;
                    });
                }
                yield return new FloatMenuOption("Null".Translate(), () =>
                {
                    Designator_SetFaction.faction = null;
                });
                yield break;
            }
        }
        public override void DesignateSingleCell(IntVec3 loc)
        {
            if (loc.InBounds(Find.CurrentMap))
            {
                loc.GetThingList(Find.CurrentMap).ForEach(t => 
                {
                    if (t.def.CanHaveFaction)
                    {
                        t.SetFaction(this.Faction);
                    }
                });
            }
        }
        public override AcceptanceReport CanDesignateCell(IntVec3 loc)
        {
            return true;
        }

        public static Faction faction = null;
    }
}
