using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace QuestEditor_Library;

public abstract class BackgroundEffectWorker
{
    public virtual void Init(CustomMapBackgroundEffectDef def, Map map)
    {
        this.def = def;
        this.map = map;
        this.nextSpawnTick = Find.TickManager.TicksGame + def.spawnIntervalTicks.RandomInRange;
        if (def.startOnRegenerate)
        {
            this.SpawnInitialParticles();
        }
    }

    public virtual void Tick()
    {
        int ticksGame = Find.TickManager.TicksGame;
        for (int i = this.particles.Count - 1; i >= 0; i--)
        {
            BackgroundEffectParticle particle = this.particles[i];
            if (particle.Expired(ticksGame))
            {
                this.particles.RemoveAt(i);
            }
        }
        if (ticksGame >= this.nextSpawnTick)
        {
            this.TrySpawnParticle(ticksGame);
            this.nextSpawnTick = ticksGame + this.def.spawnIntervalTicks.RandomInRange;
        }
    }

    public virtual void Draw(MaterialPropertyBlock propertyBlock, float altitude)
    {
        int ticksGame = Find.TickManager.TicksGame;
        foreach (BackgroundEffectParticle particle in this.particles)
        {
            if (particle.material == null)
            {
                continue;
            }
            Color color = particle.color;
            color.a *= this.AlphaFor(particle, ticksGame);
            if (color.a <= 0.001f)
            {
                continue;
            }
            propertyBlock.SetColor(ShaderPropertyIDs.Color, color);
            Vector3 position = particle.PositionAt(ticksGame);
            position.y = altitude + particle.altitudeOffset;
            Matrix4x4 matrix = Matrix4x4.TRS(position, Quaternion.AngleAxis(particle.rotation, Vector3.up), new Vector3(particle.size.x, 1f, particle.size.y));
            Graphics.DrawMesh(MeshPool.plane10, matrix, particle.material, 0, null, 0, propertyBlock);
        }
    }

    protected virtual float AlphaFor(BackgroundEffectParticle particle, int ticksGame)
    {
        float progress = particle.Progress(ticksGame);
        return Mathf.Sin(progress * Mathf.PI) * particle.alpha;
    }

    protected virtual void SpawnInitialParticles()
    {
    }

    protected abstract void TrySpawnParticle(int ticksGame);

    protected Material MaterialForRandomTexture()
    {
        if (this.def.texturePaths.NullOrEmpty())
        {
            return null;
        }
        string texturePath = this.def.texturePaths.RandomElement();
        Texture2D texture = ContentFinder<Texture2D>.Get(texturePath, false);
        return texture == null ? null : MaterialPool.MatFrom(texture, ShaderDatabase.Transparent, Color.white, EffectRenderQueue);
    }

    protected Vector3 RandomPositionInDrawRect(float border = 0f)
    {
        CellRect bounds = this.map.BoundsRect(0);
        return new Vector3(Rand.Range(bounds.minX - border, bounds.maxX + border), 0f, Rand.Range(bounds.minZ - border, bounds.maxZ + border));
    }

    protected Vector2 RandomTextureSize(Material material, float scale)
    {
        if (material?.mainTexture == null)
        {
            return Vector2.one * scale;
        }
        float width = Mathf.Max((float)material.mainTexture.width / material.mainTexture.height, 1f) * scale;
        float height = Mathf.Max((float)material.mainTexture.height / material.mainTexture.width, 1f) * scale;
        return new Vector2(width, height);
    }

    protected const int EffectRenderQueue = 1905;

    protected CustomMapBackgroundEffectDef def;
    protected Map map;
    protected int nextSpawnTick;
    protected readonly List<BackgroundEffectParticle> particles = new List<BackgroundEffectParticle>();
}
