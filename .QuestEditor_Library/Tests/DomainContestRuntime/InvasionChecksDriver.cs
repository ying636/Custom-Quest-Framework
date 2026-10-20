using QuestEditor_Library;
using RimWorld;
using UnityEngine;
using Verse;

namespace CQF.DomainContestRuntimeChecks;

public sealed class InvasionChecksDriver : MonoBehaviour
{
    private string ResultPath => Path.Combine(GenFilePaths.SaveDataFolderPath, "DomainContestChecks.txt");
    private MapComponent_Domain Strong => caseIndex % 2 == 0 ? field!.Challenger! : field!;
    private MapComponent_Domain Weak => caseIndex % 2 == 0 ? field! : field!.Challenger!;

    public void Resume(int firstId, int secondId, int savedCase)
    {
        Find.TickManager.Pause();
        caseIndex = savedCase;
        field = CQFDomainRuntime.Instance.DomainMaps.Select(map => map.GetComponent<MapComponent_Domain>())
            .Single(side => side.Caster?.thingIDNumber == firstId && side.Challenger?.Caster?.thingIDNumber == secondId);
        source = field.SourceMap;
        GameComponent_ContestChecks saved = Current.Game.GetComponent<GameComponent_ContestChecks>();
        weakProbe = Weak.Layout!.Structures.Single(item => item.Thing?.thingIDNumber == saved.StructureId);
        probeCell = weakProbe.Position;
        strongProbe = Strong.Layout!.Structures.Single(item => item.Position == probeCell);
        Check(Weak.Trapped == false && field.Contest!.Partition.Invasion!.Strength > 0f && Strong.OwnsCell(probeCell),
            "complete game reload retains actual invasion ownership and active domain identities");
        Check(weakProbe.Thing?.Spawned == false && strongProbe.Thing?.Spawned == true,
            "complete reload retains parked weak structures and the invader's deployed structure");
        stage = 3;
    }

