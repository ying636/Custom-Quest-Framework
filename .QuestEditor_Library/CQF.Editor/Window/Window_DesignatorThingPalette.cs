using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace QuestEditor_Library
{
    public class Window_DesignatorThingPalette : Window_DesignatorPalette<DesignatorThingSelection>
    {
        public Window_DesignatorThingPalette(Designator_SpawnThing designator)
        {
            this.designator = designator;
            if (cachedAllItems == null || cachedAllItems.Count != Designator_SpawnThing.Bespawnable.Count)
            {
                cachedAllItems = Designator_SpawnThing.Bespawnable.Select(def => new DesignatorThingSelection(def)).ToList();
            }
        }

        protected override string PaletteTitle => "CQF_ThingPalette".Translate();

        protected override IReadOnlyList<DesignatorThingSelection> AllItems => cachedAllItems;

        protected override IReadOnlyList<DesignatorThingSelection> RecentItems => Designator_SpawnThing.RecentSelections;

        protected override string GetLabel(DesignatorThingSelection item)
        {
            if (item.Stuff == null)
            {
                return item.Thing.label ?? item.Thing.defName;
            }
            return item.Stuff.LabelAsStuff + " " + (item.Thing.label ?? item.Thing.defName);
        }

        protected override string GetTip(DesignatorThingSelection item)
        {
            string label = this.GetLabel(item);
            return item.Thing.description.NullOrEmpty() ? label : label + "\n\n" + item.Thing.description;
        }

        protected override void DrawIcon(DesignatorThingSelection item, Rect rect)
        {
            Widgets.DefIcon(rect, item.Thing, item.Stuff, drawPlaceholder: true);
        }

        protected override void SelectItem(DesignatorThingSelection item)
        {
            if (item.Stuff == null)
            {
                this.designator.SelectThing(item.Thing);
                return;
            }
            this.designator.SelectThing(item.Thing, item.Stuff);
        }

        protected override bool IsSelected(DesignatorThingSelection item)
        {
            if (item.Thing != Designator_SpawnThing.thing)
            {
                return false;
            }
            return item.Stuff == null || item.Stuff == Designator_SpawnThing.stuff;
        }

        private readonly Designator_SpawnThing designator;
        private static List<DesignatorThingSelection> cachedAllItems;
    }
}
