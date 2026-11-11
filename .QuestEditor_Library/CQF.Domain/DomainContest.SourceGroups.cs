using UnityEngine;
using Verse;

namespace QuestEditor_Library;

public sealed partial class DomainContest
{
#if RIMWORLD_1_7
    private float DrawSourceGroup(MapComponent_Domain domain, Building_Domain anchor)
    {
        if (sourceVisualError || !domain.Active || !anchor.Spawned) return 0f;
        List<MapComponent_Domain> neighbors = domain.Instances.Where(side => side != domain && side.Active
            && side.Anchor?.Spawned == true && side.Anchor.Map == anchor.Map
            && side.Anchor.Position.DistanceToSquared(anchor.Position) < (side.Definition.OpeningRadius + domain.Definition.OpeningRadius)
                * (side.Definition.OpeningRadius + domain.Definition.OpeningRadius)).ToList();
        if (neighbors.Count == 0) return 0f;
        if (sourceVisualTick < 0) sourceVisualTick = GenTicks.TicksGame;
        float age = Math.Max(0, GenTicks.TicksGame - sourceVisualTick) / 60f;
        float blend = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(age / 0.4f));
        try
        {
            foreach (MapComponent_Domain rival in neighbors.Where(side => side.InstanceIndex > domain.InstanceIndex))
            {
                var key = (domain.InstanceIndex, rival.InstanceIndex);
                if (!sourcePairs.TryGetValue(key, out DomainFusionVisuals? visuals))
                    sourcePairs[key] = visuals = new DomainFusionVisuals(domain.Definition, rival.Definition);
                visuals.Draw(anchor.Position.ToVector3Shifted(), domain.Definition.OpeningRadius,
                    rival.Anchor!.Position.ToVector3Shifted(), rival.Definition.OpeningRadius, GenTicks.TicksGame / 60f,
                    age, blend, DomainTerritoryPartition.CalculateFraction(domain.Health, rival.Health));
            }
            return blend;
        }
        catch (Exception error)
        {
            sourceVisualError = true;
            Log.Error("[CQF] Multiple-domain source-map interaction failed: " + error);
            return 0f;
        }
    }

    private readonly Dictionary<(int first, int second), DomainFusionVisuals> sourcePairs = new();
#endif
}