    private void Update()
    {
        if (finished || stage == 99 || LongEventHandler.AnyEventNowOrWaiting) return;
        try
        {
            if (Time.realtimeSinceStartup > 250f) throw new TimeoutException("Invasion checks case " + caseIndex + ", stage " + stage);
            if (stage == 0)
            {
                if (Find.CurrentMap == null) return;
                DomainDef basic = DefDatabase<DomainDef>.GetNamed("CQF_TestDomain");
                DomainDef blood = DefDatabase<DomainDef>.GetNamed("RSH_BloodSeaDomain");
                DomainDef plain = DefDatabase<DomainDef>.GetNamed("CQF_CheckPlainDomain");
                DomainDef surface = DefDatabase<DomainDef>.GetNamed("CQF_CheckSurfaceDomain");
                DomainDef first = caseIndex >= 5 ? surface : caseIndex >= 2 ? plain : basic;
                DomainDef second = caseIndex == 4 ? plain : blood;
                CQFDomainRuntime runtime = CQFDomainRuntime.Instance;
                if (!runtime.HasAvailable(first) || !runtime.HasAvailable(second) || !runtime.HasTerrainTemplate(first)
                    || !runtime.HasTerrainTemplate(second)) return;
                Check(DefDatabase<TerrainDef>.GetNamed("RSH_BloodSeaFloor").edgeType == TerrainDef.TerrainEdgeType.Hard,
                    "hidden blood sea terrain delegates all boundaries to the curved domain renderer");
                Find.TickManager.Pause();
                source = Find.CurrentMap;
                IntVec3 near = source.mapPawns.FreeColonistsSpawned.First().Position;
                Pawn a = PawnGenerator.GeneratePawn(PawnKindDefOf.Colonist, Faction.OfPlayer);
                Pawn b = PawnGenerator.GeneratePawn(PawnKindDefOf.Colonist, Faction.OfPlayer);
                GenSpawn.Spawn(a, source.GetComponent<MapComponent_Domain>().FindFreeCell(source, near, true), source);
                GenSpawn.Spawn(b, source.GetComponent<MapComponent_Domain>().FindFreeCell(source, near + new IntVec3(8, 0, 0), true), source);
                a.abilities ??= new Pawn_AbilityTracker(a);
                b.abilities ??= new Pawn_AbilityTracker(b);
                field = runtime.EnterOrContest(first, a, new[] { b });
                runtime.EnterOrContest(second, b, Array.Empty<Pawn>());
                Weak.DrainHealth(Weak.MaximumHealth * 0.75f);
                Current.Game.CurrentMap = field.map;
                Find.CameraDriver.SetRootPosAndSize(field.map.Center.ToVector3Shifted(), 34f);
                ticks = 0;
                stage = 1;
            }
            else if (stage == 1)
            {
                for (int index = 0; index < 6; index++) { field!.Contest!.Tick(field); ticks++; }
                if (ticks < 180 || !field!.Ready || !field.Challenger!.Ready) return;
                CheckTerritory();
                Vector4 shape = field.Contest!.Partition.Invasion!.Shapes[1];
                probeCell = field.map.AllCells.Where(cell => Strong.OwnsCell(cell) && cell.Standable(field.map)
                    && !cell.GetThingList(field.map).Any(thing => thing is not Filth)
                    && Math.Abs(cell.x + 0.5f - shape.x) < shape.z * 0.6f && Math.Abs(cell.z + 0.5f - shape.y) < shape.w * 0.6f)
                    .OrderBy(cell => (cell.ToVector3Shifted() - new Vector3(shape.x, 0f, shape.y)).sqrMagnitude).First();
                weakProbe = AddProbe(Weak, 137);
                strongProbe = AddProbe(Strong, 211);
                field.Contest.RefreshLayouts(field);
                Check(weakProbe.Thing?.Spawned == false && strongProbe.Thing?.Spawned == true,
                    "invasion parks the weak layout and deploys the strong layout inside an isolated hole");
                CheckBloodTargeting();
                frames = 0;
                stage = 2;
            }
            else if (stage == 2)
            {
                if (++frames < 60) return;
                ScreenCapture.CaptureScreenshot(Path.Combine(GenFilePaths.SaveDataFolderPath, "Invasion_Battle_" + caseIndex + ".png"));
                capturedAt = Time.realtimeSinceStartup;
                stage = 8;
            }
            else if (stage == 8)
            {
                if (Time.realtimeSinceStartup - capturedAt < 0.3f || !File.Exists(Path.Combine(GenFilePaths.SaveDataFolderPath, "Invasion_Battle_" + caseIndex + ".png"))) return;
                if (caseIndex == 0 && GenCommandLine.CommandLineArgPassed("cqfcontestloadchecks"))
                {
                    GameComponent_ContestChecks saved = Current.Game.GetComponent<GameComponent_ContestChecks>();
                    saved.Record(field!.Caster!.thingIDNumber, field.Challenger!.Caster!.thingIDNumber);
                    saved.RecordStructure(weakProbe!.Thing!);
                    saved.InvasionCase = caseIndex;
                    GameDataSaveLoader.SaveGame("CQFInvasionChecks");
                    stage = 99;
                    GameDataSaveLoader.LoadGame("CQFInvasionChecks");
                    return;
                }
                frames = 0;
                stage = 3;
            }
            else if (stage == 3)
            {
                Current.Game.CurrentMap = field!.map;
                Find.CameraDriver.SetRootPosAndSize(probeCell.ToVector3Shifted(), 9f);
                if (++frames < 12) return;
                ScreenCapture.CaptureScreenshot(Path.Combine(GenFilePaths.SaveDataFolderPath, "Invasion_Hole_" + caseIndex + ".png"));
                capturedAt = Time.realtimeSinceStartup;
                stage = 9;
            }
            else if (stage == 9)
            {
                if (Time.realtimeSinceStartup - capturedAt < 0.3f || !File.Exists(Path.Combine(GenFilePaths.SaveDataFolderPath, "Invasion_Hole_" + caseIndex + ".png"))) return;
                Weak.RestoreHealth(Weak.MaximumHealth);
                ticks = 0;
                stage = 4;
            }
            else if (stage == 4)
            {
                for (int index = 0; index < 6; index++) { field!.Contest!.Tick(field); ticks++; }
                if (ticks < 180) return;
                Check(field!.Contest!.Partition.Invasion!.Strength == 0f && Weak.OwnsCell(probeCell),
                    "healing closes actual invaded territory and restores the original owner");
                Check(weakProbe!.Thing!.Spawned && !strongProbe!.Thing!.Spawned && weakProbe.Thing.HitPoints == 137,
                    "healing restores the same damaged weak building while parking the invader's building");
                Weak.DrainHealth(Weak.MaximumHealth * 0.75f);
                ticks = 0;
                stage = 5;
            }
            else if (stage == 5)
            {
                for (int index = 0; index < 6; index++) { field!.Contest!.Tick(field); ticks++; }
                if (ticks < 180) return;
                CheckTerritory();
                Check(!weakProbe!.Thing!.Spawned && strongProbe!.Thing!.Spawned && strongProbe.Thing.HitPoints == 211,
                    "renewed invasion reuses original building instances without healing or duplicating them");
                Weak.RequestReturn("invasion checks weak recall");
                stage = 6;
            }
            else if (stage == 6)
            {
                if (!Weak.Trapped) return;
                Check(field!.map.AllCells.All(cell => Strong.OwnsCell(cell)
                    && cell.GetTerrain(field.map) == field.Contest!.TerrainAt(Strong, cell)),
                    "weak recall traps its occupants and removes all competing terrain and islands immediately");
                Check(field.Contest!.Partition.Invasion!.Strength == 0f, "surviving domain clears invasion state");
                Strong.RequestReturn("invasion checks last recall");
                stage = 7;
            }
            else if (stage == 7)
            {
                if (!GameComponent_MapManager.Instance!.IsHibernating(field!.map)) return;
                Current.Game.CurrentMap = source;
                if (++caseIndex == 7) { Finish(true); return; }
                stage = 0;
            }
        }
        catch (Exception error)
        {
            File.AppendAllText(ResultPath, "FAIL " + error + "\n");
            Log.Error("[CQF] Invasion runtime checks failed: " + error);
            Finish(false);
        }
    }

