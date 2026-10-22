using RimWorld;
using Verse;

namespace QuestEditor_Library;

public sealed class CQFCinematicContext
{
    public CQFCinematicContext(CQFCinematicInstance instance) { Instance = instance; }

    public CQFCinematicInstance Instance { get; }
    public Map Map => Instance.Map;
    public Quest? Quest => Instance.Quest;
    public bool IsPreview => Instance.IsPreview;
    public Dictionary<string, TargetInfo> Targets => Instance.Targets;
    public CQFCinematicPresentation Presentation => Instance.Presentation;

    public TargetInfo ResolveTarget(string key)
    {
        if (string.IsNullOrWhiteSpace(key)) key = "Position";
        if (Targets.TryGetValue(key, out TargetInfo direct) && direct.IsValid) return direct;
        Dictionary<string, TargetInfo> found = GameTools.GetTargets(Targets, Quest!, new List<string> { key });
        if (found.Count != 1 || !found.First().Value.IsValid)
            throw new InvalidOperationException("[CQF] Cinematic requires one valid target: " + key);
        return found.First().Value;
    }

    public string SignalName(string signal, bool addQuestPrefix = true)
        => addQuestPrefix && Quest != null ? "Quest" + Quest.id + "." + signal : signal;

    public int SignalVersion(string signal)
    {
        if (Instance.SignalVersions.TryGetValue(signal, out int version)) return version;
        Instance.SignalVersions.Add(signal, 0);
        return 0;
    }

    public void SendSignal(string signal, bool addQuestPrefix = true)
    {
        if (IsPreview || string.IsNullOrWhiteSpace(signal)) return;
        SignalArgs args = default;
        foreach (KeyValuePair<string, TargetInfo> target in Targets)
            if (target.Value.HasThing) args.Add(target.Value.Thing.Named(target.Key));
        Find.SignalManager.SendSignal(new Signal(SignalName(signal, addQuestPrefix), args));
    }
}
