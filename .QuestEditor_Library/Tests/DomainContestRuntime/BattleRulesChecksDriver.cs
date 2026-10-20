using QuestEditor_Library;
using RimWorld;
using UnityEngine;
using Verse;

namespace CQF.DomainContestRuntimeChecks;

public sealed class BattleRulesChecksDriver : MonoBehaviour
{
    private string ResultPath => Path.Combine(GenFilePaths.SaveDataFolderPath, "DomainContestChecks.txt");
    private MapComponent_Domain Blood => field!.Challenger!;

    public void Resume(int first, int second)
    {
        Find.TickManager.Pause();
        field = CQFDomainRuntime.Instance.DomainMaps.Select(map => map.GetComponent<MapComponent_Domain>())
            .Single(domain => domain.Caster?.thingIDNumber == first && domain.Challenger?.Caster?.thingIDNumber == second);
        source = field.SourceMap;
        int structureId = Current.Game.GetComponent<GameComponent_ContestChecks>().StructureId;
        persistentBuilding = (Building)field.Layout!.Structures.Single(item => !item.ParkOnTerritoryLoss).Thing!;
        protectedCells = field.Contest!.Partition.Protections.Single().Cells.ToList();
        if (Current.Game.GetComponent<GameComponent_ContestChecks>().BattleRulesPhase == 1)
        {
            Check(field.Contest.Partition.Protections.Single().Source == null,
                "saving immediately after a source building is destroyed does not leave an unresolved destroyed reference");
            stage = 9;
            return;
        }
        building = (Building)field.Layout.Structures.Single(item => item.Thing?.thingIDNumber == structureId).Thing!;
        Check(field.Contest.Partition.Protections.Single().Source == building && protectedCells.All(field.OwnsCell),
            "complete game reload restores the protection's building reference and real territory");
        Check(building.Spawned && building.HitPoints == 137, "complete reload preserves the registered skill building and its damage");
        Check(persistentBuilding.Spawned && !field.OwnsCell(persistentBuilding.Position),
            "complete reload preserves a registered building configured to remain when its territory is lost");
        stage = 3;
    }

