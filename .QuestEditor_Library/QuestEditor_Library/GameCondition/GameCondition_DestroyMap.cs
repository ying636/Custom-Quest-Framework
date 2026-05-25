using RimWorld;
using Verse;

namespace QuestEditor_Library;

public class GameCondition_DestroyMap : GameCondition
{
    public override string Label => base.Label.Formatted(this.TicksLeft.ToStringTicksToDays());

    public override void End()
    {
        base.End();
        Map map = this.SingleMap;
        if (map.Parent is MapParent_Custom custom)
        { 
            custom.entrance.Destroy();
        }
    }
 
}