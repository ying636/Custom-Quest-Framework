using System;
using System.Collections.Generic;
using RimWorld;
using Verse;

namespace QuestEditor_Library
{
    public abstract class Designator_TransformThing : Designator
    {
        protected Designator_TransformThing()
        {
            this.useMouseIcon = true;
        }

        public override bool Visible => Prefs.DevMode;

        public override AcceptanceReport CanDesignateCell(IntVec3 loc)
        {
            if (!Prefs.DevMode || this.Map == null || !loc.InBounds(this.Map))
            {
                return false;
            }
            return loc.GetThingList(this.Map).Exists(thing => this.CanDesignateThing(thing).Accepted)
                ? AcceptanceReport.WasAccepted
                : "CQF_TransformThingNoTarget".Translate();
        }

        public override AcceptanceReport CanDesignateThing(Thing thing)
        {
            return Prefs.DevMode && thing.Spawned && !thing.Destroyed && thing.Map == this.Map && thing.def.selectable;
        }

        public override void SelectedUpdate()
        {
            GenUI.RenderMouseoverBracket();
        }

        protected void ChooseThing(IntVec3 loc, Action<Thing> action)
        {
            AcceptanceReport report = this.CanDesignateCell(loc);
            if (!report.Accepted)
            {
                if (!report.Reason.NullOrEmpty())
                {
                    Messages.Message(report.Reason, MessageTypeDefOf.RejectInput, false);
                }
                return;
            }
            List<Thing> things = loc.GetThingList(this.Map).FindAll(thing => this.CanDesignateThing(thing).Accepted);
            if (things.Count == 1)
            {
                action(things[0]);
                return;
            }
            List<FloatMenuOption> options = new List<FloatMenuOption>();
            foreach (Thing thing in things)
            {
                options.Add(new FloatMenuOption(thing.LabelCap, () => action(thing)));
            }
            Find.WindowStack.Add(new FloatMenu(options));
        }

        protected AcceptanceReport GetPlacementReport(Thing thing, IntVec3 position, Rot4 rotation)
        {
            Map map = this.Map;
            if (!Prefs.DevMode || map == null || !thing.Spawned || thing.Destroyed || thing.Map != map)
            {
                return "CQF_EditorTargetUnavailable".Translate();
            }
            if (thing.debugRotLocked && rotation != thing.Rotation)
            {
                return "CQF_TransformThingRotationLocked".Translate();
            }
            CellRect rect = GenAdj.OccupiedRect(position, rotation, thing.def.size);
            if (!rect.InBounds(map))
            {
                return "OutOfBounds".Translate();
            }
            foreach (IntVec3 cell in rect)
            {
                foreach (Thing other in cell.GetThingList(map))
                {
                    if (other == thing)
                    {
                        continue;
                    }
                    if (GenSpawn.SpawningWipes(thing.def, other.def) || GenSpawn.SpawningWipes(other.def, thing.def)
                        || (thing.def.passability == Traversability.Impassable && other is Pawn)
                        || (other.def.passability == Traversability.Impassable
                            && (thing is Pawn || (thing.def.category == ThingCategory.Item && other.def.surfaceType == SurfaceType.None))))
                    {
                        return "SpaceAlreadyOccupied".Translate();
                    }
                }
                if (thing.def.category == ThingCategory.Item
                    && cell.GetItemCount(map) - (thing.OccupiedRect().Contains(cell) ? 1 : 0) >= cell.GetMaxItemsAllowedInCell(map))
                {
                    return "SpaceAlreadyOccupied".Translate();
                }
            }
            return true;
        }

        protected bool TransformThing(Thing thing, IntVec3 position, Rot4 rotation)
        {
            AcceptanceReport report = this.GetPlacementReport(thing, position, rotation);
            if (!report.Accepted)
            {
                Messages.Message(report.Reason, MessageTypeDefOf.RejectInput, false);
                return false;
            }
            if (thing.Position == position && thing.Rotation == rotation)
            {
                return true;
            }
            if (thing is Pawn pawn)
            {
                bool moved = thing.Position != position;
                thing.Position = position;
                thing.Rotation = rotation;
                if (moved)
                {
                    pawn.Notify_Teleported();
                }
                return true;
            }
            Map map = thing.Map;
            IntVec3 oldPosition = thing.Position;
            Rot4 oldRotation = thing.Rotation;
            CustomMapEntrance? entrance = thing as CustomMapEntrance;
            bool preserveMap = entrance?.preserveMapOnDespawn ?? false;
            if (entrance != null)
            {
                entrance.preserveMapOnDespawn = true;
            }
            try
            {
                thing.DeSpawn(DestroyMode.WillReplace);
                thing.Position = position;
                thing.Rotation = rotation;
                thing.SpawnSetup(map, true);
                if (!thing.Spawned || thing.Destroyed)
                {
                    throw new InvalidOperationException("CQF transform did not respawn " + thing.ThingID);
                }
                return true;
            }
            catch (Exception error)
            {
                Log.Error("CQF transform failed for " + thing.ThingID + ": " + error);
                if (!thing.Destroyed)
                {
                    try
                    {
                        if (thing.Spawned)
                        {
                            thing.DeSpawn(DestroyMode.WillReplace);
                        }
                        thing.Position = oldPosition;
                        thing.Rotation = oldRotation;
                        thing.SpawnSetup(map, true);
                        if (!thing.Spawned || thing.Destroyed)
                        {
                            throw new InvalidOperationException("CQF transform did not restore " + thing.ThingID);
                        }
                    }
                    catch (Exception restoreError)
                    {
                        Log.Error("CQF transform restore failed for " + thing.ThingID + ": " + restoreError);
                    }
                }
                Messages.Message("CQF_TransformThingFailed".Translate(), MessageTypeDefOf.RejectInput, false);
                return false;
            }
            finally
            {
                if (entrance != null)
                {
                    entrance.preserveMapOnDespawn = preserveMap;
                }
            }
        }
    }
}
