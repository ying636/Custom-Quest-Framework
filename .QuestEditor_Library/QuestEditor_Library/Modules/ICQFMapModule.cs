using Verse;

namespace QuestEditor_Library;

public interface ICQFMapModule : IExposable, IDisposable
{
    void Update();
    void OnGUI();
    void StartedNewGame();
    void LoadedGame();
    void MapAdded(Map map);
}
