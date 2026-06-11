using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace QuestEditor_Library;

public class CustomMapBackgroundEffectDef : Def
{
    public BackgroundEffectWorker CreateWorker(Map map)
    {
        Type type = this.workerClass ?? typeof(BackgroundEffectWorker_BlinkStars);
        BackgroundEffectWorker worker = (BackgroundEffectWorker)Activator.CreateInstance(type);
        worker.Init(this, map);
        return worker;
    }

    public Type workerClass;
    public List<string> texturePaths = new List<string>();
    public IntRange spawnIntervalTicks = new IntRange(120, 240);
    public IntRange lifeTimeTicks = new IntRange(120, 240);
    public FloatRange scaleRange = new FloatRange(1f, 1f);
    public FloatRange alphaRange = new FloatRange(0.5f, 1f);
    public FloatRange speedRange = new FloatRange(0.1f, 0.2f);
    public FloatRange rotationRange = new FloatRange(0f, 360f);
    public Color color = Color.white;
    public int maxParticles = 30;
    public bool startOnRegenerate;
}
