using QuestEditor_Library;
using Verse;

internal sealed class MapDisposalProbeModule : ICQFMapModule
{
    public void Update() { }
    public void OnGUI() { }
    public void StartedNewGame() { }
    public void LoadedGame() { }
    public void MapAdded(Map map) { }
    public void ExposeData() { }

    public void Dispose()
    {
        Disposals++;
        throw new InvalidOperationException("CQF_Check_ModuleDisposalFailure");
    }

    public int Disposals;
}
