using System.Diagnostics;
using UnityEngine;
using Verse;

namespace QuestEditor_Library
{
    public sealed class CQFAIMapFeedback
    {
        public void Add(CQFAILiveMapTransaction transaction, CQFAILiveMapEdit edit)
        {
            if (transaction.Backend is not CQFAILiveMap backend) return;
            if (!ReferenceEquals(map, backend.Map)) { map = backend.Map; cells.Clear(); }
            var receipt = edit.Receipt;
            if ((int?)receipt.Attribute("x") is not int x || (int?)receipt.Attribute("z") is not int z) return;
            IntVec3 cell = new IntVec3(x, 0, z);
            if (!cell.InBounds(map)) return;
            if (cells.Count >= 64 && !cells.ContainsKey(cell)) cells.Remove(cells.OrderBy(pair => pair.Value).First().Key);
            cells[cell] = clock.Elapsed.TotalSeconds + 0.35;
        }
        public void Draw()
        {
            double now = clock.Elapsed.TotalSeconds;
            foreach (IntVec3 cell in cells.Where(pair => pair.Value < now).Select(pair => pair.Key).ToArray()) cells.Remove(cell);
            if (map != null && ReferenceEquals(Find.CurrentMap, map) && cells.Count > 0) GenDraw.DrawFieldEdges(cells.Keys.ToList(), CQFUIStyle.Accent);
        }
        private readonly Stopwatch clock = Stopwatch.StartNew();
        private readonly Dictionary<IntVec3, double> cells = new Dictionary<IntVec3, double>();
        private Map? map;
    }
}
