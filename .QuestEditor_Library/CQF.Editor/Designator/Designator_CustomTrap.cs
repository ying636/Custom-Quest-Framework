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
            this.defaultLabel = "TrapEditor".Translate().Colorize(CQFUIStyle.Accent);
            this.icon =  ContentFinder<Texture2D>.Get("UI/Icon_Edit");;
            this.defaultDesc = "TrapEditorDesc".Translate().Colorize(CQFUIStyle.Accent);
            this.useMouseIcon = true;
        }
        public override bool Visible => DebugSettings.godMode;
        public override Color IconDrawColor => Designator_CustomTrap.thing == null ? Color.white : Designator_CustomTrap.thing.MadeFromStuff && stuff != null ? Designator_CustomTrap.thing?.GetColorForStuff(stuff) ?? base.IconDrawColor : base.IconDrawColor;
        public List<Type> ToolTypes => new List<Type>() { typeof(CustomTrap)};
#if RIMWORLD_1_7
        private bool DirectionalMechanism => thing?.HasComp(typeof(Comp_CQFTrapMechanism)) == true;
#endif

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
                    Find.WindowStack.Add(new Dialog_Select<ThingDef>(new TextureSelectDrawer<ThingDef>(Designator_CustomTrap.bespawnable, x => x.uiIcon, x => this.IsCQFTool(x) ? x.label.Colorize(CQFUIStyle.Accent) : x.label, x =>
           {
               Designator_CustomTrap.thing = x;
               stuff = null;
               string label = x.label;
               if (this.IsCQFTool(x))
               {
                   label = label.Colorize(CQFUIStyle.Accent);
               }
               this.defaultLabel = label;
               this.icon = x.GetUIIconForStuff(stuff);
               this.defaultDesc = x.description;
#if RIMWORLD_1_7
               this.rotation = x.defaultPlacingRot;
               if (this.DirectionalMechanism)
                   this.defaultDesc += "\n" + "CQF_TrapDirectionTip".Translate();
#endif
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
                               this.defaultLabel = s.LabelAsStuff.Colorize(CQFUIStyle.Accent) + this.defaultLabel;
                               this.icon = x.GetUIIconForStuff(s);
                           },
                           t => t.graphic?.Color ?? Color.white),
                       "SelectStuff".Translate()));
               }
           }, null, null), "Select".Translate()));
                });
                yield return new FloatMenuOption("TrapEditor".Translate(), () =>
                {
                    Designator_CustomTrap.thing = null;
                    this.defaultLabel = "TrapEditor".Translate().Colorize(CQFUIStyle.Accent);
                    this.icon = ContentFinder<Texture2D>.Get("UI/Icon_Edit");
                    this.defaultDesc = "TrapEditorDesc".Translate().Colorize(CQFUIStyle.Accent);
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
                    Rot4 facing = Rot4.North;
#if RIMWORLD_1_7
                    if (this.DirectionalMechanism)
                        facing = this.rotation;
#endif
                    CQFEditorSymmetry symmetry = CQFEditorSymmetry.For(this.Map);
                    if (symmetry.Active)
                        symmetry.Place(loc, facing, def, (position, rotation) => GenSpawn.Spawn(ThingMaker.MakeThing(def, stuff), position, this.Map, rotation), stuff);
                    else
                        GenSpawn.Spawn(ThingMaker.MakeThing(def, stuff), loc, this.Map, facing);
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

#if RIMWORLD_1_7
        public override void SelectedProcessInput(Event ev)
        {
            if (this.DirectionalMechanism && ev.type == EventType.KeyDown)
            {
                if (KeyBindingDefOf.Designator_RotateLeft.KeyDownEvent || ev.keyCode == KeyCode.Q)
                {
                    this.rotation = this.rotation.Rotated(RotationDirection.Counterclockwise);
                    ev.Use();
                    return;
                }
                if (KeyBindingDefOf.Designator_RotateRight.KeyDownEvent || ev.keyCode == KeyCode.E)
                {
                    this.rotation = this.rotation.Rotated(RotationDirection.Clockwise);
                    ev.Use();
                    return;
                }
            }
            base.SelectedProcessInput(ev);
        }

        public override void SelectedUpdate()
        {
            base.SelectedUpdate();
            if (!this.DirectionalMechanism)
                return;
            IntVec3 cell = UI.MouseCell();
            if (!cell.InBounds(this.Map))
                return;

            GhostDrawer.DrawGhostThing(cell, this.rotation, thing, null, Color.white, AltitudeLayer.Blueprint, null, true, stuff);
            Vector3 start = cell.ToVector3Shifted();
            start.y = AltitudeLayer.Blueprint.AltitudeFor();
            Vector3 step = this.rotation.FacingCell.ToVector3();
            Vector3 end = start + step * 1.15f;
            Vector3 side = new Vector3(step.z, 0f, -step.x) * 0.19f;
            GenDraw.DrawLineBetween(start, end, SimpleColor.White, 0.07f);
            GenDraw.DrawLineBetween(end, end - step * 0.28f + side, SimpleColor.White, 0.07f);
            GenDraw.DrawLineBetween(end, end - step * 0.28f - side, SimpleColor.White, 0.07f);
        }
#endif

        public static ThingDef stuff = null;
        public static ThingDef thing = null;
        public static List<ThingDef> bespawnable = new List<ThingDef>();
#if RIMWORLD_1_7
        private Rot4 rotation = Rot4.North;
#endif
    }
}
