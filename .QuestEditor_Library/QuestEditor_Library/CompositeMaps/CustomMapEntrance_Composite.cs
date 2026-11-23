using RimWorld;
using Verse;

namespace QuestEditor_Library;

public class CustomMapEntrance_Composite : CustomMapEntrance_Chance
{
    public CQFCompositeMapPlacement Placement => def.GetModExtension<ModExtension_CQFCompositeEntrance>()?.layout
        ?? CQFCompositeMapPlacement.Vertical;
    public CQFCompositeMapGroup? CompositeGroup => GameComponent_MapManager.Instance?.GetCompositeGroup(this);
    public override bool DestroyMapWhenDeSpawn =>
        GameComponent_MapManager.Instance?.CompositeBranchRemovalBlock(this) == null;

    public override void SetMapDef(CustomMapDataDef mapDef)
    {
        if (tagWithChance.Count == 0 && mapDefWithChance.Count == 0)
            this.mapDef = mapDef;
        else
            base.SetMapDef(mapDef);
    }

    public override void Destroy(DestroyMode mode = DestroyMode.Vanish)
    {
        GameComponent_MapManager? manager = GameComponent_MapManager.Instance;
        if (manager?.CompositeBranchRemovalBlock(this) is { } blocked)
        {
            Messages.Message(blocked.Translate(), this, MessageTypeDefOf.RejectInput, historical: false);
            return;
        }
        manager?.DestroyEntranceCompositeBranch(this);
        base.Destroy(mode);
    }

    public override void DeSpawn(DestroyMode mode = DestroyMode.Vanish)
    {
        if (!preserveMapOnDespawn && DestroyMapWhenDeSpawn)
            GameComponent_MapManager.Instance?.DestroyEntranceCompositeBranch(this);
        base.DeSpawn(mode);
    }

    protected internal override void NotifySubMapGenerated(Map source, Map destination) =>
        GameComponent_MapManager.Instance?.RegisterCompositeEntrance(this, source, destination);
}
