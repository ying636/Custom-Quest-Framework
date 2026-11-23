using RimWorld;
using Verse;
using Verse.AI;

namespace QuestEditor_Library;

public sealed class CQFCompositeMapPortal : Building
{
    internal Map? Destination => destination;
    internal IntVec3 Arrival => arrival;

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_References.Look(ref destination, "destination");
        Scribe_Values.Look(ref arrival, "arrival");
    }

    public override IEnumerable<Gizmo> GetGizmos()
    {
        foreach (Gizmo gizmo in base.GetGizmos()) yield return gizmo;
        if (!Spawned) yield break;
        if (destination is { Disposed: false } target)
        {
            string name = target.Parent is MapParent_Custom parent ? parent.MapName : target.Parent.LabelCap.ToString();
            yield return new Command_Action
            {
                defaultLabel = "CQF_Composite_PortalTravel".Translate(name),
                defaultDesc = "CQF_Composite_SelectTraveler".Translate(),
                icon = TexCommand.Install,
                action = BeginTravel
            };
        }
        if (GameComponent_MapManager.Instance is not GameComponent_MapManager manager ||
            manager.GetCompositeMap(destination) is not CQFCompositeMapInstance instance || instance.Entrance != this)
            yield break;
        yield return new Command_Action
        {
            defaultLabel = instance.DisplayName,
            icon = TexCommand.Install,
            action = () => manager.ViewCompositeMap(instance)
        };
    }

    internal void Bind(Map map, IntVec3 cell)
    {
        destination = map;
        arrival = cell;
    }

    private void BeginTravel()
    {
        TargetingParameters parameters = new()
        {
            canTargetPawns = true,
            canTargetBuildings = false,
            canTargetLocations = false,
            validator = target => target.Thing is Pawn { IsColonistPlayerControlled: true } pawn && pawn.Map == Map
        };
        Find.Targeter.BeginTargeting(parameters, target =>
        {
            if (target.Thing is not Pawn pawn || !Spawned || pawn.Map != Map) return;
            if (!pawn.CanReach(this, PathEndMode.Touch, Danger.Deadly))
            {
                Messages.Message("NoPath".Translate(), pawn, MessageTypeDefOf.RejectInput, historical: false);
                return;
            }
            pawn.jobs.TryTakeOrderedJob(JobMaker.MakeJob(CQFCompositeMapDefs.CQF_CompositeMapTravel, this), JobTag.Misc);
        }, requiresCastedSelected: false);
    }

    private Map? destination;
    private IntVec3 arrival;
}
