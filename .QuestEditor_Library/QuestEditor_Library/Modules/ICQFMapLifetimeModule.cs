using Verse;

namespace QuestEditor_Library;

public interface ICQFMapLifetimeModule
{
    bool KeepsMapActive(Game game, Map map);
    void BeforeMapsDisposed(Game game);
}
