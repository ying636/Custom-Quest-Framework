using Verse;
using Verse.Sound;

namespace QuestEditor_Library;

public sealed class CQFCinematicStep_Sound : CQFCinematicStep
{
    public override void Restore(CQFCinematicContext context, CQFCinematicStepState state)
    {
        if (sound.sustain) OnEnter(context, state);
    }

    public override IEnumerable<string> ConfigErrors()
    {
        foreach (string error in base.ConfigErrors()) yield return error;
        if (sound == null) yield return "CQFCinematicStep_Sound requires a sound.";
        if ((float.IsNaN(volume) || float.IsInfinity(volume)) || volume < 0f) yield return "CQFCinematicStep_Sound requires a nonnegative finite volume.";
    }

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Defs.Look(ref sound, "sound");
        Scribe_Values.Look(ref onCamera, "onCamera", true);
        Scribe_Values.Look(ref targetKey, "targetKey", "Position");
        Scribe_Values.Look(ref volume, "volume", 1f);
    }

    protected override void OnEnter(CQFCinematicContext context, CQFCinematicStepState state)
    {
        if (sound == null) throw new InvalidOperationException("CQF cinematic sound is missing.");
        SoundInfo info;
        if (onCamera) info = SoundInfo.OnCamera(sound.sustain ? MaintenanceType.PerFrame : MaintenanceType.None);
        else
        {
            TargetInfo target = context.ResolveTarget(targetKey);
            if (!target.IsValid || target.Map != context.Map) throw new InvalidOperationException("CQF cinematic sound target is missing or on another map: " + targetKey);
            info = SoundInfo.InMap(target, sound.sustain ? MaintenanceType.PerFrame : MaintenanceType.None);
        }
        info.volumeFactor = volume;
        if (sound.sustain)
        {
            state.runtime = sound.TrySpawnSustainer(info);
            if (state.runtime == null) throw new InvalidOperationException("CQF cinematic could not start sustainer: " + sound.defName);
        }
        else sound.PlayOneShot(info);
    }

    protected override bool OnUpdate(CQFCinematicContext context, CQFCinematicStepState state, float realSeconds, int gameTicks)
    {
        if (state.runtime is Sustainer sustainer)
        {
            if (sustainer.Ended) throw new InvalidOperationException("CQF cinematic sustainer ended unexpectedly: " + sound.defName);
            sustainer.Maintain();
        }
        return state.elapsed >= duration;
    }

    protected override void OnExit(CQFCinematicContext context, CQFCinematicStepState state, bool cancelled)
    {
        if (state.runtime is Sustainer sustainer && !sustainer.Ended) sustainer.End();
        state.runtime = null;
    }

    public SoundDef sound = null!;
    public bool onCamera = true;
    public string targetKey = "Position";
    public float volume = 1f;
}
