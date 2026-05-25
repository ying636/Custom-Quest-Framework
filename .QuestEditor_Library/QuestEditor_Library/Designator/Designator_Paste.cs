using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace QuestEditor_Library
{
    public class Designator_Paste : Designator_Cells
    {
        public Designator_Paste()
        {
            this.defaultLabel = "CQFPaste".Translate();
            this.icon = TexButton.Paste;
            this.defaultDesc = "CQFPasteDesc".Translate();
            this.useMouseIcon = true;
        }
        public override string Label
        {
            get
            {
                string text = this.pasteActionComp ? "ActionComp".Translate() : this.type.Name.Translate();
                return base.Label + $"({text})";
            }
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
        public override IEnumerable<FloatMenuOption> RightClickFloatMenuOptions
        {
            get
            {
                foreach (Type t in new List<Type>() { typeof(LootBox),
                    typeof(InteractableThing), typeof(CustomTrap), typeof(ZoneCore),typeof(GenerationActionWorker) })
                {
                    yield return new FloatMenuOption(t.Name.Translate(), () => 
                    {
                        this.type = t;
                        this.pasteActionComp = false;
                    });
                }
                yield return new FloatMenuOption("ActionComp".Translate(), () => this.pasteActionComp = true);
                yield break;
            }
        }
        public override void DesignateSingleCell(IntVec3 loc)
        {
            if (loc.InBounds(Find.CurrentMap))
            {
                if (!this.pasteActionComp)
                {
                    if (loc.GetThingList(Find.CurrentMap).Find(t =>this.type.IsAssignableFrom(t.GetType()))
                        is IPastableData box)
                    {
                        box.PasteData();
                    }
                }
                else 
                {
                    if (loc.GetThingList(Find.CurrentMap).Find(t => t.TryGetComp<CompActionWorker>() != null)?.TryGetComp<CompActionWorker>() is CompActionWorker comp)
                    {
                        comp.PasteSingleComp();
                    }
                }
            }
        }
        public override AcceptanceReport CanDesignateCell(IntVec3 loc)
        {
            return loc.InBounds(Find.CurrentMap) &&
                   loc.GetThingList(Find.CurrentMap).Exists(t => this.type.IsAssignableFrom(t.GetType())
                                                                 || (this.pasteActionComp && t.TryGetComp<CompActionWorker>()
                                                                     != null));
        }

        public Type type = typeof(LootBox);
        public bool pasteActionComp = false;
    }
}