    private void Update()
    {
        if (finished || stage == 99 || LongEventHandler.AnyEventNowOrWaiting) return;
        try
        {
            if (Time.realtimeSinceStartup > 250f) throw new TimeoutException("Battle rules stage " + stage);
            if (stage == 0)
            {
                if (Find.CurrentMap == null) return;
                CQFDomainRuntime runtime = CQFDomainRuntime.Instance;
                DomainDef basic = DefDatabase<DomainDef>.GetNamed("CQF_TestDomain");
                DomainDef blood = DefDatabase<DomainDef>.GetNamed("RSH_BloodSeaDomain");
                if (!runtime.HasAvailable(basic) || !runtime.HasAvailable(blood)
                    || !runtime.HasTerrainTemplate(basic) || !runtime.HasTerrainTemplate(blood)) return;
                Find.TickManager.Pause();
                source = Find.CurrentMap;
                Pawn a = PawnGenerator.GeneratePawn(PawnKindDefOf.Colonist, Faction.OfPlayer);
                Pawn b = PawnGenerator.GeneratePawn(PawnKindDefOf.Colonist, Faction.OfPlayer);
                IntVec3 near = source.mapPawns.FreeColonistsSpawned.First().Position;
                GenSpawn.Spawn(a, source.GetComponent<MapComponent_Domain>().FindFreeCell(source, near, true), source);
                GenSpawn.Spawn(b, source.GetComponent<MapComponent_Domain>().FindFreeCell(source, near + new IntVec3(8, 0, 0), true), source);
                a.abilities ??= new Pawn_AbilityTracker(a); b.abilities ??= new Pawn_AbilityTracker(b);
                field = runtime.EnterOrContest(basic, a, new[] { b });
                runtime.EnterOrContest(blood, b, Array.Empty<Pawn>());
                Current.Game.CurrentMap = field.map;
                stage = 1;
            }
            else if (stage == 1)
            {
                if (!field!.Ready || !Blood.Ready || field.ArrivalLocked || Blood.ArrivalLocked) return;
                CheckConsumption();
                CheckDamageReduction();
                CheckSkills();
                PrepareProtection();
                frames = 0;
                stage = 2;
            }
            else if (stage == 2)
            {
                if (++frames < 45) return;
                ScreenCapture.CaptureScreenshot(Path.Combine(GenFilePaths.SaveDataFolderPath, "BattleRules_ProtectedTerritory.png"));
                capturedAt = Time.realtimeSinceStartup;
                stage = 8;
            }
            else if (stage == 8)
            {
                if (Time.realtimeSinceStartup - capturedAt < 0.3f
                    || !File.Exists(Path.Combine(GenFilePaths.SaveDataFolderPath, "BattleRules_ProtectedTerritory.png"))) return;
                if (GenCommandLine.CommandLineArgPassed("cqfcontestloadchecks"))
                {
                    GameComponent_ContestChecks saved = Current.Game.GetComponent<GameComponent_ContestChecks>();
                    saved.Record(field!.Caster!.thingIDNumber, Blood.Caster!.thingIDNumber);
                    saved.RecordStructure(building!);
                    GameDataSaveLoader.SaveGame("CQFBattleRulesChecks");
                    stage = 99;
                    GameDataSaveLoader.LoadGame("CQFBattleRulesChecks");
                }
                else stage = 3;
            }
            else if (stage == 3)
            {
                int overlapping = field!.ProtectTerritory(protectedCells.Take(3), 2);
                for (int tick = 0; tick < 3; tick++) Find.TickManager.DoSingleTick();
                Check(field.Contest!.Partition.Protections.Count == 1 && protectedCells.All(field.OwnsCell),
                    "timed protection expires on native game ticks without releasing overlapping protection");
                Check(!field.RemoveTerritoryProtection(overlapping), "expired protection cannot be released twice");
                building!.Destroy();
                if (GenCommandLine.CommandLineArgPassed("cqfcontestloadchecks"))
                {
                    Current.Game.GetComponent<GameComponent_ContestChecks>().BattleRulesPhase = 1;
                    GameDataSaveLoader.SaveGame("CQFBattleRulesDestroyedSource");
                    stage = 99;
                    GameDataSaveLoader.LoadGame("CQFBattleRulesDestroyedSource");
                    return;
                }
                stage = 9;
            }
            else if (stage == 9)
            {
                for (int tick = 0; tick < 180; tick++) field!.Contest!.Tick(field);
                Check(!field!.Contest!.Partition.HasProtections && protectedCells.Any(cell => !field.OwnsCell(cell)),
                    "destroying the source removes protection and permits actual enemy occupation");
                Check(field.Layout!.Structures.Where(item => item.ParkOnTerritoryLoss).All(item => item.Thing == null || item.Thing.Destroyed),
                    "a destroyed registered skill building is never recreated by territory changes");
                field.RequestReturn("battle rule checks cancellation");
                stage = 4;
            }
            else if (stage == 4)
            {
                if (!field!.Trapped) return;
                float health = Blood.Health;
                for (int tick = 0; tick < 60; tick++) Find.TickManager.DoSingleTick();
                Check(Blood.Health == health && field.Caster!.MapHeld == Blood.map,
                    "natural contest consumption stops after cancellation while the losing caster remains trapped");
                Check(persistentBuilding!.Destroyed, "domain dissolution also cleans registered structures that remained across the boundary");
                Blood.RequestReturn("battle rule checks completion");
                stage = 5;
            }
            else if (stage == 5)
            {
                if (!GameComponent_MapManager.Instance!.IsHibernating(field!.map)) return;
                Check(field.Caster == null && source!.mapPawns.AllPawnsSpawned.Count >= 2,
                    "final return cleans battle rules and recycles the battlefield");
                PrepareSimultaneousDepletion();
                stage = 6;
            }
            else if (stage == 6)
            {
                if (field!.ArrivalLocked || field.Challenger!.ArrivalLocked) return;
                float loss = field.MaximumHealth * field.Definition.contestHealthLossPerSecond / 60f;
                float secondLoss = field.Challenger.MaximumHealth * field.Challenger.Definition.contestHealthLossPerSecond / 60f;
                field.DrainHealth(field.Health - loss * 0.5f);
                field.Challenger.DrainHealth(field.Challenger.Health - secondLoss * 0.5f);
                Find.TickManager.DoSingleTick();
                Check(field.Health == 0f && field.Challenger.Health == 0f && !field.Ready && !field.Challenger.Ready,
                    "both domains exhaust in the same native settlement without a first-processed winner");
                stage = 7;
            }
            else if (stage == 7)
            {
                if (!GameComponent_MapManager.Instance!.IsHibernating(field!.map)) return;
                Check(field.Challenger == null && field.Available, "simultaneous natural depletion returns both groups and hibernates cleanly");
                Finish(true);
            }
        }
        catch (Exception error)
        {
            File.AppendAllText(ResultPath, "FAIL " + error + "\n");
            Log.Error("[CQF] Battle rules runtime checks failed: " + error);
            Finish(false);
        }
    }

