using Verse;

namespace QuestEditor_Library;

public interface ICQFMapLifetimeCollector : ICQFMapLifetimeModule
{
    void CollectActiveMaps(Game game, HashSet<Map> maps);
}
