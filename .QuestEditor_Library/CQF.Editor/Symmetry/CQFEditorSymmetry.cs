using System.Runtime.CompilerServices;
using RimWorld;
using UnityEngine;
using Verse;

namespace QuestEditor_Library
{
    public sealed class CQFEditorSymmetry
    {
        private CQFEditorSymmetry(Map map)
        {
            this.settings = map.GetComponent<MapComponent_CQFSymmetrySettings>();
        }

        public Map map => this.settings.map;
        public bool Active => this.settings.Active;
        public int Axes => this.settings.Axes;
        public int AxisX => this.settings.AxisX;
        public int AxisZ => this.settings.AxisZ;
        public bool enabled { get => this.settings.enabled; set => this.settings.enabled = value; }
        public bool mirrorX { get => this.settings.mirrorX; set => this.settings.mirrorX = value; }
        public bool mirrorZ { get => this.settings.mirrorZ; set => this.settings.mirrorZ = value; }
        public bool showAxes { get => this.settings.showAxes; set => this.settings.showAxes = value; }
        public int axisX { get => this.settings.axisX; set => this.settings.axisX = value; }
        public int axisZ { get => this.settings.axisZ; set => this.settings.axisZ = value; }
        public CQFSymmetryAxisDrag AxisDrag => this.axisDrag ??= new CQFSymmetryAxisDrag(this.settings);

        public static CQFEditorSymmetry For(Map map) => sessions.GetValue(map, value => new CQFEditorSymmetry(value));

        public CQFSymmetryTransform Transform(int axes) => new CQFSymmetryTransform(axes, this.AxisX, this.AxisZ);

        public IEnumerable<CQFSymmetryTransform> Transforms()
        {
            int axes = this.Active ? this.Axes : 0;
            for (int mask = 0; mask <= 3; mask++)
                if ((mask & axes) == mask) yield return this.Transform(mask);
        }

        public void BeginOperation()
        {
            if (this.depth == 0) this.FinishInspector();
            this.depth++;
        }

        public void TrackInspector(Thing thing, Type tab)
        {
            this.inspectorFrame = Time.frameCount;
            if (this.inspectorOwner == thing && this.inspectorTab == tab) return;
            this.FinishInspector();
            if (!this.Active) return;
            this.inspectorOwner = thing;
            this.inspectorTab = tab;
            this.inspectorSession = new CQFSymmetryEditSession(thing);
            this.settings.EditorSessionOpen = true;
        }

        public void EndOperation()
        {
            if (--this.depth != 0) return;
            this.cells.Clear();
            this.things.Clear();
            this.footprints.Clear();
            if (this.failure != null)
            {
                Messages.Message("CQF_SymmetryFailed".Translate(this.failure), MessageTypeDefOf.RejectInput, false);
                this.failure = null;
            }
        }

        public void Reject(string reason) => this.failure ??= reason;

        public bool Visit(Thing thing) => this.depth == 0 || this.things.Add(thing);

        public void ApplyCells(IntVec3 source, Action<IntVec3> apply, Func<IntVec3, AcceptanceReport> validate)
        {
            using CQFSymmetryScope scope = new CQFSymmetryScope(this);
            foreach (CQFSymmetryTransform transform in this.Transforms())
            {
                IntVec3 cell = transform.Cell(source);
                if (!this.cells.Add(cell)) continue;
                AcceptanceReport report = cell.InBounds(this.map) ? validate(cell) : "OutOfBounds".Translate();
                if (report.Accepted) apply(cell);
                else this.Reject(report.Reason.NullOrEmpty() ? "CQF_SymmetryNoTarget".Translate().ToString() : report.Reason);
            }
        }

        public void Place(IntVec3 source, Rot4 rotation, ThingDef def, Action<IntVec3, Rot4> apply, ThingDef? stuff = null)
        {
            using CQFSymmetryScope scope = new CQFSymmetryScope(this);
            List<(IntVec3 cell, Rot4 rot, CellRect rect)> targets = new List<(IntVec3, Rot4, CellRect)>();
            foreach (CQFSymmetryTransform transform in this.Transforms())
            {
                IntVec3 cell = transform.Position(source, rotation, def);
                Rot4 rot = transform.Rotation(rotation);
                CellRect rect = GenAdj.OccupiedRect(cell, rot, def.size);
                if (this.footprints.Contains(rect) || targets.Any(target => target.rect.Equals(rect))) continue;
                if (targets.Any(target => target.rect.Overlaps(rect)))
                {
                    this.Reject("CQF_SymmetryOverlap".Translate());
                    return;
                }
                AcceptanceReport report = this.PlacementReport(def, cell, rot, stuff);
                if (!report.Accepted)
                {
                    this.Reject(report.Reason);
                    return;
                }
                targets.Add((cell, rot, rect));
            }
            foreach (var target in targets)
            {
                this.footprints.Add(target.rect);
                apply(target.cell, target.rot);
            }
        }

