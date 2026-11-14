using QuestEditor_Library;
using RimWorld;
using UnityEngine;
using Verse;

namespace CQF.DomainPerformanceChecks;

public sealed partial class DomainPerformanceDriver
{
    private static IEnumerable<object?> Gameplay()
    {
        DomainDef definition = DefDatabase<DomainDef>.GetNamed("CQF_TestDomain");
        DomainVisualDef? visuals = definition.visuals;
        definition.visuals = null;
        try
        {
            Map source = Find.CurrentMap;
            int sourceId = source.uniqueID;
            Pawn[] actors = new Pawn[8];
            IntVec3[] origins = new IntVec3[8];
            for (int index = 0; index < actors.Length; index++)
            {
                actors[index] = PawnGenerator.GeneratePawn(new PawnGenerationRequest(PawnKindDefOf.Colonist, Faction.OfPlayer, mustBeCapableOfViolence: true));
                IntVec3 cell = source.GetComponent<MapComponent_Domain>().FindFreeCell(source, source.Center + new IntVec3(index * 2, 0, 0), true);
                GenSpawn.Spawn(actors[index], cell, source);
                actors[index].drafter.Drafted = true;
                actors[index].drafter.FireAtWill = false;
                origins[index] = cell;
            }
            CQFDomainRuntime runtime = CQFDomainRuntime.Instance;
            MapComponent_Domain field = runtime.EnterOrContest(definition, actors[0], actors.Skip(1));
            for (int index = 1; index < actors.Length; index++)
            {
                field = runtime.EnterOrContest(definition, actors[index], Array.Empty<Pawn>()).Battlefield;
                Check(field.InstanceCount == index + 1 && field.ActiveDomainCount == index + 1, "native domain join retains every caster: " + (index + 1));
            }
            Check(field.Contest!.Partition.DomainCount == 8 && field.map.Size == new IntVec3(180, 1, 180), "native eight-domain battlefield has the measured dimensions");
            Current.Game.CurrentMap = field.map;
            Find.CameraDriver.SetRootPosAndSize(field.map.Center.ToVector3Shifted(), 85f);
            for (int i = 0; i < 6; i++) yield return null;
            ValidateLive(field);
            ScreenCapture.CaptureScreenshot(Path.Combine(Bootstrap.Output, "02-EightDomains.png"));
            for (int i = 0; i < 3; i++) yield return null;
            DomainTerritoryPartition partition = field.Contest.Partition;
            IntVec3 protectedCell = field.map.AllCells.First(cell => partition.OwnerIndex(cell) == 0);
            int protection = field.Instances.ElementAt(0).ProtectTerritory(new[] { protectedCell }, 600);
            field.Instances.ElementAt(7).DrainHealth(field.Instances.ElementAt(7).Health - 1000f);
            field.Contest.ResolveSurvivor(field);
            Check(partition.OwnerIndex(protectedCell) == 0 && partition.ProtectionAt(protectedCell.x, protectedCell.z) == 1,
                "native protection remains bound to its owner after a health-driven solve");
            ValidateLive(field);
            for (int i = 0; i < 4; i++) yield return null;
            ScreenCapture.CaptureScreenshot(Path.Combine(Bootstrap.Output, "03-WeakDomain.png"));
            for (int i = 0; i < 3; i++) yield return null;
            foreach (MapComponent_Domain side in field.Instances) side.RestoreHealth(side.MaximumHealth);
            field.Contest.ResolveSurvivor(field);
            Check(field.Instances.ElementAt(0).RemoveTerritoryProtection(protection), "native protection can be removed after repeated cached draws");
            field.Contest.ResolveSurvivor(field);
            string expected = StateHash(partition), mesh = GeometryHash(partition, new DomainTerritoryGeometry(partition));
            int fieldId = field.map.uniqueID;
            Game oldGame = Current.Game;
            GameDataSaveLoader.SaveGame("CQF54DomainGameplay");
            GameDataSaveLoader.LoadGame("CQF54DomainGameplay");
            yield return null;
            Find.TickManager.Pause();
            Check(Current.Game != oldGame, "native active-domain game reload completes");
            source = Current.Game.Maps.Single(map => map.uniqueID == sourceId);
            field = Current.Game.Maps.Single(map => map.uniqueID == fieldId).GetComponent<MapComponent_Domain>();
            actors = field.Instances.Select(side => side.Caster!).ToArray();
            partition = field.Contest!.Partition;
            Check(field.ActiveDomainCount == 8 && StateHash(partition) == expected && GeometryHash(partition, new DomainTerritoryGeometry(partition)) == mesh,
                "native reload rebuilds transient caches with identical territory and geometry");
            ValidateLive(field);
            for (int i = 0; i < 5; i++) yield return null;
            ScreenCapture.CaptureScreenshot(Path.Combine(Bootstrap.Output, "04-ReloadedEightDomains.png"));
            for (int i = 0; i < 3; i++) yield return null;
            field.Instances.ElementAt(0).Collapse("CQF performance regression");
            float deadline = Time.realtimeSinceStartup + 90f;
            while (field.ActiveDomainCount != 7)
            {
                if (Time.realtimeSinceStartup >= deadline) throw new TimeoutException("Native domain collapse did not complete.");
                yield return null;
            }
            field.Contest.ResolveSurvivor(field);
            ValidateLive(field);
            foreach (MapComponent_Domain side in field.Instances.ToList()) side.RequestReturn("CQF performance cleanup");
            while (runtime.Transition != null || actors.Any(actor => actor.Map != source))
            {
                runtime = CQFDomainRuntime.Instance;
                if (Time.realtimeSinceStartup >= deadline) throw new TimeoutException("Native domain return did not complete.");
                yield return null;
            }
            Check(actors.Select((actor, index) => actor.Position == origins[index]).All(value => value), "all native casters return to their exact original cells");
            foreach (Pawn actor in actors) actor.Destroy();
        }
        finally { definition.visuals = visuals; }
    }

