using Verse;

public sealed class CQFCheckRuntimeHolder : Thing, IThingHolder
{
    public ThingOwner GetDirectlyHeldThings() => contents;
    public void GetChildHolders(List<IThingHolder> outChildren) { }
    public ThingOwner contents = null!;
}
