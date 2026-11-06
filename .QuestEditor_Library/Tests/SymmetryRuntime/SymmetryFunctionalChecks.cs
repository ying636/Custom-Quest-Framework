using System.Reflection;
using QuestEditor_Library;
using RimWorld;
using Verse;

namespace CQF.SymmetryChecks;

internal sealed class SymmetryFunctionalChecks
{
    public SymmetryFunctionalChecks(Map map, Action<bool, string> check)
    {
        this.map = map;
        this.symmetry = CQFEditorSymmetry.For(map);
        this.check = check;
    }

    public void Run()
    {
        check(!symmetry.enabled && symmetry.AxisX == 80 && symmetry.AxisZ == 80, "new maps start disabled with centered axes");
        Geometry();
        Designator_SpawnThing spawn = new();
        spawn.SelectThing(ThingDefOf.WoodLog, null!);
        IntVec3 logCell = new(10, 0, 10);
        spawn.DesignateSingleCell(logCell);
        check(At(logCell, ThingDefOf.WoodLog).Count == 1 && At(new IntVec3(70, 0, 10), ThingDefOf.WoodLog).Count == 0,
            "disabled symmetry preserves ordinary placement");
        At(logCell, ThingDefOf.WoodLog).Single().Destroy();
        symmetry.enabled = true;
        symmetry.mirrorZ = true;
        symmetry.mirrorZ = false;
        spawn.DesignateSingleCell(new IntVec3(5, 0, 15));
        check(Mirrors(new IntVec3(5, 0, 15), ThingDefOf.WoodLog).Count == 2, "left/right mode places exactly two objects");
        symmetry.mirrorX = false;
        symmetry.mirrorZ = true;
        spawn.DesignateSingleCell(new IntVec3(6, 0, 15));
        check(Mirrors(new IntVec3(6, 0, 15), ThingDefOf.WoodLog).Count == 2, "top/bottom mode places exactly two objects");
        symmetry.mirrorX = true;
        spawn.DesignateSingleCell(logCell);
        List<Thing> logs = Mirrors(logCell, ThingDefOf.WoodLog);
        check(logs.Count == 4, "dual axes place four objects");
        spawn.DesignateMultiCell(logs.Select(thing => thing.Position).ToArray());
        check(logs.All(thing => thing.stackCount == 2), "a stroke covering both sides increments each stack once");
        Designator_SetThingCount count = new();
        Invoke(count, "SetAdjustmentMode", 1);
        count.DesignateMultiCell(logs.Select(thing => thing.Position).ToArray());
        check(logs.All(thing => thing.stackCount == 3), "relative quantity brush deduplicates reflected cells");
        IntVec3 center = new(40, 0, 40);
        spawn.DesignateSingleCell(center);
        check(At(center, ThingDefOf.WoodLog).Single().stackCount == 1, "objects on both axes are placed only once");
        Designator_Terrain.terrain = TerrainDefOf.Concrete;
        new Designator_Terrain().DesignateSingleCell(new IntVec3(8, 0, 8));
        check(symmetry.Transforms().All(transform => map.terrainGrid.TerrainAt(transform.Cell(new IntVec3(8, 0, 8))) == TerrainDefOf.Concrete),
            "terrain painting follows all axes");
        Designator_Roof.roof = RoofDefOf.RoofConstructed;
        new Designator_Roof().DesignateSingleCell(new IntVec3(9, 0, 9));
        check(symmetry.Transforms().All(transform => map.roofGrid.RoofAt(transform.Cell(new IntVec3(9, 0, 9))) == RoofDefOf.RoofConstructed),
            "roof painting follows all axes");
        Designator_Roof.roof = null!;
        new Designator_Roof().DesignateSingleCell(new IntVec3(9, 0, 9));
        check(symmetry.Transforms().All(transform => map.roofGrid.RoofAt(transform.Cell(new IntVec3(9, 0, 9))) == null), "roof removal is mirrored");
        ThingDef bedDef = DefDatabase<ThingDef>.GetNamed("Bed");
        spawn.SelectThing(bedDef, ThingDefOf.WoodLog);
        Field(typeof(Designator_Place), "placingRot").SetValue(spawn, Rot4.East);
        IntVec3 bedCell = new(20, 0, 10);
        spawn.DesignateSingleCell(bedCell);
        List<Thing> beds = Mirrors(bedCell, bedDef, Rot4.East);
        check(beds.Count == 4 && beds.All(thing => thing.Stuff == ThingDefOf.WoodLog), "multicell building placement preserves material and mirrored orientation");
        foreach (Thing bed in beds) bed.TryGetComp<CompQuality>().SetQuality(QualityCategory.Normal, null);
        IntVec3[] bedCells = beds.SelectMany(thing => thing.OccupiedRect()).ToArray();
        Designator_SetHitPoints hp = new();
        Invoke(hp, "SetAdjustmentMode", -10);
        int oldHp = beds[0].HitPoints;
        hp.DesignateMultiCell(bedCells);
        check(beds.All(thing => thing.HitPoints == oldHp - 10), "relative hit point brush visits multicell buildings once");
        Designator_SetQuality quality = new();
        FieldInfo mode = Field(typeof(Designator_SetQuality), "mode");
        mode.SetValue(quality, Enum.Parse(mode.FieldType, "Upgrade"));
        quality.DesignateMultiCell(bedCells);
        check(beds.All(thing => thing.TryGetComp<CompQuality>().Quality == QualityCategory.Good), "relative quality brush visits multicell buildings once");
        Designator_SetFaction.faction = Faction.OfPlayer;
        new Designator_SetFaction().DesignateMultiCell(bedCells);
        check(beds.All(thing => thing.Faction == Faction.OfPlayer), "faction brush is mirrored");
        var styled = DefDatabase<StyleCategoryDef>.AllDefsListForReading
            .SelectMany(category => category.thingDefStyles.Select(mapping => (category, mapping)))
            .First(pair => pair.mapping.ThingDef.category == ThingCategory.Building && pair.mapping.ThingDef.size.x <= 3
                && pair.mapping.ThingDef.size.z <= 3 && pair.mapping.ThingDef.comps.Any(comp => comp is CompProperties_Styleable));
        ThingDef styledDef = styled.mapping.ThingDef;
        spawn.SelectThing(styledDef, styledDef.MadeFromStuff ? GenStuff.AllowedStuffsFor(styledDef).First() : null!);
        Field(typeof(Designator_Place), "placingRot").SetValue(spawn, Rot4.North);
        IntVec3 styleCell = new(30, 0, 10);
        spawn.DesignateSingleCell(styleCell);
        Designator_SetStyle.style = styled.category;
        new Designator_SetStyle().DesignateSingleCell(styleCell);
        check(Mirrors(styleCell, styledDef).All(thing => thing.StyleDef == styled.mapping.StyleDef), "style brush is mirrored");
        Thing source = beds[0];
        Designator_MoveThing move = new();
        IntVec3 destination = new(21, 0, 14);
        move.DesignateThing(source);
        move.DesignateSingleCell(destination);
        check(beds.All(thing => symmetry.Transforms().Any(transform => thing.Position == transform.Position(destination, Rot4.East, bedDef)
            && thing.Rotation == transform.Rotation(Rot4.East))), "moving one building moves every counterpart");
        new Designator_RotateThing().DesignateThing(source);
        check(beds.All(thing => symmetry.Transforms().Any(transform => thing.Position == transform.Position(source.Position, source.Rotation, bedDef)
            && thing.Rotation == transform.Rotation(source.Rotation))), "rotation changes reflected orientation and even-sized anchors");
        IntVec3[] beforePositions = beds.Select(thing => thing.Position).ToArray();
        IntVec3 blockedDestination = new(27, 0, 17);
        CQFSymmetryTransform mirror = symmetry.Transform(1);
        IntVec3 blockedCell = mirror.Position(blockedDestination, source.Rotation, bedDef);
        Thing wall = GenSpawn.Spawn(ThingMaker.MakeThing(ThingDefOf.Wall, ThingDefOf.WoodLog), blockedCell, map);
        move.DesignateThing(source);
        move.DesignateSingleCell(blockedDestination);
        check(beds.Select(thing => thing.Position).SequenceEqual(beforePositions), "a blocked mirrored move leaves all source positions unchanged");
        wall.Destroy();
        IntVec3 blockedPlacement = new(16, 0, 25);
        wall = GenSpawn.Spawn(ThingMaker.MakeThing(ThingDefOf.Wall, ThingDefOf.WoodLog), symmetry.Transform(1).Cell(blockedPlacement), map);
        spawn.SelectThing(ThingDefOf.Wall, ThingDefOf.WoodLog);
        spawn.DesignateSingleCell(blockedPlacement);
        check(At(blockedPlacement, ThingDefOf.Wall).Count == 0 && !wall.Destroyed, "blocked placement does not wipe objects or partially create the mirrored group");
        wall.Destroy();
        ThingDef plantDef = DefDatabase<ThingDef>.GetNamed("Plant_Potato");
        spawn.SelectThing(plantDef, null!);
        IntVec3 plantCell = new(10, 0, 27);
        spawn.DesignateSingleCell(plantCell);
        new Designator_SetPlantGrowth().DesignateSingleCell(plantCell);
        check(Mirrors(plantCell, plantDef).Cast<Plant>().All(plant => plant.Growth == 1f), "plant growth brush is mirrored");
        Configurations();
        Persistence();
        new Designator_DestroyThing().DesignateMultiCell(logs.Select(thing => thing.Position).ToArray());
        check(logs.All(thing => thing.Destroyed), "deletion removes every counterpart once");
        Thing missing = beds[1];
        missing.Destroy();
        beforePositions = beds.Where(thing => !thing.Destroyed).Select(thing => thing.Position).ToArray();
        move.DesignateThing(source);
        move.DesignateSingleCell(new IntVec3(26, 0, 19));
        check(beds.Where(thing => !thing.Destroyed).Select(thing => thing.Position).SequenceEqual(beforePositions), "a missing counterpart rejects movement without changing survivors");
        symmetry.axisX = 79;
        symmetry.axisZ = 80;
        symmetry.enabled = true;
        symmetry.mirrorX = true;
        symmetry.mirrorZ = true;
    }

