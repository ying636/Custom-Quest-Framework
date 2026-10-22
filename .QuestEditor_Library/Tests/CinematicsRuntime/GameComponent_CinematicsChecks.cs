using QuestEditor_Library;
using RimWorld;
using Verse;

namespace CQF.CinematicsRuntimeChecks;

public sealed class GameComponent_CinematicsChecks : GameComponent, ISignalReceiver
{
    public GameComponent_CinematicsChecks(Game game) { }

    public int Count(string signal) => Events.Count(entry => entry == signal);

    public override void StartedNewGame() => Find.SignalManager.RegisterReceiver(this);

    public override void LoadedGame()
    {
        LoadCount++;
        Find.SignalManager.RegisterReceiver(this);
    }

    public void Notify_SignalReceived(Signal signal)
    {
        if (signal.tag.StartsWith("CQF_Runtime_", StringComparison.Ordinal)) Events.Add(signal.tag);
    }

    public override void ExposeData()
    {
        Scribe_References.Look(ref Actor, "actor");
        Scribe_Values.Look(ref Origin, "origin");
        Scribe_Values.Look(ref Destination, "destination");
        Scribe_Values.Look(ref SavedElapsed, "savedElapsed");
        Scribe_Values.Look(ref SavedStep, "savedStep");
        Scribe_Values.Look(ref LoadCount, "loadCount");
        Scribe_Values.Look(ref SkipActions, "skipActions");
        Scribe_Values.Look(ref CoverActions, "coverActions");
        Scribe_Values.Look(ref ObservedCoverVersion, "observedCoverVersion");
        Scribe_Collections.Look(ref Events, "events", LookMode.Value);
        if (Scribe.mode == LoadSaveMode.PostLoadInit) Events ??= new();
    }

    public Pawn? Actor;
    public IntVec3 Origin;
    public IntVec3 Destination;
    public float SavedElapsed;
    public int SavedStep;
    public int LoadCount;
    public int SkipActions;
    public int CoverActions;
    public int ObservedCoverVersion;
    public List<string> Events = new();
}
