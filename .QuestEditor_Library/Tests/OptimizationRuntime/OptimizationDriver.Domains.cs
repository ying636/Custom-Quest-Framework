using System.Reflection;
using System.Runtime.CompilerServices;
using HarmonyLib;
using QuestEditor_Library;
using UnityEngine;
using Verse;

namespace CQF.OptimizationChecks;

public sealed partial class OptimizationDriver
{
    private void CheckDomainTicks()
    {
        Map map = (Map)RuntimeHelpers.GetUninitializedObject(typeof(Map));
        map.info = new MapInfo { Size = new IntVec3(10, 1, 10) };
        MapComponent_Domain first = new(map), second = new(map, true), third = new(map, true);
        map.components = new() { first };
        AccessTools.Field(typeof(MapComponent_Domain), "challenger").SetValue(first, second);
        ((List<MapComponent_Domain>)Field(first, "additionalDomains")!).Add(third);
        foreach (MapComponent_Domain side in new[] { first, second, third })
        {
            side.Configure(new DomainDef { defName = "CQF_Check_Buffer", workerClass = typeof(DomainWorker), maximumHealth = 100000f, contestHealthLossPerSecond = .001f });
            AccessTools.Field(typeof(MapComponent_Domain), "active").SetValue(side, true);
            AccessTools.Field(typeof(MapComponent_Domain), "health").SetValue(side, 100000f);
        }
        DomainContest contest = new();
        Action<MapComponent_Domain> settle = AccessTools.Method(typeof(DomainContest), "SettleHealth").CreateDelegate<Action<MapComponent_Domain>>(contest);
        AccessTools.FieldRef<DomainContest, int> lastTick = AccessTools.FieldRefAccess<DomainContest, int>("settlementTick");
        settle(first);
        float expected = 100000f - 100000f * .001f * 2f / 60f;
        Check(Mathf.Abs(first.Health - expected) < .02f && first.Health == second.Health && second.Health == third.Health, "reused domain snapshots preserve simultaneous three-way attrition");
        float beforeHealth = first.Health;
        settle(first);
        Check(first.Health == beforeHealth, "domain attrition still runs once per game tick");
        AccessTools.Field(typeof(MapComponent_Domain), "arrivalLocked").SetValue(second, true);
        lastTick(contest) = -1;
        settle(first);
        Check(first.Health == beforeHealth, "one arrival-locked domain still suspends shared attrition");
        AccessTools.Field(typeof(MapComponent_Domain), "arrivalLocked").SetValue(second, false);
        for (int i = 0; i < 100; i++) { lastTick(contest) = -1; settle(first); }
        long before = GC.GetAllocatedBytesForCurrentThread();
        for (int i = 0; i < 512; i++) { lastTick(contest) = -1; settle(first); }
        Check(GC.GetAllocatedBytesForCurrentThread() - before == 0, "512 warmed domain attrition ticks allocate zero managed bytes");
        DomainTerritoryPartition partition = new(10, 10, new[] { new IntVec3(2, 0, 2), new IntVec3(7, 0, 2), new IntVec3(5, 0, 7) }, new[] { first.Health, second.Health, third.Health });
        AccessTools.Field(typeof(DomainContest), "partition").SetValue(contest, partition);
        FieldInfo occupations = AccessTools.Field(typeof(DomainContest), "occupations");
        occupations.SetValue(contest, new List<DomainOccupation> { new(), new(), new() });
        Action<MapComponent_Domain> tick = AccessTools.Method(typeof(DomainContest), "TickMultiple").CreateDelegate<Action<MapComponent_Domain>>(contest);
        int revision = partition.Revision;
        tick(first);
        Check(partition.Revision == revision, "three-way tick buffers preserve stable equal-health territory");
        occupations.SetValue(contest, new List<DomainOccupation>());
        bool failed = false;
        try { tick(first); } catch (ArgumentOutOfRangeException) { failed = true; }
        occupations.SetValue(contest, new List<DomainOccupation> { new(), new(), new() });
        tick(first);
        Check(failed && partition.Revision == revision, "an interrupted multilateral tick releases its buffer for the following tick");
    }
}
