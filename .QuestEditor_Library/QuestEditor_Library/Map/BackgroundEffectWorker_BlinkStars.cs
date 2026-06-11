using UnityEngine;
using Verse;

namespace QuestEditor_Library;

public class BackgroundEffectWorker_BlinkStars : BackgroundEffectWorker
{
    protected override void SpawnInitialParticles()
    {
        int count = Mathf.Min(this.def.maxParticles, Mathf.RoundToInt(this.map.Size.x * this.map.Size.z / 650f));
        int ticksGame = Find.TickManager.TicksGame;
        for (int i = 0; i < count; i++)
        {
            this.TrySpawnParticle(ticksGame - Rand.Range(0, this.def.lifeTimeTicks.max));
        }
    }

    protected override void TrySpawnParticle(int ticksGame)
    {
        if (this.particles.Count >= this.def.maxParticles)
        {
            return;
        }
        Material material = this.MaterialForRandomTexture();
        if (material == null)
        {
            return;
        }
        float scale = this.def.scaleRange.RandomInRange;
        this.particles.Add(new BackgroundEffectParticle
        {
            material = material,
            startPosition = this.RandomPositionInDrawRect(),
            size = this.RandomTextureSize(material, scale),
            color = this.def.color,
            alpha = this.def.alphaRange.RandomInRange,
            rotation = this.def.rotationRange.RandomInRange,
            altitudeOffset = Rand.Range(0f, 0.02f),
            spawnTick = ticksGame,
            lifeTimeTicks = this.def.lifeTimeTicks.RandomInRange
        });
    }
}
