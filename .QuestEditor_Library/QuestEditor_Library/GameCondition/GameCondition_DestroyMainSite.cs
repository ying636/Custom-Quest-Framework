using RimWorld;
using RimWorld.Planet;
using Verse;

namespace QuestEditor_Library;

public class GameCondition_DestroyMainSite : GameCondition
{
    public override string Label => base.Label.Formatted(this.TicksLeft.ToStringTicksToDays());

    public override void End()
    {
        base.End();
        Map map = this.SingleMap;
        if (map?.Parent is MainSite site)
        {
            MainMapWorldComponent.Component?.DestroyMainSite(site);
        }
        else if (map?.Parent is MapParent parent)
        {
            Current.Game.DeinitAndRemoveMap(map, true);
            if (!parent.Destroyed)
            {
                parent.Destroy();
            }
        }
    }
}
