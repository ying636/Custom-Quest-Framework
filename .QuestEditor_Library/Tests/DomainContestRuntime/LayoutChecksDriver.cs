using QuestEditor_Library;
using RimWorld;
using UnityEngine;
using Verse;

namespace CQF.DomainContestRuntimeChecks;

public sealed class LayoutChecksDriver : MonoBehaviour
{
    private string ResultPath => Path.Combine(GenFilePaths.SaveDataFolderPath, "DomainContestChecks.txt");
    private DomainDef Definition => DefDatabase<DomainDef>.GetNamed("CQF_CheckPlainDomain");

    public void Resume(int firstId, int secondId)
    {
        first = CQFDomainRuntime.Instance.GetInstances(Definition).First(side => side.Caster?.thingIDNumber == firstId);
        second = first.Challenger!;
        Check(second.Caster?.thingIDNumber == secondId, "plain contest restores both domain identities");
        source = first.SourceMap!;
        mapCount = CQFDomainRuntime.Instance.DomainMaps.Count;
        stage = 2;
    }

    private void Update()
    {
        if (finished || stage == 99 || LongEventHandler.AnyEventNowOrWaiting || Current.Game?.CurrentMap == null) return;
        try
        {
            if (Time.realtimeSinceStartup > 240f) throw new TimeoutException("Layout checks stage " + stage);
            CQFDomainRuntime runtime = CQFDomainRuntime.Instance;
            if (stage == 0)
            {
                if (!runtime.HasAvailable(Definition) || !runtime.HasTerrainTemplate(Definition)) return;
                source = Find.CurrentMap;
                Pawn a = PawnGenerator.GeneratePawn(PawnKindDefOf.Colonist, Faction.OfPlayer);
                Pawn b = PawnGenerator.GeneratePawn(PawnKindDefOf.Colonist, Faction.OfPlayer);
                IntVec3 origin = source.mapPawns.FreeColonistsSpawned.First().Position;
                GenSpawn.Spawn(a, source.GetComponent<MapComponent_Domain>().FindFreeCell(source, origin, true), source);
                GenSpawn.Spawn(b, source.GetComponent<MapComponent_Domain>().FindFreeCell(source, a.Position + new IntVec3(8, 0, 0), true), source);
                mapCount = runtime.DomainMaps.Count;
                first = runtime.EnterOrContest(Definition, a, new[] { b });
                second = runtime.EnterOrContest(Definition, b, Array.Empty<Pawn>());
                Check(Definition.surface == null && first.Worker!.SupportsContests, "terrain and building domain joins a contest without a surface effect");
                Check(first.Layout?.Structures.Count == 5 && second.Layout?.Structures.Count == 5,
                    "both instances receive their own pre-generated building layout");
                Check(first.map.AllCells.All(cell => cell.GetTerrain(first.map) == TerrainDefOf.PavedTile), "plain domain retains its actual vanilla terrain");
                DomainStructure left = At(first, 14);
                Check(left.Thing!.Spawned && !At(second, 14).Thing!.Spawned && !At(first, 48).Thing!.Spawned && At(second, 48).Thing!.Spawned,
                    "each side exposes only buildings fully inside its territory");
                left.Thing.TryGetComp<CompQuality>().SetQuality(QualityCategory.Excellent, ArtGenerationContext.Outsider);
                left.Thing.HitPoints = left.Thing.MaxHitPoints - 9;
                Current.Game.GetComponent<GameComponent_ContestChecks>().RecordStructure(left.Thing);
                a.DeSpawn();
                Check(((Building_Casket)At(first, 16).Thing!).TryAcceptThing(a), "template container accepts a real domain participant");
                first.DrainHealth(first.Health * 0.8f);
                Find.TickManager.CurTimeSpeed = TimeSpeed.Normal;
                Current.Game.CurrentMap = first.map;
                frames = 0;
                stage = 1;
            }
            else if (stage == 1)
            {
                Focus();
                first!.Contest!.Tick(first);
                if (++frames == 3) Shot("PlainDomain_Recoil");
                if (Math.Abs(first.Contest.Partition.FirstFraction - 1f / 6f) > 0.0001f || frames < 20) return;
                DomainStructure left = At(first, 14);
                Check(left.Thing != null && !left.Thing.Spawned && left.GetDirectlyHeldThings().Count == 1,
                    "retreat parks the original building in a real holder instead of recreating it");
                Check(At(second!, 14).Thing!.Spawned, "opposing layout takes over cleared space without overlapping buildings");
                Check(first.Caster!.Spawned && first.Caster.Map == first.map,
                    "retreat releases contained participants before parking their building");
                Check(runtime.DomainMaps.Count == mapCount, "structural counter-expansion creates no additional map");
                Shot("PlainDomain_Battle");
                frames = 0;
                stage = 9;
            }
            else if (stage == 9)
            {
                Focus();
                if (++frames < 3) return;
                Current.Game.GetComponent<GameComponent_ContestChecks>().Record(first.Caster!.thingIDNumber, second!.Caster!.thingIDNumber);
                GameDataSaveLoader.SaveGame("CQFLayoutChecks");
                stage = 99;
                GameDataSaveLoader.LoadGame("CQFLayoutChecks");
            }
            else if (stage == 2)
            {
                GameComponent_ContestChecks saved = Current.Game.GetComponent<GameComponent_ContestChecks>();
                DomainStructure left = At(first!, 14);
                Check(left.Thing!.thingIDNumber == saved.StructureId && left.Thing.HitPoints == saved.StructureHitPoints
                    && left.Thing.TryGetComp<CompQuality>().Quality == QualityCategory.Excellent && !left.Thing.Spawned,
                    "full game reload preserves parked building identity, damage and component state");
                first!.RestoreHealth(first.MaximumHealth);
                frames = 0;
                stage = 3;
            }
            else if (stage == 3)
            {
                Focus();
                first!.Contest!.Tick(first);
                if (Math.Abs(first.Contest.Partition.FirstFraction - 0.5f) > 0.0001f) return;
                DomainStructure left = At(first, 14);
                GameComponent_ContestChecks saved = Current.Game.GetComponent<GameComponent_ContestChecks>();
                Check(left.Thing!.Spawned && left.Thing.thingIDNumber == saved.StructureId && left.Thing.HitPoints == saved.StructureHitPoints,
                    "reclaiming territory restores the same damaged building after reload");
                At(first, 12).Thing!.Destroy();
                first.DrainHealth(first.Health * 0.8f);
                stage = 4;
            }
            else if (stage == 4)
            {
                first!.Contest!.Tick(first);
                if (first.Contest.Partition.FirstFraction > 0.17f) return;
                blocker = ThingMaker.MakeThing(ThingDefOf.Steel);
                GenSpawn.Spawn(blocker, new IntVec3(14, 0, 34), first.map);
                first.RestoreHealth(first.MaximumHealth);
                stage = 5;
            }
            else if (stage == 5)
            {
                first!.Contest!.Tick(first);
                if (first.Contest.Partition.FirstFraction != 0.5f) return;
                Check(!At(first, 14).Thing!.Spawned && blocker!.Spawned && !blocker.Destroyed,
                    "returning layout waits for a blocked footprint without destroying existing items");
                blocker.Destroy();
                first.Contest.RefreshLayouts(first);
                Check(At(first, 14).Thing!.Spawned, "clearing the obstruction safely restores the parked structure");
                Check(At(first, 12).Thing == null || At(first, 12).Thing!.Destroyed,
                    "destroyed structures do not respawn when the battlefront returns");
                containedCaster = first.Caster!;
                containedCaster.DeSpawn();
                Check(((Building_Casket)At(first, 16).Thing!).TryAcceptThing(containedCaster), "return case begins with the caster inside a template container");
                Current.Game.CurrentMap = source;
                first.RequestReturn("layout checks");
                second!.RequestReturn("layout checks");
                stage = 6;
            }
            else if (stage == 6)
            {
                if (!GameComponent_MapManager.Instance!.IsHibernating(first!.map)) return;
                Check(containedCaster is { Spawned: true, Destroyed: false } && containedCaster.Map == source,
                    "recall returns contained participants before clearing the layout");
                Pawn pawn = source!.mapPawns.FreeColonistsSpawned.First();
                MapComponent_Domain reused = runtime.EnterOrContest(Definition, pawn, Array.Empty<Pawn>());
                Check(reused.map == first.map && reused.Layout!.Structures.Count == 5 && At(reused, 12).Thing!.Spawned,
                    "hibernated map rebuilds a fresh complete layout for the next independent use");
                Check(runtime.DomainMaps.Count == mapCount, "layout reset reuses the existing hibernated map");
                Pawn other = PawnGenerator.GeneratePawn(PawnKindDefOf.Colonist, Faction.OfPlayer);
                GenSpawn.Spawn(other, source.GetComponent<MapComponent_Domain>().FindFreeCell(source, reused.SourceOrigin + new IntVec3(8, 0, 0), true), source);
                second = runtime.EnterOrContest(Definition, other, Array.Empty<Pawn>());
                first = reused;
                stage = 7;
            }
            else if (stage == 7)
            {
                if (first!.Challenger == null) return;
                second = first.Challenger;
                Check(At(first, 14).Position == new IntVec3(14, 0, 34) && At(second, 48).Position == new IntVec3(48, 0, 34)
                    && At(first, 14).Thing!.Spawned && At(second, 48).Thing!.Spawned,
                    "overlap merge preserves both template layouts at their exact intended coordinates");
                Current.Game.CurrentMap = source;
                first.RequestReturn("layout checks finished");
                second.RequestReturn("layout checks finished");
                stage = 8;
            }
            else if (stage == 8)
            {
                if (!GameComponent_MapManager.Instance!.IsHibernating(first!.map)) return;
                Finish(true);
            }
        }
        catch (Exception error)
        {
            File.AppendAllText(ResultPath, "FAIL " + error + "\n");
            Log.Error("[CQF] Layout runtime checks failed: " + error);
            Finish(false);
        }
    }

    private DomainStructure At(MapComponent_Domain side, int x)
        => side.Layout!.Structures.Single(structure => structure.Position.x == x);

    private void Focus()
    {
        Current.Game.CurrentMap = first!.map;
        Find.CameraDriver.SetRootPosAndSize(first.map.Center.ToVector3Shifted(), 18f);
    }

    private void Check(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
        File.AppendAllText(ResultPath, "PASS " + message + "\n");
    }

    private void Shot(string name) => ScreenCapture.CaptureScreenshot(Path.Combine(GenFilePaths.SaveDataFolderPath, name + ".png"));

    private void Finish(bool success)
    {
        finished = true;
        File.AppendAllText(ResultPath, success ? "COMPLETE\n" : "FAILED\n");
        Application.Quit(success ? 0 : 1);
    }

    private Map? source;
    private MapComponent_Domain? first;
    private MapComponent_Domain? second;
    private Thing? blocker;
    private Pawn? containedCaster;
    private int stage;
    private int frames;
    private int mapCount;
    private bool finished;
}
