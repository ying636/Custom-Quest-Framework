using UnityEngine;

namespace QuestEditor_Library;

public class BackgroundEffectParticle
{
    public bool Expired(int ticksGame)
    {
        return ticksGame >= this.spawnTick + this.lifeTimeTicks;
    }

    public float Progress(int ticksGame)
    {
        return Mathf.Clamp01((float)(ticksGame - this.spawnTick) / this.lifeTimeTicks);
    }

    public Vector3 PositionAt(int ticksGame)
    {
        return this.startPosition + this.velocity * (ticksGame - this.spawnTick);
    }

    public Material material;
    public Vector3 startPosition;
    public Vector3 velocity;
    public Vector2 size = Vector2.one;
    public Color color = Color.white;
    public float alpha = 1f;
    public float rotation;
    public float altitudeOffset;
    public int spawnTick;
    public int lifeTimeTicks;
}