    private void CheckConsumption()
    {
        float first = field!.Health, second = Blood.Health;
        for (int tick = 0; tick < 60; tick++) Find.TickManager.DoSingleTick();
        Check(Math.Abs(first - field.Health - field.MaximumHealth * 0.005f) < 1f
            && Math.Abs(second - Blood.Health - Blood.MaximumHealth * 0.005f) < 1f,
            "both domains lose 0.5 percent maximum health per native game second");
        Check(field.Anchor!.HitPoints == Mathf.CeilToInt(field.Health) && Blood.Anchor!.HitPoints == Mathf.CeilToInt(Blood.Health),
            "natural consumption synchronizes both external anchors");
        Check(field.Contest!.RecoilAge >= 100f, "natural consumption does not produce a damage recoil animation");
        first = field.Health; second = Blood.Health;
        field.Contest.Tick(field); field.Contest.Tick(field);
        Check(field.Health == first && Blood.Health == second, "repeated evaluation during one game tick cannot charge consumption twice");
        field.BeginArrivalTransition();
        for (int tick = 0; tick < 4; tick++) Find.TickManager.DoSingleTick();
        Check(field.Health == first && Blood.Health == second, "one side's arrival lock suspends consumption for both sides");
        field.EndArrivalTransition();
    }

    private void CheckDamageReduction()
    {
        Pawn caster = field!.Caster!;
        IntVec3 own = FreeCell(field, true);
        caster.Position = own; caster.Notify_Teleported();
        float before = field.Health;
        DamageInfo damage = new(DamageDefOf.Blunt, 100f);
        Comp_DomainProtection.Ensure(caster).PostPreApplyDamage(ref damage, out bool absorbed);
        Check(absorbed && Math.Abs(before - field.Health - 70f) < 0.01f, "native caster component applies 30 percent home-territory reduction before shared health");
        caster.Position = FreeCell(field, false); caster.Notify_Teleported();
        before = field.Health; damage = new DamageInfo(DamageDefOf.Blunt, 100f);
        Comp_DomainProtection.Ensure(caster).PostPreApplyDamage(ref damage, out absorbed);
        Check(absorbed && Math.Abs(before - field.Health - 100f) < 0.01f, "the caster receives no home reduction inside opposing territory");
        caster.Position = own; caster.Notify_Teleported();
    }