    private void Geometry()
    {
        foreach (int axis in new[] { 79, 80 })
        foreach (int width in Enumerable.Range(1, 4))
        foreach (int height in Enumerable.Range(1, 4))
        foreach (int direction in Enumerable.Range(0, 4))
        foreach (int mask in Enumerable.Range(1, 3))
        {
            ThingDef def = new() { defName = "CQF_SymmetryGeometry", size = new IntVec2(width, height), rotatable = true };
            CQFSymmetryTransform transform = new(mask, axis, 80);
            IntVec3 position = new(12, 0, 17);
            Rot4 rotation = new(direction);
            IntVec3 reflected = transform.Position(position, rotation, def);
            check(GenAdj.OccupiedRect(reflected, transform.Rotation(rotation), def.size).Equals(transform.Rect(GenAdj.OccupiedRect(position, rotation, def.size)))
                && transform.Position(reflected, transform.Rotation(rotation), def) == position,
                $"occupied rectangle reflection round trip {axis}/{width}x{height}/{direction}/{mask}");
        }
    }

    private void Configurations()
    {
        ThingDef def = CQFEditorTools.CustomThings.First(value => value.thingClass == typeof(InteractableThing));
        Designator_CQFTools spawn = new();
        spawn.SelectThing(def, def.MadeFromStuff ? ThingDefOf.WoodLog : null);
        IntVec3 cell = new(15, 0, 33);
        spawn.DesignateSingleCell(cell);
        List<InteractableThing> things = Mirrors(cell, def).Cast<InteractableThing>().ToList();
        InteractableThing source = things[0];
        Dialog_EditIDrawTabable window = new(source);
        Find.WindowStack.Add(window);
        source.operations.Add(new InteractionOperation { interactionText = "CQF symmetry operation" });
        window.Close(false);
        check(things.All(thing => thing.operations.Count == source.operations.Count), "closing the object window synchronizes complex configurations");
        check(things.Skip(1).All(thing => !ReferenceEquals(source.operations, thing.operations)
            && !ReferenceEquals(source.operations.Last(), thing.operations.Last())), "mirrored operation lists and nested objects are independent");
        int sourceCount = source.operations.Count;
        things[1].operations.Add(new InteractionOperation { interactionText = "Counterpart only" });
        check(source.operations.Count == sourceCount, "editing a copied list does not mutate its source");
        CompCustomText text = source.TryGetComp<CompCustomText>();
        check(text != null, "test object has the actual custom text component");
        symmetry.TrackInspector(source, typeof(ITab_CustomText));
        text!.useCustomName = true;
        text.customName = "Symmetric object";
        using (new CQFSymmetryScope(symmetry)) { }
        check(things.All(thing => thing.TryGetComp<CompCustomText>().customName == text.customName), "inspector edits synchronize before another map operation");
        check(things[1].operations.Count == sourceCount + 1, "synchronizing a text edit preserves unrelated counterpart configurations");
        MapComponent_CQFTargets targets = map.GetComponent<MapComponent_CQFTargets>();
        targets.TryRegister("CQF_SymmetrySource", source);
        window = new(source);
        Find.WindowStack.Add(window);
        source.operations[0].interactionText = "Updated symmetric operation";
        window.Close(false);
        check(things.Skip(1).All(thing => !targets.GetKeys(thing).Contains("CQF_SymmetrySource")), "unique target registration is not duplicated by configuration copying");
    }

