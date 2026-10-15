using RimWorld.Planet;

namespace QuestEditor_Library;

public class MapParent_Domain : PocketMapParent
{
    public override bool ShouldRemoveMapNow(out bool alsoRemoveWorldObject)
    {
        alsoRemoveWorldObject = false;
        return false;
    }
}
