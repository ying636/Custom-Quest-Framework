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
    public class Designator_MoveIn : Designator_Cells
    {
        public Designator_MoveIn()
        {
            this.defaultLabel = "QE_Designator_MoveIn_NoTarget".Translate();
            this.icon = ContentFinder<Texture2D>.Get("UI/Icon_MoveIn", true);
            this.defaultDesc = "QE_Designator_MoveInDesc".Translate();
            this.useMouseIcon = true;
        }
        public override bool Visible => true;
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
                if (Find.CurrentMap is Map map)
                {
#if RIMWORLD_1_7
                    if (GameComponent_MapManager.Instance is { } manager)
                    {
                        IReadOnlyList<Map> maps = manager.GetReachableCQFMaps(map);
                        if (manager.GetCompositeMap(map) != null || maps.Any(candidate => manager.GetCompositeMap(candidate) != null))
                        {
                            foreach (Map destination in maps)
                            {
                                string label = destination.Parent is MapParent_Custom custom ? custom.MapName : destination.Parent.LabelCap;
                                yield return new FloatMenuOption(label, () =>
                                {
                                    crossMapSource = map;
                                    crossMapTarget = destination;
                                    this.defaultLabel = "QE_Designator_MoveIn".Translate(label);
                                });
                            }
                            yield break;
                        }
                    }
#endif
                    if (map.GetComponent<MapComponent_CustomMapData>() is MapComponent_CustomMapData data)
                    {
                        if (data.subMaps != null && data.subMaps.Any()) 
                        {
                            foreach (MapParent_Custom parent in data.subMaps)
                            {
                                if (parent.entrance != null) 
                                {
                                    yield return new FloatMenuOption(parent.MapName, () =>
                                    {
#if RIMWORLD_1_7
                                        crossMapSource = crossMapTarget = null;
#endif
                                        Designator_MoveIn.entrance = parent.entrance;
                                        Designator_MoveIn.target = parent;
                                        this.defaultLabel = "QE_Designator_MoveIn".Translate(parent.MapName);
                                    });
                                }
                            } 
                        }
                    }
                    else 
                    {
                        map.components.Add(new MapComponent_CustomMapData(map));
                    }
                }
                yield break;
            }
        }
        public override void DesignateThing(Thing t)
        {
            this.DesignateSingleCell(t.Position);
        }
        public override AcceptanceReport CanDesignateThing(Thing t)
        {
            if (!t.def.alwaysHaulable)
            {
                return false;
            }
            if (base.Map.designationManager.DesignationAt(t.Position, this.Designation) != null)
            {
                return AcceptanceReport.WasRejected;
            }
            return true;
        }
        public override void DesignateSingleCell(IntVec3 loc)
        {
            if (!loc.InBounds(this.Map))
            {
                return;
            }
            Pawn targetPawn = loc.GetFirstPawn(this.Map);
            Thing target = targetPawn != null && (targetPawn.Faction == Faction.OfPlayer || targetPawn.IsPrisoner || targetPawn.Downed) ? targetPawn : loc.GetFirstItem(this.Map);
            if (target != null && !this.Map.designationManager.HasMapDesignationOn(target)) 
            {
#if RIMWORLD_1_7
                if (crossMapSource == target.Map && crossMapTarget is { } destination &&
                    GameComponent_MapManager.Instance is { } manager)
                {
                    if (manager.AssignCrossMapHaul(target.Map, target, destination))
                        this.Map.designationManager.AddDesignation(new Designation(target, QEDefOf.QE_MoveIn));
                    else
                        Messages.Message("NoPath".Translate(), MessageTypeDefOf.RejectInput);
                    return;
                }
#endif
                if (Designator_MoveIn.target != null)
                {
                    if (target.Map == Designator_MoveIn.entrance.Map)
                    {
                        this.Map.designationManager.AddDesignation(new Designation(target, QEDefOf.QE_MoveIn));
                        target.Map.GetComponent<MapComponent_CustomMapData>().designatedAndMap.SetOrAdd<Thing, Thing>(target, Designator_MoveIn.target.entrance);
                    }
                    else
                    {
                        Messages.Message("ThereIsntTargetSubMap".Translate(Designator_MoveIn.target.MapName), MessageTypeDefOf.NegativeEvent);
                    }
                }
                else
                {
                    Messages.Message("NoCustomMapSelected".Translate(), MessageTypeDefOf.NegativeEvent);
                }
            } 
        }
        public override AcceptanceReport CanDesignateCell(IntVec3 loc)
        {
            return true;
        }

        public static CustomMapEntrance entrance = null;
        public static MapParent_Custom target = null;
#if RIMWORLD_1_7
        private Map? crossMapSource;
        private Map? crossMapTarget;
#endif
    }
}