    private void Persistence()
    {
        symmetry.axisX = 67;
        symmetry.axisZ = 70;
        symmetry.showAxes = false;
        string path = Path.Combine(GenFilePaths.SaveDataFolderPath, "SymmetryComponent.xml");
        Scribe.saver.InitSaving(path, "symmetry");
        symmetry.ExposeData();
        Scribe.saver.FinalizeSaving();
        symmetry.enabled = false;
        symmetry.axisX = -1;
        symmetry.axisZ = -1;
        symmetry.showAxes = true;
        Scribe.loader.InitLoading(path);
        symmetry.ExposeData();
        Scribe.loader.FinalizeLoading();
        check(symmetry.enabled && symmetry.axisX == 67 && symmetry.axisZ == 70 && !symmetry.showAxes, "native Scribe round trip preserves all per-map symmetry settings");
        symmetry.axisX = 80;
        symmetry.axisZ = 80;
        symmetry.showAxes = true;
    }

    private List<Thing> At(IntVec3 cell, ThingDef def) => cell.GetThingList(map).Where(thing => thing.def == def && thing.Position == cell).ToList();

    private List<Thing> Mirrors(IntVec3 cell, ThingDef def, Rot4? rotation = null)
        => symmetry.Transforms().SelectMany(transform => At(transform.Position(cell, rotation ?? Rot4.North, def), def)).Distinct().ToList();

    private static FieldInfo Field(Type type, string name) => type.GetField(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
        ?? throw new MissingFieldException(type.FullName, name);

    private static void Invoke(object value, string method, object argument) => value.GetType().GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(value, new[] { argument });

    private readonly Map map;
    private readonly CQFEditorSymmetry symmetry;
    private readonly Action<bool, string> check;
}
