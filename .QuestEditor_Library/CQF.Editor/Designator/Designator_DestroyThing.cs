using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace QuestEditor_Library
{
    public class Designator_DestroyThing : Designator_CQFCells
    {
        public Designator_DestroyThing() 
        {
            this.defaultLabel = "DesignatorDestroyThing".Translate();
            this.icon = CQFEditorTools.icon_DestroyThing;
            this.defaultDesc = "DesignatorDestroyThingDesc".Translate();
            this.useMouseIcon = true;
            this.tutorTag = "DestroyThing";
        }
        public override DrawStyleCategoryDef DrawStyleCategory
        {
            get
            {
                return QEDefOf.CQF_Areas;
            }
        }
        public override bool DragDrawMeasurements => true;
        public override bool Visible => DebugSettings.godMode;
        public override IEnumerable<FloatMenuOption> RightClickFloatMenuOptions
        {
            get
            {
                yield return new FloatMenuOption("DestroyAllPawn".Translate(), () => Find.CurrentMap.mapPawns.AllPawnsSpawned.ToList().ListFullCopy().ForEach(p => 
                {
                    if (p.Spawned) 
                    {
                        p.Destroy();
                    }
                }));
                yield return new FloatMenuOption("DestroyAllFilth".Translate(), () => Find.CurrentMap.listerThings.ThingsInGroup(ThingRequestGroup.Filth).ListFullCopy().ForEach(p =>
                {
                    if (p.Spawned)
                    {
                        p.Destroy();
                    }
                }));
                yield break;
            }
        }
        protected override void ApplyCell(IntVec3 loc)
        {
            if (loc.InBounds(Find.CurrentMap))
            {
                foreach (Thing thing in loc.GetThingList(Find.CurrentMap).ToList())
                {
                    if (thing.def.destroyable && !thing.Destroyed && this.Visit(thing))
                    {
                        thing.Destroy();
                    }
                }
            }
        }
        public override void SelectedUpdate()
        {
            GenUI.RenderMouseoverBracket();
        }
        public override AcceptanceReport CanDesignateCell(IntVec3 loc)
        {
            return true;
        }

    }
}
