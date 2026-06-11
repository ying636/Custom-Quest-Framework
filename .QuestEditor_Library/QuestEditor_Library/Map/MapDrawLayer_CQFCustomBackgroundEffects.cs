using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace QuestEditor_Library;

public class MapDrawLayer_CQFCustomBackgroundEffects : MapDrawLayer
{
    public MapDrawLayer_CQFCustomBackgroundEffects(Map map) : base(map)
    {
        this.relevantChangeTypes = MapMeshFlagDefOf.Terrain;
    }

    public override bool Visible => this.Background is { Enabled: true } && !this.Background.backgroundEffects.NullOrEmpty();

    public override void Regenerate()
    {
        this.RecacheWorkers();
    }

    public override void DrawLayer()
    {
        if (!this.Visible)
        {
            return;
        }
        this.EnsureWorkers();
        float altitude = AltitudeLayer.BelowTerrain.AltitudeFor() + 0.01f;
        foreach (BackgroundEffectWorker worker in this.workers)
        {
            worker.Tick();
            worker.Draw(this.propertyBlock, altitude);
        }
        this.propertyBlock.Clear();
    }

    private CustomMapBackgroundData Background => MapComponent_CustomMapData.GetComp(base.Map)?.background;

    private void EnsureWorkers()
    {
        List<CustomMapBackgroundEffectDef> effects = this.Background?.backgroundEffects;
        if (this.workers == null || !SameDefs(effects, this.cachedDefs))
        {
            this.RecacheWorkers();
        }
    }

    private void RecacheWorkers()
    {
        this.workers = new List<BackgroundEffectWorker>();
        this.cachedDefs = this.Background?.backgroundEffects?.Where(def => def != null).ToList() ?? new List<CustomMapBackgroundEffectDef>();
        foreach (CustomMapBackgroundEffectDef def in this.cachedDefs)
        {
            this.workers.Add(def.CreateWorker(base.Map));
        }
    }

    private static bool SameDefs(List<CustomMapBackgroundEffectDef> a, List<CustomMapBackgroundEffectDef> b)
    {
        if (a == null || b == null)
        {
            return a == b;
        }
        List<CustomMapBackgroundEffectDef> cleanA = a.Where(def => def != null).ToList();
        if (cleanA.Count != b.Count)
        {
            return false;
        }
        for (int i = 0; i < cleanA.Count; i++)
        {
            if (cleanA[i] != b[i])
            {
                return false;
            }
        }
        return true;
    }

    private List<BackgroundEffectWorker> workers;
    private List<CustomMapBackgroundEffectDef> cachedDefs;
    private readonly MaterialPropertyBlock propertyBlock = new MaterialPropertyBlock();
}