    private void CheckSkills()
    {
        IntVec3 own = FreeCell(Blood, true), enemy = FreeCell(Blood, false);
        var method = Blood.Worker!.GetType().GetMethod("SkillStrengthAt")!;
        Check((float)method.Invoke(Blood.Worker, new object[] { own })! == 1f
            && Math.Abs((float)method.Invoke(Blood.Worker, new object[] { enemy })! - 0.7f) < 0.001f,
            "the blood sea mod implements full own-territory strength and 70 percent opposing-territory strength");
        Ability ability = Blood.Caster!.abilities.GetAbility(DefDatabase<AbilityDef>.GetNamed("RSH_BloodSeaTentacles"));
        CompAbilityEffect effect = ability.comps.OfType<CompAbilityEffect>().Single(comp => comp.GetType().Name == "CompAbilityEffect_BloodSeaTentacles");
        Check(effect.Valid(new LocalTargetInfo(enemy), false), "an actual blood sea ability accepts an opposing-territory target");
        Pawn target = PawnGenerator.GeneratePawn(PawnKindDefOf.Colonist, Faction.OfAncientsHostile);
        GenSpawn.Spawn(target, enemy, field!.map);
        effect.Apply(new LocalTargetInfo(enemy), LocalTargetInfo.Invalid);
        Hediff binding = target.health.hediffSet.hediffs.Single(item => item.GetType().Name == "Hediff_BloodSeaTentacles");
        Check((int)binding.GetType().GetProperty("TicksRemaining")!.GetValue(binding)! == 336,
            "actual enemy-territory tentacles apply a reduced control duration rather than rejecting the cast");
        target.health.RemoveHediff(binding);
        target.Destroy();
        Building wall = (Building)ThingMaker.MakeThing(ThingDefOf.Wall, ThingDefOf.Steel);
        GenSpawn.Spawn(wall, enemy, field.map);
        wall.HitPoints = 200;
        Thing wave = ThingMaker.MakeThing(DefDatabase<ThingDef>.GetNamed("RSH_CrimsonTideEffect"));
        wave.GetType().GetMethod("Configure")!.Invoke(wave, new object[] { Blood.Caster!, 1, 8f });
        wave.GetType().GetMethod("SetDirection")!.Invoke(wave, new object[] { Vector3.left });
        GenSpawn.Spawn(wave, enemy + new IntVec3(4, 0, 0), field.map);
        for (int tick = 0; tick < 95; tick++) Find.TickManager.DoSingleTick();
        Check(wall.HitPoints == 137, "a real tide crosses enemy territory and applies 63 damage instead of the normal 90");
        wall.Destroy();
        IntVec3 start = Blood.Caster.Position;
        PawnFlyer flyer = PawnFlyer.MakeFlyer(DefDatabase<ThingDef>.GetNamed("RSH_BloodSeaTraversal"), Blood.Caster, enemy, null, null);
        GenSpawn.Spawn(flyer, start, field.map);
        int duration = (int)typeof(PawnFlyer).GetField("ticksFlightTime", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!.GetValue(flyer)!;
        int normal = Mathf.Max(Mathf.Max(start.DistanceTo(enemy), 1f) / flyer.def.pawnFlyer.flightSpeed, flyer.def.pawnFlyer.flightDurationMin).SecondsToTicks();
        Check(duration == Mathf.CeilToInt(normal / 0.7f), "cross-territory traversal retains its destination and runs at the blood sea mod's reduced speed");
        for (int tick = 0; tick <= duration + 1; tick++) Find.TickManager.DoSingleTick();
        Check(Blood.Caster.Spawned && Blood.Caster.Map == field.map && !Blood.OwnsCell(Blood.Caster.Position),
            "reduced traversal lands safely in opposing territory without dissolving its caster's domain");
    }

    private void PrepareProtection()
    {
        IntVec3 center = new(field!.Contest!.Partition.Reversed ? 36 : 23, 0, 30);
        protectedCells = CellRect.CenteredOn(center, 2).Where(field.OwnsCell).ToList();
        Check(protectedCells.Count == 25, "protection test starts inside the first domain's actual territory");
        building = (Building)ThingMaker.MakeThing(ThingDefOf.Wall, ThingDefOf.Steel);
        GenSpawn.Spawn(building, center, field.map);
        building.HitPoints = 137;
        field.RegisterStructure(building);
        persistentBuilding = (Building)ThingMaker.MakeThing(ThingDefOf.Wall, ThingDefOf.Steel);
        GenSpawn.Spawn(persistentBuilding, center + new IntVec3(0, 0, 8), field.map);
        field.RegisterStructure(persistentBuilding, false);
        int protection = field.ProtectTerritory(protectedCells, 10000, building);
        Check(protection > 0 && field.Layout!.Structures.Any(item => item.Thing == building), "skill buildings and building-sourced protection register with the owning domain");
        field.DrainHealth(field.MaximumHealth * 0.85f);
        for (int tick = 0; tick < 180; tick++) field.Contest.Tick(field);
        Check(protectedCells.All(field.OwnsCell) && building.Spawned && building.HitPoints == 137,
            "protected terrain and its original damaged building survive strong enemy territorial pressure");
        Check(protectedCells.All(cell => cell.GetTerrain(field.map) == field.Contest.TerrainAt(field, cell)),
            "protected cells keep real terrain consistent with their rule ownership");
        Check(persistentBuilding.Spawned && !field.OwnsCell(persistentBuilding.Position),
            "a registered skill building may retain its physical presence across lost territory");
        Find.CameraDriver.SetRootPosAndSize(center.ToVector3Shifted(), 22f);
    }

    private void PrepareSimultaneousDepletion()
    {
        Current.Game.CurrentMap = source;
        Pawn a = source!.mapPawns.FreeColonistsSpawned.First();
        Pawn b = source.mapPawns.FreeColonistsSpawned.First(pawn => pawn != a);
        DomainDef basic = DefDatabase<DomainDef>.GetNamed("CQF_TestDomain");
        field = CQFDomainRuntime.Instance.EnterOrContest(basic, a, new[] { b });
        CQFDomainRuntime.Instance.EnterOrContest(basic, b, Array.Empty<Pawn>());
        Current.Game.CurrentMap = field.map;
    }

    private IntVec3 FreeCell(MapComponent_Domain side, bool owned) => side.map.AllCells.First(cell => side.OwnsCell(cell) == owned
        && cell.Standable(side.map) && !cell.Fogged(side.map) && !cell.GetThingList(side.map).Any(thing => thing is not Filth)
        && cell.x > 5 && cell.z > 5 && cell.x < side.map.Size.x - 6 && cell.z < side.map.Size.z - 6);

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
    private Building? building;
    private Building? persistentBuilding;
    private List<IntVec3> protectedCells = new();
    private int stage;
    private int frames;
    private float capturedAt;
    private bool finished;
}