    private DomainStructure AddProbe(MapComponent_Domain owner, int hitPoints)
    {
        Building building = (Building)ThingMaker.MakeThing(ThingDefOf.Wall, ThingDefOf.Steel);
        building.HitPoints = hitPoints;
        DomainStructure structure = new(building, probeCell, Rot4.North);
        ((List<DomainStructure>)owner.Layout!.Structures).Add(structure);
        return structure;
    }

    private void CheckTerritory()
    {
        DomainContest contest = field!.Contest!;
        Check(contest.Partition.Invasion!.Strength > 0f && contest.Partition.Invasion.Shapes.Count == 3,
            "case " + caseIndex + " creates three invading territories");
        Check(field.map.AllCells.All(cell => cell.GetTerrain(field.map) == contest.TerrainAt(field.DomainAt(cell)!, cell)),
            "all actual terrain cells agree with gameplay ownership including holes");
        foreach (Vector4 shape in contest.Partition.Invasion.Shapes)
        {
            IntVec3 cell = new(Mathf.FloorToInt(shape.x), 0, Mathf.FloorToInt(shape.y));
            Check(Strong.OwnsCell(cell) && !Weak.OwnsCell(cell), "hole center changes domain gameplay ownership");
            SectionLayer layer = field.map.mapDrawer.SectionAt(cell).GetLayer(typeof(SectionLayer_DomainContest));
            layer.Regenerate();
            Check(layer.Visible && layer.subMeshes.Any(mesh => mesh.finalized && mesh.verts.Count > 0),
                "native terrain mesh renders curved invasion boundaries without requiring a surface shader");
        }
    }

    private void CheckBloodTargeting()
    {
        MapComponent_Domain? blood = field!.Definition.defName == "RSH_BloodSeaDomain" ? field
            : field.Challenger!.Definition.defName == "RSH_BloodSeaDomain" ? field.Challenger : null;
        if (blood == null) return;
        Vector4 shape = field.Contest!.Partition.Invasion!.Shapes[1];
        IntVec3 cell = field.map.AllCells.First(cell => cell.Standable(field.map) && !cell.Fogged(field.map)
            && Math.Abs(cell.x + 0.5f - shape.x) < shape.z * 0.8f && Math.Abs(cell.z + 0.5f - shape.y) < shape.w * 0.8f
            && Strong.OwnsCell(cell));
        Ability ability = blood.Caster!.abilities.GetAbility(DefDatabase<AbilityDef>.GetNamed("RSH_BloodSeaTentacles"));
        CompAbilityEffect effect = ability.comps.OfType<CompAbilityEffect>().Single(comp => comp.GetType().Name == "CompAbilityEffect_BloodSeaTentacles");
        Check(effect.Valid(new LocalTargetInfo(cell), false), "blood sea abilities can target either side of the actual invasion boundary");
        float strength = (float)blood.Worker!.GetType().GetMethod("SkillStrengthAt")!.Invoke(blood.Worker, new object[] { cell })!;
        Check(Math.Abs(strength - (ReferenceEquals(blood, Strong) ? 1f : 0.7f)) < 0.001f,
            "blood sea skill weakening is implemented by its own worker and follows actual invasion ownership");
    }

    private void Check(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
        File.AppendAllText(ResultPath, "PASS " + message + "\n");
    }

    private void Finish(bool success)
    {
        finished = true;
        File.AppendAllText(ResultPath, success ? "COMPLETE\n" : "FAILED\n");
        Application.Quit(success ? 0 : 1);
    }

    private MapComponent_Domain? field;
    private Map? source;
    private DomainStructure? weakProbe;
    private DomainStructure? strongProbe;
    private IntVec3 probeCell;
    private int caseIndex;
    private int stage;
    private int ticks;
    private int frames;
    private bool finished;
    private float capturedAt;
}
