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

        public override bool Visible => DebugSettings.godMode;

        public override bool CanRemainSelected() => this.Visible;

        public override AcceptanceReport CanDesignateCell(IntVec3 loc)
        {
            if (!DebugSettings.godMode || this.Map == null || !loc.InBounds(this.Map))
            {
                return false;
            }
            return loc.GetThingList(this.Map).Exists(thing => this.CanDesignateThing(thing).Accepted)
                ? AcceptanceReport.WasAccepted
                : "CQF_TransformThingNoTarget".Translate();
        }

        public override AcceptanceReport CanDesignateThing(Thing thing)
        {
            return DebugSettings.godMode && thing.Spawned && !thing.Destroyed && thing.Map == this.Map && thing.def.selectable;
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
            return this.GetGroupPlacementReport(thing, position, rotation, null);
        }

        protected bool TransformThing(Thing thing, IntVec3 position, Rot4 rotation)
        {
            CQFEditorSymmetry symmetry = CQFEditorSymmetry.For(this.Map);
            if (!symmetry.Active) return this.TransformSingleThing(thing, position, rotation);
            using CQFSymmetryScope scope = new CQFSymmetryScope(symmetry);
            List<(Thing thing, IntVec3 position, Rot4 rotation, IntVec3 oldPosition, Rot4 oldRotation)> targets = new();
            foreach (CQFSymmetryTransform transform in symmetry.Transforms())
            {
                Thing? target = transform.Axes == 0 ? thing : symmetry.Counterpart(thing, transform);
                if (target == null) return false;
                IntVec3 targetPosition = transform.Position(position, rotation, thing.def);
                Rot4 targetRotation = transform.Rotation(rotation);
                int existing = targets.FindIndex(value => value.thing == target);
                if (existing >= 0)
                {
                    if (!GenAdj.OccupiedRect(targets[existing].position, targets[existing].rotation, thing.def.size)
                        .Equals(GenAdj.OccupiedRect(targetPosition, targetRotation, thing.def.size)))
                    {
                        symmetry.Reject("CQF_SymmetryAxisMove".Translate());
                        return false;
                    }
                    continue;
                }
                targets.Add((target, targetPosition, targetRotation, target.Position, target.Rotation));
            }
            List<Thing> moving = targets.Select(value => value.thing).ToList();
            for (int index = 0; index < targets.Count; index++)
            {
                var target = targets[index];
                AcceptanceReport report = this.GetGroupPlacementReport(target.thing, target.position, target.rotation, moving);
                if (!report.Accepted)
                {
                    symmetry.Reject(report.Reason);
                    return false;
                }
                CellRect rect = GenAdj.OccupiedRect(target.position, target.rotation, target.thing.def.size);
                if (targets.Take(index).Any(other => rect.Overlaps(GenAdj.OccupiedRect(other.position, other.rotation, other.thing.def.size))))
                {
                    symmetry.Reject("CQF_SymmetryOverlap".Translate());
                    return false;
                }
            }
            for (int index = 0; index < targets.Count; index++)
            {
                var target = targets[index];
                if (this.TransformSingleThing(target.thing, target.position, target.rotation, moving)) continue;
                foreach (var previous in targets.Take(index).Reverse())
                    if (!this.TransformSingleThing(previous.thing, previous.oldPosition, previous.oldRotation, moving))
                        Log.Error("[CQF] Symmetry could not restore " + previous.thing.ThingID);
                return false;
            }
            return true;
        }

        private AcceptanceReport GetGroupPlacementReport(Thing thing, IntVec3 position, Rot4 rotation, ICollection<Thing>? moving)
        {
            Map map = this.Map;
            if (!DebugSettings.godMode || map == null || !thing.Spawned || thing.Destroyed || thing.Map != map)
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
                    if (other == thing || moving?.Contains(other) == true)
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
                    && cell.GetItemCount(map) - (moving == null ? (thing.OccupiedRect().Contains(cell) ? 1 : 0)
                        : moving.Count(other => other.def.category == ThingCategory.Item && other.OccupiedRect().Contains(cell))) >= cell.GetMaxItemsAllowedInCell(map))
                {
                    return "SpaceAlreadyOccupied".Translate();
                }
            }
            return true;
        }

        private bool TransformSingleThing(Thing thing, IntVec3 position, Rot4 rotation, ICollection<Thing>? moving = null)
        {
            AcceptanceReport report = this.GetGroupPlacementReport(thing, position, rotation, moving);
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
