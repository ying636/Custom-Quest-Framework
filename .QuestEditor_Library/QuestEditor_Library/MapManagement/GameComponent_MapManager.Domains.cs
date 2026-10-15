using RimWorld;
using RimWorld.Planet;
using Verse;

namespace QuestEditor_Library;

public sealed partial class GameComponent_MapManager
{
    public IReadOnlyList<Map> DomainMaps => domainMaps;

    public IEnumerable<Map> GetMaps(DomainDef definition) => domainMaps.Where(map => !map.Disposed
        && !map.Parent.Destroyed && map.GetComponent<MapComponent_Domain>().Definition == definition);

    public bool HasAvailable(DomainDef definition) => GetMaps(definition)
        .Any(map => map.GetComponent<MapComponent_Domain>().Available);

    public Map Acquire(DomainDef definition, Pawn pawn)
    {
        Map? realm = GetMaps(definition).FirstOrDefault(map => map.GetComponent<MapComponent_Domain>().Available);
        if (realm == null) realm = GenerateDomain(definition, pawn.Map);
        else
        {
            if (IsHibernating(realm) || IsHibernateRequested(realm)) Activate(realm);
            if (!game.Maps.Contains(realm)) throw new InvalidOperationException("[CQF] Domain activation did not register its map.");
        }
        ((MapParent_Domain)realm.Parent).sourceMap = pawn.Map;
        realm.Parent.SetFaction(pawn.Faction);
        return realm;
    }

    public void Recycle(MapComponent_Domain domain, Map destination)
    {
        Map realm = domain.map;
        if (!domainMaps.Contains(realm) || realm.mapPawns.AllPawns.Count > 0)
            throw new InvalidOperationException("[CQF] Cannot recycle an unregistered or occupied domain map.");
        foreach (Thing thing in realm.listerThings.AllThings.ToList()) thing.Destroy();
        foreach (Verse.AI.Group.Lord lord in realm.lordManager.lords.ToList()) realm.lordManager.RemoveLord(lord);
        domain.Worker!.Recycling(destination);
        Hibernate(realm);
    }

    public void QueueReturn(MapComponent_Domain domain)
    {
        if (!returns.Contains(domain)) returns.Add(domain);
    }

    internal void UnregisterDomainMap(Map map)
    {
        FinishTransitionFor(map);
        if (!domainMaps.Remove(map)) return;
        DomainDef definition = map.GetComponent<MapComponent_Domain>().Definition;
        if (definition.preload && !GetMaps(definition).Any() && !failedPreparation.Contains(definition))
        {
            pendingDomainPreloads.Add(definition);
            preloadCheckRequested = true;
        }
    }

    private void InitializeDomainPreloads()
    {
        if (domainPreloadsInitialized) return;
        domainPreloadsInitialized = true;
        foreach (DomainDef definition in DefDatabase<DomainDef>.AllDefsListForReading)
            if (definition.preload && !GetMaps(definition).Any()) pendingDomainPreloads.Add(definition);
        preloadCheckRequested = pendingDomainPreloads.Count > 0;
    }

    private void ProcessDomainPreloads()
    {
        InitializeDomainPreloads();
        if (!preloadCheckRequested || pendingDomainPreloads.Count == 0) return;
        preloadCheckRequested = false;
        Map? source = game.Maps.FirstOrDefault(map => map.IsPlayerHome && map.Parent is not MapParent_Domain)
            ?? game.Maps.FirstOrDefault(map => map.Parent is not MapParent_Domain);
        if (source == null) return;
        foreach (DomainDef definition in pendingDomainPreloads.ToArray())
        {
            pendingDomainPreloads.Remove(definition);
            if (!preparing.Contains(definition) && !failedPreparation.Contains(definition) && !GetMaps(definition).Any())
                QueueDomainPreload(definition, source);
        }
    }

    private void QueueDomainPreload(DomainDef definition, Map source)
    {
        preparing.Add(definition);
        LongEventHandler.ExecuteWhenFinished(() =>
        {
            try
            {
                if (Current.Game != game || GetMaps(definition).Any()) return;
                if (source.Disposed || !game.Maps.Contains(source))
                {
                    pendingDomainPreloads.Add(definition);
                    preloadCheckRequested = true;
                    return;
                }
                Hibernate(GenerateDomain(definition, source));
            }
            catch (Exception exception)
            {
                failedPreparation.Add(definition);
                Log.Error("[CQF] Domain preloading failed for " + definition.defName + ": " + exception);
            }
            finally { preparing.Remove(definition); }
        });
    }

    private Map GenerateDomain(DomainDef definition, Map source)
    {
        MapParent_Domain parent = (MapParent_Domain)WorldObjectMaker.MakeWorldObject(definition.worldObject);
        parent.SetFaction(Faction.OfPlayer);
        parent.mapGenerator = definition.mapGenerator;
        Map realm = GameTools.GenerateSubMap(definition.size, parent, parent.mapGenerator, Array.Empty<GenStepWithParams>(), source);
        realm.GetComponent<MapComponent_Domain>().Configure(definition);
        realm.mapDrawer.RegenerateEverythingNow();
        domainMaps.Add(realm);
        pendingDomainPreloads.Remove(definition);
        return realm;
    }

    private void UpdateDomains()
    {
        if (domainMaps.Count == 0) return;
        updatingDomains.AddRange(domainMaps);
        try
        {
            foreach (Map realm in updatingDomains)
            {
                if (realm.Disposed || realm.Parent.Destroyed) UnregisterDomainMap(realm);
                else realm.GetComponent<MapComponent_Domain>().Worker?.ManagedUpdate();
            }
        }
        finally { updatingDomains.Clear(); }
    }

    private void ProcessDomainReturns()
    {
        if (returns.Count == 0) return;
        MapComponent_Domain[] pending = returns.ToArray();
        returns.Clear();
        foreach (MapComponent_Domain domain in pending) domain.CompleteReturn();
    }

    private List<Map> domainMaps = new();
    private readonly List<Map> updatingDomains = new();
    private readonly List<MapComponent_Domain> returns = new();
    private readonly HashSet<DomainDef> pendingDomainPreloads = new();
    private readonly HashSet<DomainDef> preparing = new();
    private readonly HashSet<DomainDef> failedPreparation = new();
    private bool domainPreloadsInitialized;
    private bool preloadCheckRequested;
}