        public AcceptanceReport PlacementReport(ThingDef def, IntVec3 cell, Rot4 rotation, ThingDef? stuff = null)
        {
            CellRect rect = GenAdj.OccupiedRect(cell, rotation, def.size);
            if (!rect.InBounds(this.map)) return "OutOfBounds".Translate();
            foreach (IntVec3 occupied in rect)
            {
                foreach (Thing other in occupied.GetThingList(this.map))
                {
                    if (other.def == def && other.Stuff == stuff && other.Position == cell && (!def.rotatable || other.Rotation == rotation)
                        && other.def.stackLimit > 1 && other.stackCount < other.def.stackLimit) continue;
                    if (GenSpawn.SpawningWipes(def, other.def) || GenSpawn.SpawningWipes(other.def, def)
                        || def.passability == Traversability.Impassable && other is Pawn
                        || other.def.passability == Traversability.Impassable && def.category == ThingCategory.Item && other.def.surfaceType == SurfaceType.None)
                        return "SpaceAlreadyOccupied".Translate();
                }
                if (def.category == ThingCategory.Item && occupied.GetItemCount(this.map) >= occupied.GetMaxItemsAllowedInCell(this.map)
                    && !occupied.GetThingList(this.map).Any(thing => thing.def == def && thing.Stuff == stuff && thing.stackCount < def.stackLimit))
                    return "SpaceAlreadyOccupied".Translate();
            }
            return true;
        }

        public Thing? Counterpart(Thing source, CQFSymmetryTransform transform, bool reportMissing = true)
        {
            IntVec3 cell = transform.Position(source.Position, source.Rotation, source.def);
            Rot4 rotation = transform.Rotation(source.Rotation);
            CellRect rect = GenAdj.OccupiedRect(cell, rotation, source.def.size);
            if (rect.Equals(source.OccupiedRect())) return source;
            if (!rect.InBounds(this.map))
            {
                this.Reject("OutOfBounds".Translate());
                return null;
            }
            List<Thing> matches = cell.GetThingList(this.map).Where(thing => thing.Spawned && !thing.Destroyed
                && thing.Position == cell && thing.def == source.def && thing.Stuff == source.Stuff
                && (!thing.def.rotatable || thing.Rotation == rotation)).ToList();
            if (matches.Count == 1) return matches[0];
            if (reportMissing) this.Reject((matches.Count == 0 ? "CQF_SymmetryNoTarget" : "CQF_SymmetryAmbiguous").Translate());
            return null;
        }

        public void Update()
        {
            if (this.inspectorSession != null && Time.frameCount - this.inspectorFrame > 2) this.FinishInspector();
            if (!this.Active || !this.showAxes || Find.CurrentMap != this.map) return;
            float height = AltitudeLayer.MetaOverlays.AltitudeFor();
            if (this.mirrorX)
            {
                float x = (this.AxisX + 1) * 0.5f;
                GenDraw.DrawLineBetween(new Vector3(x, height, 0f), new Vector3(x, height, this.map.Size.z), SimpleColor.Cyan, 0.12f);
            }
            if (this.mirrorZ)
            {
                float z = (this.AxisZ + 1) * 0.5f;
                GenDraw.DrawLineBetween(new Vector3(0f, height, z), new Vector3(this.map.Size.x, height, z), SimpleColor.Cyan, 0.12f);
            }
        }

        public void ExposeData() => this.settings.ExposeData();

        private void FinishInspector()
        {
            CQFSymmetryEditSession? session = this.inspectorSession;
            this.inspectorSession = null;
            this.settings.EditorSessionOpen = false;
            this.inspectorOwner = null;
            this.inspectorTab = null;
            session?.Complete();
        }

        private readonly MapComponent_CQFSymmetrySettings settings;
        private static readonly ConditionalWeakTable<Map, CQFEditorSymmetry> sessions = new ConditionalWeakTable<Map, CQFEditorSymmetry>();
        private int depth;
        private CQFSymmetryAxisDrag? axisDrag;
        private string? failure;
        private CQFSymmetryEditSession? inspectorSession;
        private Thing? inspectorOwner;
        private Type? inspectorTab;
        private int inspectorFrame;
        private readonly HashSet<IntVec3> cells = new HashSet<IntVec3>();
        private readonly HashSet<Thing> things = new HashSet<Thing>();
        private readonly HashSet<CellRect> footprints = new HashSet<CellRect>();
    }
}
