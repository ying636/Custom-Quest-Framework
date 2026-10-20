using QuestEditor_Library;
using RimWorld;
using UnityEngine;
using Verse;

namespace CQF.DomainContestRuntimeChecks;

public sealed class ContestChecksDriver : MonoBehaviour
{
    private string ResultPath => Path.Combine(GenFilePaths.SaveDataFolderPath, "DomainContestChecks.txt");
    private DomainDef ChallengerDefinition => DefDatabase<DomainDef>.GetNamed(GenCommandLine.CommandLineArgPassed("cqfcontestbloodsea")
        ? "RSH_BloodSeaDomain" : "CQF_TestDomain");

    public void Resume(int first, int second)
    {
        File.AppendAllText(ResultPath, "TRACE Resumed domains=" + CQFDomainRuntime.Instance.DomainMaps.Count
            + ", gameMaps=" + Current.Game.Maps.Count + "\n");
        foreach (Map map in Current.Game.Maps)
        {
            MapComponent_Domain domain = map.GetComponent<MapComponent_Domain>();
            File.AppendAllText(ResultPath, "TRACE Map " + map.uniqueID + ", active=" + domain.Active + ", challenger=" + domain.Challenger?.Active + "\n");
        }
        firstId = first;
        secondId = second;
        stage = 2;
    }

