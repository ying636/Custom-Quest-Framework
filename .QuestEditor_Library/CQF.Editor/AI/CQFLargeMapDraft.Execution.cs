using System.Diagnostics;
using System.Xml.Linq;
using Verse;

namespace QuestEditor_Library
{
    public sealed partial class CQFLargeMapDraft
    {
        public void BeginApply(int revision)
        {
            RequireIdle();
            if (revision != Revision || CanUndo) throw new InvalidOperationException("CQF_AI_StaleTarget");
            CheckPlacements();
            if (placementConflicts.Count > 0) throw new InvalidDataException("CQF_AI_LargeDraftPlacementConflict");
            if (BlockedRoad.Count > 0) throw new InvalidDataException("CQF_AI_LargeDraftRoadBlocked");
            Start("CQF_AI_LargeDraftApplying", ApplySteps(), terrain.Count + roofs.Count + placements.Count);
        }

        public void BeginUndo()
        {
            RequireIdle();
            if (!CanUndo) throw new InvalidOperationException("CQF_AI_StaleTarget");
            Start("CQF_AI_LargeDraftUndoing", UndoSteps(), applied.Count);
        }

        public void Advance(double budgetMilliseconds = 4, int itemLimit = 384)
        {
            if (work == null) return;
            if (Phase == "CQF_AI_LargeDraftPreparing") itemLimit *= 8;
            var clock = Stopwatch.StartNew();
            try
            {
                for (int i = 0; i < itemLimit && clock.Elapsed.TotalMilliseconds < budgetMilliseconds; i++)
                    if (!work.MoveNext())
                    {
                        work.Dispose();
                        work = null;
                        return;
                    }
            }
            catch (Exception error)
            {
                work.Dispose();
                work = null;
                Error = error;
                if (Phase == "CQF_AI_LargeDraftApplying" && applied.Count > 0)
                {
                    Phase = "CQF_AI_MapRollingBack";
                    work = Rollback().GetEnumerator();
                }
            }
        }

        private void Start(string phase, IEnumerable<int> steps, int total)
        {
            Error = null;
            Phase = phase;
            Completed = 0;
            Total = total;
            work = steps.GetEnumerator();
        }

        private IEnumerable<int> ApplySteps()
        {
            var plan = new CQFAILiveMapPlan(backend, model, "", false);
            foreach (var entry in before)
            {
                if (!IsValid || Map.terrainGrid.TopTerrainAt(entry.Key) != entry.Value)
                    throw new InvalidOperationException("CQF_AI_StaleTarget");
                yield return 0;
            }
            foreach (var entry in terrain)
            {
                if (!IsValid || Map.terrainGrid.TopTerrainAt(entry.Key) != before[entry.Key])
                    throw new InvalidOperationException("CQF_AI_StaleTarget");
                ApplyEdits(plan.PrepareDraftCell(entry.Key, entry.Value, null));
                Completed++;
                yield return 0;
            }
            foreach (var entry in roofs)
            {
                if (!IsValid) throw new InvalidOperationException("CQF_AI_StaleTarget");
                ApplyEdits(plan.PrepareDraftCell(entry.Key, null, entry.Value));
                Completed++;
                yield return 0;
            }

            if (placements.Count > 0)
            {
                foreach (ThingData data in placements)
                {
                    if (!IsValid) throw new InvalidOperationException("CQF_AI_StaleTarget");
                    var command = PlacementCommand(data);
                    ApplyEdits(plan.PrepareDraftPlacement(command, data));
                    Completed++;
                    yield return 0;
                }
            }
            for (int i = 0; i < applied.Count; i++)
            {
                applied[i].Refresh();
                yield return 0;
            }
        }

        private void ApplyEdits(IReadOnlyList<CQFAILiveMapEdit> edits)
        {
            foreach (CQFAILiveMapEdit edit in edits)
            {
                applied.Add(edit);
                edit.Apply();
            }
        }

        private IEnumerable<int> UndoSteps()
        {
            foreach (CQFAILiveMapEdit edit in applied)
            {
                if (!IsValid || !edit.IsCurrent) throw new InvalidOperationException("CQF_AI_StaleTarget");
                yield return 0;
            }

            foreach (int step in Rollback()) yield return step;
        }

        private IEnumerable<int> Rollback()
        {
            for (int i = applied.Count - 1; i >= 0; i--)
            {
                if (!IsValid) throw new InvalidOperationException("CQF_AI_StaleTarget");
                applied[i].Undo();
                applied.RemoveAt(i);
                if (Phase == "CQF_AI_LargeDraftUndoing") Completed++;
                yield return 0;
            }
        }

        private static XElement PlacementCommand(ThingData data) => new XElement("place",
            new XAttribute("def", data.def.defName), new XAttribute("x", data.position.x),
            new XAttribute("z", data.position.z), new XAttribute("rotation", data.rotation.AsInt),
            new XAttribute("count", data.count), data.stuff == null ? null : new XAttribute("stuff", data.stuff.defName));

        private IEnumerator<int>? work;
        private readonly List<CQFAILiveMapEdit> applied = new List<CQFAILiveMapEdit>();
    }
}
