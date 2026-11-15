using System.Collections;
using System.Reflection;
using QuestEditor_Library;
using RimWorld;
using UnityEngine;
using Verse;

namespace CQF.OptimizationChecks;

public sealed partial class OptimizationDriver
{
    private void CheckEventAreas()
    {
        Map map = Find.CurrentMap;
        MapComponent_CustomMapData component = map.GetComponent<MapComponent_CustomMapData>();
        Pawn pawn = map.mapPawns.FreeColonistsSpawned.First();
        Faction original = pawn.Faction;
        Faction other = Find.FactionManager.AllFactionsListForReading.First(faction => faction != original);
        IntVec3 position = pawn.Position;
        CQFEventArea area = new() { key = "CQF_Check_FactionArea", faction = original.def.defName, cells = new() { position } };
        component.AddOrReplaceEventArea(area);
        pawn.SetFaction(other);
        Check(!area.TryTrigger(map, null!), "an event area cannot trigger for a pawn whose faction stopped matching");
        pawn.SetFaction(original);
        Check(area.TryTrigger(map, null!) && !component.EventAreas.Contains(area), "an event area triggers after a pawn changes into the required faction");
        area = new CQFEventArea { key = "CQF_Check_FactionAreaLate", faction = other.def.defName, cells = new() { position } };
        component.AddOrReplaceEventArea(area);
        Check(!area.TryTrigger(map, null!), "initially ineligible pawns do not trigger the event area");
        pawn.SetFaction(other);
        Check(area.TryTrigger(map, null!), "initially ineligible cached pawns can trigger after faction conversion without respawning");
        pawn.SetFaction(original);
        area = new CQFEventArea { key = "CQF_Check_AllocationArea", cells = new() };
        component.AddOrReplaceEventArea(area);
        for (int i = 0; i < 100; i++) area.TryTrigger(map, null!);
        long before = GC.GetAllocatedBytesForCurrentThread();
        for (int i = 0; i < 512; i++) area.TryTrigger(map, null!);
        long allocated = GC.GetAllocatedBytesForCurrentThread() - before;
        Check(allocated == 0, "512 warmed event-area polls allocate zero managed bytes");
        component.EventAreas.Remove(area);
    }

    private void CheckDutyRuntime()
    {
        Pawn pawn = Find.CurrentMap.mapPawns.FreeColonistsSpawned.First();
        GameComponent_ComplexDuty component = GameComponent_ComplexDuty.Instance;
        component.Remove(pawn);
        int count = ((IDictionary)Field(component, "runtimes")!).Count;
        component.NotifyPawnDamaged(pawn, default);
        Check(component.TryGetRuntime(pawn) == null && ((IDictionary)Field(component, "runtimes")!).Count == count, "damage notifications do not create duty runtimes for ordinary pawns");
        CustomDutyMap runtime = component.GetRuntime(pawn);
        runtime.RecordTarget("CQF_Check_Target", pawn);
        component.NotifyPawnDamaged(pawn, default);
        Check(ReferenceEquals(runtime, component.TryGetRuntime(pawn)) && runtime.GetTarget("CQF_Check_Target").Thing == pawn, "damage notifications preserve existing runtime database contents");
        component.Remove(pawn);
    }

    private void CheckQuestBookDraft()
    {
        QuestBookDef source = new() { defName = "CQF_Check_Draft", chapters = new() { new QuestBookChapter { id = "CQF_Check_Chapter", labelKey = "CQF_QuestBook_Chapter", steps = new() { new QuestBookStep { id = "CQF_Check_Step" } } } } };
        QuestEditor_QuestBook editor = new();
        MethodInfo copy = typeof(QuestEditor_QuestBook).GetMethod("CopyDefinition", BindingFlags.NonPublic | BindingFlags.Instance)!;
        QuestBookDef draft = (QuestBookDef)copy.Invoke(editor, new object[] { source })!;
        typeof(QuestEditor_QuestBook).GetMethod("PrepareEditableText", BindingFlags.NonPublic | BindingFlags.Static)!.Invoke(null, new object[] { draft });
        draft.chapters[0].steps[0].id = "CQF_Check_DraftStep";
        Check(source.chapters[0].labelKey == "CQF_QuestBook_Chapter" && source.chapters[0].steps[0].id == "CQF_Check_Step", "editing and translating a task book draft preserve the loaded definition and translation keys");
        QuestBookDef published = (QuestBookDef)copy.Invoke(editor, new object[] { draft })!;
        draft.chapters[0].steps.Clear();
        Check(published.chapters[0].steps.Count == 1, "a published task book snapshot remains isolated from subsequent draft edits");
    }

    private void CheckLighting()
    {
        Map map = Find.CurrentMap;
        MapComponent_CQFSceneLighting lighting = map.GetComponent<MapComponent_CQFSceneLighting>();
        MethodInfo update = typeof(MapComponent_CQFSceneLighting).GetMethod("UpdateOccluders", BindingFlags.NonPublic | BindingFlags.Instance)!;
        lighting.Configure(new[] { new CQFSceneLight { position = map.Center.ToVector3(), castsShadows = true } });
        update.Invoke(lighting, null);
        List<Bounds> boxes = (List<Bounds>)Field(lighting, "occluders")!;
        Bounds sentinel = new(new Vector3(-10f, -10f, -10f), Vector3.one);
        boxes.Add(sentinel);
        update.Invoke(lighting, null);
        Check(boxes.Contains(sentinel), "unchanged scene occluders are reused without a new map scan");
        lighting.SetOccluders(new[] { new Bounds(map.Center.ToVector3(), Vector3.one) });
        update.Invoke(lighting, null);
        Check(!boxes.Contains(sentinel), "changing scene occluders invalidates and rebuilds the cached geometry");
        map.events.Notify_DoorOpened(null!);
        Check((bool)Field(lighting, "occludersDirty")!, "door events invalidate cached occluder geometry");
        lighting.Dispose();
    }
}