    private void Update()
    {
        if (stage == 8)
        {
            if (Time.realtimeSinceStartup - lastLoadingTrace > 10f)
            {
                lastLoadingTrace = Time.realtimeSinceStartup;
                File.AppendAllText(ResultPath, "TRACE Loading " + Scribe.mode + ", state=" + Current.ProgramState
                    + ", longEvent=" + LongEventHandler.AnyEventNowOrWaiting + "\n");
            }
            return;
        }
        if (finished || LongEventHandler.AnyEventNowOrWaiting) return;
        try
        {
            if (Time.realtimeSinceStartup > 240f) throw new TimeoutException("Contest checks stage " + stage);
            if (stage == 0)
            {
                if (Current.Game?.CurrentMap == null) return;
                CQFDomainRuntime runtime = CQFDomainRuntime.Instance;
                DomainDef preparedDefinition = DefDatabase<DomainDef>.GetNamed("CQF_TestDomain");
                if (!runtime.HasAvailable(preparedDefinition) || !runtime.HasTerrainTemplate(preparedDefinition)
                    || !runtime.HasAvailable(ChallengerDefinition) || !runtime.HasTerrainTemplate(ChallengerDefinition)) return;
                int preparedMapCount = runtime.DomainMaps.Count;
                source = Find.CurrentMap;
                firstPawn = source.mapPawns.FreeColonistsSpawned.First();
                secondPawn = PawnGenerator.GeneratePawn(PawnKindDefOf.Colonist, Faction.OfPlayer);
                IntVec3 secondPosition = source.GetComponent<MapComponent_Domain>().FindFreeCell(source, firstPawn.Position + new IntVec3(8, 0, 0), true);
                GenSpawn.Spawn(secondPawn, secondPosition, source);
                secondPawn.abilities ??= new Pawn_AbilityTracker(secondPawn);
                firstId = firstPawn.thingIDNumber;
                secondId = secondPawn.thingIDNumber;
                DomainDef definition = DefDatabase<DomainDef>.GetNamed("CQF_TestDomain");
                MapComponent_Domain first = CQFDomainRuntime.Instance.EnterOrContest(definition, firstPawn, new[] { secondPawn });
                realm = first.map;
                MapComponent_Domain second = CQFDomainRuntime.Instance.EnterOrContest(ChallengerDefinition, secondPawn, Array.Empty<Pawn>());
                Check(runtime.DomainMaps.Count == preparedMapCount, "counter-expansion reuses cached terrain without generating or activating a template map");
                Check(second != first && second.map == realm && first.Challenger == second && !realm.components.Contains(second), "counter-expansion shares the original battlefield without registering its secondary instance as a map component");
                Check(realm.components.Count(component => component is MapComponent_Domain) == 1,
                    "vanilla creates exactly one domain map component and cannot replace it with the challenger type");
                Check(first.Participants.Count == 1 && second.Participants.Count == 1, "counter-expansion transfers participant ownership");
                Check(first.Anchor?.Domain == first && second.Anchor?.Domain == second, "both external anchors bind their own domain instance");
                first.DrainHealth(first.MaximumHealth * 0.5f);
                second.DrainHealth(second.MaximumHealth * 0.1f);
                Current.Game.CurrentMap = realm;
                Find.CameraDriver.JumpToCurrentMapLoc(realm.Center);
                stage = 1;
                frames = 0;
            }
            else if (stage == 1)
            {
                MapComponent_Domain first = realm!.GetComponent<MapComponent_Domain>();
                first.Contest!.Tick(first);
                if (++frames < 150) return;
                Check(Math.Abs(first.Contest.Partition.FirstFraction - DomainTerritoryPartition.CalculateFraction(first.Health, first.Challenger!.Health)) < 0.02f
                    && first.Contest.Partition.CellCount == realm.Size.x * realm.Size.z,
                    "straight boundary converges to the health ratio while preserving the full map");
                Check(realm.AllCells.All(cell => first.Contest.Partition.IsFirst(cell)
                    == first.Contest.Partition.IsFirst(new IntVec3(cell.x, 0, 0))),
                    "every battlefield row shares the same straight divider");
                float boundary = first.Contest.Partition.BoundaryX;
                SectionLayer layer = realm.mapDrawer.SectionAt(new IntVec3((int)Math.Floor(boundary), 0, realm.Size.z / 2))
                    .GetLayer(typeof(SectionLayer_DomainContest));
                layer.Regenerate();
                Check(layer.Visible && layer.subMeshes.Any(mesh => mesh.finalized && mesh.verts.Any(vertex => Math.Abs(vertex.x - boundary) < 0.0001f)),
                    "native terrain geometry meets the divider inside a cell");
                if (GenCommandLine.CommandLineArgPassed("cqfcontestbloodsea"))
                {
                    TerrainDef terrain = first.Contest.TerrainAt(first.Challenger, realm.Center);
                    Material material = first.Challenger.Worker!.GetTerrainMaterial(terrain)!;
                    Check(first.Challenger.HealthFraction < 1f && material.GetFloat("_RSHDomainFullness") == 1f,
                        "blood sea stays filled within its territory despite reduced health during a contest");
                }
                ScreenCapture.CaptureScreenshot(Path.Combine(GenFilePaths.SaveDataFolderPath, "DomainContestBoundary.png"));
                Current.Game.GetComponent<GameComponent_ContestChecks>().Record(firstId, secondId);
                GameDataSaveLoader.SaveGame("CQFContestChecks");
                stage = 2;
                if (GenCommandLine.CommandLineArgPassed("cqfcontestloadchecks"))
                {
                    GameDataSaveLoader.LoadGame("CQFContestChecks");
                    stage = 8;
                }
            }
            else if (stage == 2)
            {
                realm = CQFDomainRuntime.Instance.DomainMaps.FirstOrDefault(map => map.GetComponent<MapComponent_Domain>().Challenger?.Active == true);
                if (realm == null) return;
                MapComponent_Domain first = realm.GetComponent<MapComponent_Domain>();
                firstPawn = first.Caster!;
                secondPawn = first.Challenger!.Caster!;
                source = first.SourceMap;
                Check(CQFDomainRuntime.Instance.HasTerrainTemplate(first.Definition) && CQFDomainRuntime.Instance.HasTerrainTemplate(ChallengerDefinition),
                    "terrain templates survive a full game save and load");
                Check(firstPawn.thingIDNumber == firstId && secondPawn.thingIDNumber == secondId
                    && first.Health == first.MaximumHealth * 0.5f && first.Challenger.Health == first.Challenger.MaximumHealth * 0.9f, "saved battlefield retains both casters and independent health");
                Check(first.Challenger.Anchor?.Domain == first.Challenger
                    && secondPawn.health.hediffSet.hediffs.OfType<Hediff_DomainProtection>().Single().Domain == first.Challenger,
                    "challenger anchor and damage protection retain independent domain references");
                Current.Game.CurrentMap = source;
                first.DrainHealth(first.Health);
                stage = 3;
            }
            else if (stage == 3)
            {
                MapComponent_Domain first = realm!.GetComponent<MapComponent_Domain>();
                if (!first.Trapped) return;
                Check(firstPawn!.Map == realm && secondPawn!.Map == realm && first.Challenger?.Active == true
                    && !GameComponent_MapManager.Instance!.IsHibernating(realm),
                    "first domain defeat traps its occupants while the challenger continues");
                first.Challenger!.RequestReturn("contest runtime checks");
                stage = 4;
            }
            else if (stage == 4)
            {
                if (firstPawn?.Map != source || secondPawn?.Map != source || !GameComponent_MapManager.Instance!.IsHibernating(realm!)) return;
                Check(realm!.GetComponent<MapComponent_Domain>().Available, "shared map hibernates after the last domain leaves");
                DomainDef definition = DefDatabase<DomainDef>.GetNamed("CQF_TestDomain");
                MapComponent_Domain first = CQFDomainRuntime.Instance.EnterOrContest(definition, firstPawn!, Array.Empty<Pawn>());
                MapComponent_Domain second = CQFDomainRuntime.Instance.EnterOrContest(ChallengerDefinition, secondPawn!, Array.Empty<Pawn>());
                realm = first.map;
                donor = second.map;
                if (GenCommandLine.CommandLineArgPassed("cqfcontestbloodsea"))
                {
                    Type layerType = GenTypes.GetTypeInAnyAssembly("RSH.SectionLayer_BloodSeaDomain");
                    SectionLayer layer = donor.mapDrawer.SectionAt(donor.Center).GetLayer(layerType);
                    Check(layer.Visible && layer.subMeshes.Any(mesh => mesh.finalized && mesh.mesh.vertexCount > 0),
                        "standalone blood sea terrain meshes remain available before overlap merge");
                }
                Thing wall = ThingMaker.MakeThing(ThingDefOf.Wall, ThingDefOf.Steel);
                GenSpawn.Spawn(wall, second.FindFreeCell(donor, donor.Center + new IntVec3(5, 0, 0), false), donor);
                buildingId = wall.thingIDNumber;
                Thing loot = ThingMaker.MakeThing(ThingDefOf.Steel);
                GenSpawn.Spawn(loot, donor.Center, donor);
                lootId = loot.thingIDNumber;
                Check(realm != donor, "overlap case begins with two separate active maps");
                stage = 5;
            }
            else if (stage == 5)
            {
                MapComponent_Domain first = realm!.GetComponent<MapComponent_Domain>();
                if (first.Challenger == null) return;
                Check(secondPawn!.Map == realm && GameComponent_MapManager.Instance!.IsHibernating(donor!),
                    "overlapping external anchors merge their battlefields and hibernate the unused map");
                Check(realm.listerThings.AllThings.Any(thing => thing.thingIDNumber == buildingId)
                    && realm.listerThings.AllThings.Any(thing => thing.thingIDNumber == lootId),
                    "merging preserves donor buildings and loot without wiping destination structures");
                Current.Game.CurrentMap = source;
                first.Challenger.DrainHealth(first.Challenger.Health);
                stage = 6;
            }
            else if (stage == 6)
            {
                MapComponent_Domain first = realm!.GetComponent<MapComponent_Domain>();
                if (first.Challenger?.Trapped != true) return;
                Check(secondPawn!.Map == realm && firstPawn!.Map == realm && first.Active && !GameComponent_MapManager.Instance!.IsHibernating(realm),
                    "challenger defeat preserves the original winner and battlefield");
                first.RequestReturn("contest runtime checks");
                stage = 7;
            }
            else if (stage == 7)
            {
                if (firstPawn?.Map != source || secondPawn?.Map != source || !GameComponent_MapManager.Instance!.IsHibernating(realm!)) return;
                Check(Current.Game.components.Count(component => component is GameComponent_MapManager) == 1, "both entry paths retain one map manager");
                Finish(true);
            }
        }
        catch (Exception error)
        {
            File.AppendAllText(ResultPath, "FAIL " + error + "\n");
            Log.Error("[CQF] Contest runtime checks failed: " + error);
            Finish(false);
        }
    }

    private void Check(bool condition, string name)
    {
        if (!condition) throw new InvalidOperationException(name);
        File.AppendAllText(ResultPath, "PASS " + name + "\n");
    }

    private void Finish(bool success)
    {
        finished = true;
        File.AppendAllText(ResultPath, success ? "COMPLETE\n" : "FAILED\n");
        Application.Quit(success ? 0 : 1);
    }

    private Map? source;
    private Map? realm;
    private Map? donor;
    private Pawn? firstPawn;
    private Pawn? secondPawn;
    private int firstId;
    private int secondId;
    private int buildingId;
    private int lootId;
    private int stage;
    private int frames;
    private bool finished;
    private float lastLoadingTrace;
}
