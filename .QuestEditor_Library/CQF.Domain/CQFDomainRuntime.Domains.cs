using System.Diagnostics.CodeAnalysis;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace QuestEditor_Library;

public sealed partial class CQFDomainRuntime
{
    public IReadOnlyList<Map> DomainMaps => domainMaps;

    // Domain maps can be missing after a load: older releases dropped every hibernating map record,
    // leaving holes in this list (see CQFDomainRuntime.DropLostDomainMaps). Every walk over the list
    // has to tolerate them instead of dereferencing Map.Parent.
    private static bool IsLiveDomainMap([NotNullWhen(true)] Map? map) => map != null && !map.Disposed
        && map.Parent != null && !map.Parent.Destroyed;

    public IEnumerable<Map> GetMaps(DomainDef definition) => domainMaps.Where(map => IsLiveDomainMap(map)
        && (map.GetComponent<MapComponent_Domain>().Definition == definition
            || map.GetComponent<MapComponent_Domain>().Challenger?.Definition == definition));

    public IEnumerable<MapComponent_Domain> GetInstances(DomainDef definition)
    {
        foreach (Map realm in GetMaps(definition))
        {
            MapComponent_Domain domain = realm.GetComponent<MapComponent_Domain>();
            if (domain.Definition == definition) yield return domain;
            if (domain.Challenger?.Definition == definition) yield return domain.Challenger;
        }
    }

    public bool HasAvailable(DomainDef definition) => GetMaps(definition)
        .Any(map => map.mapPawns.AllPawns.Count == 0 && map.GetComponent<MapComponent_Domain>().Available);

    public bool HasTerrainTemplate(DomainDef definition) => mapTemplates.ContainsKey(definition);

    internal DomainTerrainSnapshot GetTerrainTemplate(DomainDef definition, Map source)
        => GetMapTemplate(definition, source).Terrain;

    internal DomainMapTemplate GetMapTemplate(DomainDef definition, Map source)
    {
        if (mapTemplates.TryGetValue(definition, out DomainMapTemplate? template)) return template;
        Map? prepared = domainMaps.FirstOrDefault(map => IsLiveDomainMap(map)
            && map.GetComponent<MapComponent_Domain>().Definition == definition && map.GetComponent<MapComponent_Domain>().Available);
        if (prepared != null) return mapTemplates[definition] = new DomainMapTemplate(prepared);
        host.Hibernate(GenerateDomain(definition, source));
        return mapTemplates[definition];
    }

    public Map Acquire(DomainDef definition, Pawn pawn)
    {
        Map? realm = GetMaps(definition).FirstOrDefault(map => map.mapPawns.AllPawns.Count == 0 && map.GetComponent<MapComponent_Domain>().Available);
        if (realm == null) realm = GenerateDomain(definition, pawn.Map);
        else
        {
            if (host.IsHibernating(realm) || host.IsHibernateRequested(realm)) host.Activate(realm);
            if (!game.Maps.Contains(realm)) throw new InvalidOperationException("[CQF] Domain activation did not register its map.");
        }
        ((MapParent_Domain)realm.Parent).sourceMap = pawn.Map;
        realm.Parent.SetFaction(pawn.Faction);
        return realm;
    }

    public void Recycle(MapComponent_Domain domain, Map destination)
    {
        Map realm = domain.map;
        if (domain.Battlefield.HasOtherActiveDomain(domain)) return;
        if (!domainMaps.Contains(realm) || realm.mapPawns.AllPawns.Count > 0)
            throw new InvalidOperationException("[CQF] Cannot recycle an unregistered or occupied domain map.");
        foreach (Thing thing in realm.listerThings.AllThings.ToList()) thing.Destroy();
        foreach (Verse.AI.Group.Lord lord in realm.lordManager.lords.ToList()) realm.lordManager.RemoveLord(lord);
        domain.Battlefield.FinishContest(destination);
        GetTerrainTemplate(domain.Battlefield.Definition, destination).Restore(realm);
        host.Hibernate(realm);
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
        if (definition.preload && PreparedMapCount(definition) < 2 && !failedPreparation.Contains(definition))
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
            if (definition.preload && (PreparedMapCount(definition) < 2 || !HasTerrainTemplate(definition))) pendingDomainPreloads.Add(definition);
        preloadCheckRequested = pendingDomainPreloads.Count > 0;
    }