    private static void ValidateLive(MapComponent_Domain field)
    {
        DomainTerritoryPartition partition = field.Contest!.Partition;
        Check(field.map.mapPawns.AllPawnsSpawned.Count == 8 && field.Instances.Where(side => side.Active).All(side => side.Caster?.Map == field.map),
            "all native casters remain present during territory changes");
        for (int z = 0; z < partition.Height; z++)
        for (int x = 0; x < partition.Width; x++)
        {
            IntVec3 cell = new(x, 0, z);
            int owner = -1; float best = float.NegativeInfinity;
            for (int side = 0; side < partition.DomainCount; side++)
                if (partition.FractionFor(side) > 0f && partition.ScoreFor(side, x, z) > best)
                { best = partition.ScoreFor(side, x, z); owner = side; }
            bool boundary = false;
            foreach ((int dx, int dz) in new[] { (1, 0), (0, 1), (1, 1) })
            {
                int next = -1; float score = float.NegativeInfinity;
                for (int side = 0; side < partition.DomainCount; side++)
                    if (partition.FractionFor(side) > 0f && partition.ScoreFor(side, x + dx, z + dz) > score)
                    { score = partition.ScoreFor(side, x + dx, z + dz); next = side; }
                boundary |= next != owner;
            }
            boundary |= partition.OverrideAt(x, z) != 0 || partition.ProtectionAt(x - 1, z) != 0 || partition.ProtectionAt(x + 1, z) != 0
                || partition.ProtectionAt(x, z - 1) != 0 || partition.ProtectionAt(x, z + 1) != 0;
            if (partition.IsBoundaryCell(cell) != boundary) throw new InvalidOperationException("Native cached boundary differs from direct corner scores: " + cell);
        }
        Check(true, "every native boundary matches direct scoring after lifecycle changes");
        DomainTerritoryField[] fields = Enumerable.Range(0, field.InstanceCount).Select(_ => new DomainTerritoryField(partition)).ToArray();
        try { CheckPixels(partition, fields); }
        finally { foreach (DomainTerritoryField item in fields) item.Dispose(); }
    }
}
