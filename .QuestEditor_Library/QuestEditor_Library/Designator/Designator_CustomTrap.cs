using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace QuestEditor_Library
{
    public class Designator_CustomTrap : Designator
    {
        public Designator_CustomTrap()
        {
            this.defaultLabel = "TrapEditor".Translate().Colorize(ColorLibrary.SkyBlue);
            this.icon =  ContentFinder<Texture2D>.Get("UI/Icon_Edit");;
            this.defaultDesc = "TrapEditorDesc".Translate().Colorize(ColorLibrary.SkyBlue);
            this.useMouseIcon = true;
        }
        public override bool Visible => DebugSettings.godMode;
        public override Color IconDrawColor => Designator_CustomTrap.thing == null ? Color.white : Designator_CustomTrap.thing.MadeFromStuff && stuff != null ? Designator_CustomTrap.thing?.GetColorForStuff(stuff) ?? base.IconDrawColor : base.IconDrawColor;
        public List<Type> ToolTypes => new List<Type>() { typeof(CustomTrap)};
        public bool IsCQFTool(ThingDef def)
        {
            return this.ToolTypes.Exists(t => def.thingClass == t || def.thingClass.IsSubclassOf(t));
        }
        public override IEnumerable<FloatMenuOption> RightClickFloatMenuOptions
        {
            get
            {
                if (Designator_CustomTrap.bespawnable.NullOrEmpty())
                {
                    foreach (ThingDef def in DefDatabase<ThingDef>.AllDefsListForReading)
                    {
                        if (this.IsCQFTool(def))
                        {
                            Designator_CustomTrap.bespawnable.Add(def);
                        }
                    }
                }
                yield return new FloatMenuOption("Select".Translate(), () =>
                {
                    Find.WindowStack.Add(new Dialog_Select<ThingDef>(new TextureSelectDrawer<ThingDef>(Designator_CustomTrap.bespawnable, x => x.uiIcon, x => this.IsCQFTool(x) ? x.label.Colorize(ColorLibrary.SkyBlue) : x.label, x =>
           {
               Designator_CustomTrap.thing = x;
               stuff = null;
               string label = x.label;
               if (this.IsCQFTool(x))
               {
                   label = label.Colorize(ColorLibrary.SkyBlue);
               }
               this.defaultLabel = label;
               this.icon = x.GetUIIconForStuff(stuff);
               this.defaultDesc = x.description;
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
                               this.icon = x.GetUIIconForStuff(s);
                           },
                           t => t.graphic?.Color ?? Color.white,
                           (t, r) => Widgets.DefIcon(r, t, null)),
                       "SelectStuff".Translate()));
               }
           }, null, null), "Select".Translate()));
                });
                yield return new FloatMenuOption("TrapEditor".Translate(), () =>
                {
                    Designator_CustomTrap.thing = null;
                    this.defaultLabel = "TrapEditor".Translate().Colorize(ColorLibrary.SkyBlue);
                    this.icon = ContentFinder<Texture2D>.Get("UI/Icon_Edit");
                    this.defaultDesc = "TrapEditorDesc".Translate().Colorize(ColorLibrary.SkyBlue);
                });
               yield break;
            }
        }
        public override void DesignateSingleCell(IntVec3 loc)
        {
            if (loc.InBounds(Find.CurrentMap))
            {
                if (Designator_CustomTrap.thing is ThingDef def)
                {
                    GenSpawn.Spawn(ThingMaker.MakeThing(def,stuff),loc,Find.CurrentMap);
                }
                else if (loc.GetFirstThing<CustomTrap>(Find.CurrentMap) is CustomTrap trap)
                {
                    Find.WindowStack.Add(new Dialog_EditCustomTrap(trap));
                }
            }
        }
        public override AcceptanceReport CanDesignateCell(IntVec3 loc)
        {
            return true;
        }

        public static ThingDef stuff = null;
        public static ThingDef thing = null;
        public static List<ThingDef> bespawnable = new List<ThingDef>();
    }
}