    private void ProcessDomainPreloads()
    {
        InitializeDomainPreloads();
        if (!preloadCheckRequested || pendingDomainPreloads.Count == 0) return;
        preloadCheckRequested = false;
        Map? source = game.Maps.FirstOrDefault(map => map.IsPlayerHome && map.Parent is not MapParent_Domain && !host.IsHibernating(map) && !host.IsHibernateRequested(map))
            ?? game.Maps.FirstOrDefault(map => map.Parent is not MapParent_Domain && !host.IsHibernating(map) && !host.IsHibernateRequested(map));
        if (source == null) return;
        foreach (DomainDef definition in pendingDomainPreloads.ToArray())
        {
            pendingDomainPreloads.Remove(definition);
            if (!preparing.Contains(definition) && !failedPreparation.Contains(definition)
                && (PreparedMapCount(definition) < 2 || !HasTerrainTemplate(definition)))
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
                if (Current.Game != game) return;
                if (source.Disposed || !game.Maps.Contains(source))
                {
                    pendingDomainPreloads.Add(definition);
                    preloadCheckRequested = true;
                    return;
                }
                while (PreparedMapCount(definition) < 2) host.Hibernate(GenerateDomain(definition, source));
                GetTerrainTemplate(definition, source);
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
        System.Diagnostics.Stopwatch timer = System.Diagnostics.Stopwatch.StartNew();
        MapParent_Domain parent = (MapParent_Domain)WorldObjectMaker.MakeWorldObject(definition.worldObject);
        parent.SetFaction(Faction.OfPlayer);
        parent.mapGenerator = definition.mapGenerator;
        Map realm = GameTools.GenerateSubMap(definition.size, parent, parent.mapGenerator, Array.Empty<GenStepWithParams>(), source);
        long generationMs = timer.ElapsedMilliseconds;
        realm.GetComponent<MapComponent_Domain>().Configure(definition);
        realm.mapDrawer.RegenerateEverythingNow();
        domainMaps.Add(realm);
        mapTemplates[definition] = new DomainMapTemplate(realm);
        Log.Message("[CQF] Prepared domain " + definition.defName + " map " + realm.uniqueID + ": generation=" + generationMs
            + " ms, rendering/template=" + (timer.ElapsedMilliseconds - generationMs) + " ms, total=" + timer.ElapsedMilliseconds + " ms.");
        pendingDomainPreloads.Remove(definition);
        return realm;
    }

    private int PreparedMapCount(DomainDef definition)
        => domainMaps.Count(map => IsLiveDomainMap(map) && map.GetComponent<MapComponent_Domain>().Definition == definition);

    private void UpdateDomains()
    {
        if (domainMaps.Count == 0) return;
        foreach (Map realm in game.Maps)
            if (realm.Parent is MapParent_Domain && domainMaps.Contains(realm)) updatingDomains.Add(realm);
        try
        {
            foreach (Map realm in updatingDomains)
            {
                if (realm.Disposed || realm.Parent.Destroyed) UnregisterDomainMap(realm);
                else if (!host.IsHibernating(realm))
                {
                    MapComponent_Domain battlefield = realm.GetComponent<MapComponent_Domain>();
                    battlefield.Worker?.ManagedUpdate();
                    battlefield.Challenger?.Worker?.ManagedUpdate();
                }
            }
        }
        finally { updatingDomains.Clear(); }
    }

    private void ProcessDomainReturns()
    {
        if (returns.Count == 0) return;
        processingReturns.AddRange(returns);
        returns.Clear();
        try
        {
            foreach (MapComponent_Domain domain in processingReturns)
            {
                if (transition != null && transition.ReturningDomain != domain) QueueReturn(domain);
                else domain.CompleteReturn();
            }
        }
        finally { processingReturns.Clear(); }
    }

    private List<Map> domainMaps = new();
    private Dictionary<DomainDef, DomainMapTemplate> mapTemplates = new();
    private readonly List<Map> updatingDomains = new();
    private readonly List<MapComponent_Domain> returns = new();
    private readonly List<MapComponent_Domain> processingReturns = new();
    private readonly HashSet<DomainDef> pendingDomainPreloads = new();
    private readonly HashSet<DomainDef> preparing = new();
    private readonly HashSet<DomainDef> failedPreparation = new();
    private bool domainPreloadsInitialized;
    private bool preloadCheckRequested;
}
