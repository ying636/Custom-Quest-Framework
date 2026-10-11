using System.Reflection;
using System.Runtime.CompilerServices;
using RimWorld;
using Verse;

internal static class ThingPlacementStyleChecks
{
    public static void Run(Map map, Assembly editor)
    {
        Type styleType = editor.GetType("QuestEditor_Library.Designator_SetStyle")!;
        FieldInfo selectedStyle = styleType.GetField("style")!;
        FieldInfo clearStyle = styleType.GetField("clearStyle", BindingFlags.NonPublic | BindingFlags.Static)!;
        object? previousStyle = selectedStyle.GetValue(null);
        object? previousClear = clearStyle.GetValue(null);
        Designator designator = (Designator)RuntimeHelpers.GetUninitializedObject(styleType);
        ThingDef definition = (ThingDef)RuntimeHelpers.GetUninitializedObject(typeof(ThingDef));
        definition.defName = "CQF_Check_StyledThing";
        definition.label = definition.defName;
        definition.category = ThingCategory.Item;
        definition.drawerType = DrawerType.RealtimeOnly;
        definition.size = IntVec2.One;
        ThingStyleDef appearance = (ThingStyleDef)RuntimeHelpers.GetUninitializedObject(typeof(ThingStyleDef));
        appearance.defName = "CQF_Check_Style";
        StyleCategoryDef category = new StyleCategoryDef { defName = "CQF_Check_StyleCategory" };
        ThingDefStyle pair = new ThingDefStyle();
        typeof(ThingDefStyle).GetField("thingDef", BindingFlags.NonPublic | BindingFlags.Instance)!.SetValue(pair, definition);
        typeof(ThingDefStyle).GetField("styleDef", BindingFlags.NonPublic | BindingFlags.Instance)!.SetValue(pair, appearance);
        category.thingDefStyles = new List<ThingDefStyle> { pair };
        ThingWithComps thing = new ThingWithComps { def = definition, Position = new IntVec3(2, 0, 2) };
        thing.compStyleable = new CompStyleable { parent = thing, styleDef = appearance, cachedStyleCategoryDef = category };
        typeof(Thing).GetField("mapIndexOrState", BindingFlags.NonPublic | BindingFlags.Instance)!.SetValue(thing, (sbyte)0);
        map.thingGrid.Register(thing);
        try
        {
            selectedStyle.SetValue(null, null);
            clearStyle.SetValue(null, false);
            Check(!designator.CanDesignateCell(thing.Position).Accepted, "missing style selection is rejected without a null reference");
            selectedStyle.SetValue(null, category);
            Check(designator.CanDesignateCell(thing.Position).Accepted, "matching style category accepts a styleable thing");
            Check(!designator.CanDesignateCell(new IntVec3(3, 0, 2)).Accepted, "empty cells reject style changes");
            selectedStyle.SetValue(null, null);
            clearStyle.SetValue(null, true);
            Check(designator.CanDesignateCell(thing.Position).Accepted, "clear mode accepts an existing style without a selected category");
            FieldInfo graphicCache = typeof(Thing).GetField("styleGraphicInt", BindingFlags.NonPublic | BindingFlags.Instance)!;
            graphicCache.SetValue(thing, RuntimeHelpers.GetUninitializedObject(typeof(Graphic_Single)));
            designator.DesignateSingleCell(thing.Position);
            Check(thing.StyleDef == null && thing.compStyleable.cachedStyleCategoryDef == null && graphicCache.GetValue(thing) == null,
                "clearing restores default style and invalidates category and graphic caches");
            Check(!designator.CanDesignateCell(thing.Position).Accepted, "clear mode rejects things already using their default style");
            selectedStyle.SetValue(null, category);
            clearStyle.SetValue(null, false);
            designator.DesignateSingleCell(thing.Position);
            Check(thing.StyleDef == appearance, "a category can be reapplied after clearing");
        }
        finally
        {
            map.thingGrid.Deregister(thing);
            selectedStyle.SetValue(null, previousStyle);
            clearStyle.SetValue(null, previousClear);
        }
        foreach (string name in new[] { "Designator_SpawnThing", "Designator_CQFTools" })
        {
            Type type = editor.GetType("QuestEditor_Library." + name)!;
            FieldInfo selection = type.GetField("thing")!;
            object? previous = selection.GetValue(null);
            try
            {
                Designator placement = (Designator)RuntimeHelpers.GetUninitializedObject(type);
                selection.SetValue(null, definition);
                definition.rotatable = true;
                Check(placement.DrawStyleCategory == null, name + " disables area placement for rotatable things");
                Check(type.GetMethod("DoExtraGuiControls")!.DeclaringType == typeof(Designator_Place)
                    && type.GetMethod("SelectedProcessInput")!.DeclaringType == typeof(Designator_Place),
                    name + " inherits native rotation controls and shortcuts");
                definition.rotatable = false;
                Check(placement.DrawStyleCategory == QuestEditor_Library.QEDefOf.CQF_Areas,
                    name + " preserves area placement for non-rotatable things");
            }
            finally
            {
                selection.SetValue(null, previous);
            }
        }
    }

    private static void Check(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
        Console.WriteLine("PASS " + message);
    }
}
