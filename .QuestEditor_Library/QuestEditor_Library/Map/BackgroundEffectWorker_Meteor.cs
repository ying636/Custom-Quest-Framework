using UnityEngine;
using Verse;

namespace QuestEditor_Library;

public class BackgroundEffectWorker_Meteor : BackgroundEffectWorker
{
    protected override float AlphaFor(BackgroundEffectParticle particle, int ticksGame)
    {
        float progress = particle.Progress(ticksGame);
        if (progress < 0.15f)
        {
            return Mathf.InverseLerp(0f, 0.15f, progress) * particle.alpha;
        }
        if (progress > 0.85f)
        {
            return (1f - Mathf.InverseLerp(0.85f, 1f, progress)) * particle.alpha;
        }
        return particle.alpha;
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
        float angle = this.def.rotationRange.RandomInRange;
        float speed = this.def.speedRange.RandomInRange;
        Vector3 direction = Quaternion.AngleAxis(angle, Vector3.up) * Vector3.forward;
        CellRect bounds = this.map.BoundsRect(0);
        float border = Rand.Range(8f, 18f);
        Vector3 position = SpawnPositionFor(bounds, direction, border);
        this.particles.Add(new BackgroundEffectParticle
        {
            material = material,
            startPosition = position,
            velocity = direction * speed,
            size = this.RandomTextureSize(material, scale),
            color = this.def.color,
            alpha = this.def.alphaRange.RandomInRange,
            rotation = angle,
            altitudeOffset = Rand.Range(0.02f, 0.04f),
            spawnTick = ticksGame,
            lifeTimeTicks = Mathf.Max(this.def.lifeTimeTicks.RandomInRange, LifeTimeToCrossMap(bounds, speed, border))
        });
    }

    private static Vector3 SpawnPositionFor(CellRect bounds, Vector3 direction, float border)
    {
        if (Mathf.Abs(direction.x) >= Mathf.Abs(direction.z))
        {
            float x = direction.x >= 0f ? bounds.minX - border : bounds.maxX + border;
            return new Vector3(x, 0f, Rand.Range(bounds.minZ - border, bounds.maxZ + border));
        }
        float z = direction.z >= 0f ? bounds.minZ - border : bounds.maxZ + border;
        return new Vector3(Rand.Range(bounds.minX - border, bounds.maxX + border), 0f, z);
    }

    private static int LifeTimeToCrossMap(CellRect bounds, float speed, float border)
    {
        float distance = Mathf.Sqrt(bounds.Width * bounds.Width + bounds.Height * bounds.Height) + border * 2f;
        return Mathf.CeilToInt(distance / Mathf.Max(speed, 0.001f));
    }
}
