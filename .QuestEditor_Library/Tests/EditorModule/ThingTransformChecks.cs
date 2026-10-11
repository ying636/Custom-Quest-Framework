using System.Reflection;
using System.Runtime.CompilerServices;
using System.Xml.Linq;
using RimWorld;
using Verse;

internal static class ThingTransformChecks
{
    public static void Run(string gameRoot, Assembly editor)
    {
        Game? previousGame = Current.Game;
        LoadedLanguage? previousLanguage = LanguageDatabase.activeLanguage;
        LoadedLanguage? previousDefaultLanguage = LanguageDatabase.defaultLanguage;
        object prefs = typeof(Prefs).GetField("data", BindingFlags.NonPublic | BindingFlags.Static)!.GetValue(null)!;
        FieldInfo devMode = prefs.GetType().GetField("devMode")!;
        object previousDevMode = devMode.GetValue(prefs)!;
        try
        {
            FieldInfo binding = typeof(DefOfHelper).GetField("bindingNow", BindingFlags.NonPublic | BindingFlags.Static)!;
            object previousBinding = binding.GetValue(null)!;
            try
            {
                binding.SetValue(null, true);
                RuntimeHelpers.RunClassConstructor(typeof(ThingDefOf).TypeHandle);
            }
            finally
            {
                binding.SetValue(null, previousBinding);
            }
            LoadedLanguage language = (LoadedLanguage)RuntimeHelpers.GetUninitializedObject(typeof(LoadedLanguage));
            language.info = new LanguageInfo();
            typeof(LoadedLanguage).GetField("dataIsLoaded", BindingFlags.NonPublic | BindingFlags.Instance)!.SetValue(language, true);
            language.keyedReplacements = XDocument.Load(Path.Combine(gameRoot, "Mods", "CQF", "Languages", "English", "Keyed", "Key.xml"))
                .Root!.Elements().ToDictionary(element => element.Name.LocalName,
                    element => new LoadedLanguage.KeyedReplacement { value = element.Value });
            foreach (string key in new[] { "OutOfBounds", "SpaceAlreadyOccupied", "CQF_EditorTargetUnavailable" })
                language.keyedReplacements[key] = new LoadedLanguage.KeyedReplacement { value = key };
            LanguageDatabase.activeLanguage = language;
            LanguageDatabase.defaultLanguage = language;
            Game game = (Game)RuntimeHelpers.GetUninitializedObject(typeof(Game));
            Map map = (Map)RuntimeHelpers.GetUninitializedObject(typeof(Map));
            map.info = new MapInfo { Size = new IntVec3(12, 1, 12) };
            map.cellIndices = new CellIndices(map);
            map.thingGrid = new ThingGrid(map);
            map.edificeGrid = new EdificeGrid(map);
            typeof(Game).GetField("maps", BindingFlags.NonPublic | BindingFlags.Instance)!.SetValue(game, new List<Map> { map });
            game.currentMapIndex = 0;
            Current.Game = game;
            Type moveType = editor.GetType("QuestEditor_Library.Designator_MoveThing")!;
            Type rotateType = editor.GetType("QuestEditor_Library.Designator_RotateThing")!;
            Designator move = (Designator)RuntimeHelpers.GetUninitializedObject(moveType);
            Designator rotate = (Designator)RuntimeHelpers.GetUninitializedObject(rotateType);
            MethodInfo placement = moveType.BaseType!.GetMethod("GetPlacementReport", BindingFlags.NonPublic | BindingFlags.Instance)!;
            Thing thing = MakeThing(new IntVec2(2, 3), new IntVec3(5, 0, 5));
            devMode.SetValue(prefs, false);
            Check(!move.Visible && !rotate.Visible && !move.CanDesignateThing(thing).Accepted, "tools are hidden and reject interaction outside developer mode");
            devMode.SetValue(prefs, true);
            Check(move.Visible && rotate.Visible && move.CanDesignateThing(thing).Accepted, "developer mode exposes both tools without god mode");
            Check(move.DrawStyleCategory == null && rotate.DrawStyleCategory == null, "tools use single clicks instead of area dragging");
            Check(Report(new IntVec3(5, 0, 5), Rot4.North).Accepted, "multi-cell thing can stay in its original footprint");
            Check(Report(new IntVec3(8, 0, 8), Rot4.North).Accepted, "multi-cell thing can move to a free destination");
            Check(Report(new IntVec3(5, 0, 5), Rot4.East).Accepted, "rotation ignores the same thing in overlapping footprint cells");
            Check(!Report(new IntVec3(11, 0, 11), Rot4.North).Accepted, "moving rejects a footprint that crosses the map boundary");
            Check(!Report(new IntVec3(0, 0, 0), Rot4.East).Accepted, "rotation rejects a footprint that crosses the map boundary");
            thing.debugRotLocked = true;
            Check(!Report(thing.Position, Rot4.East).Accepted && Report(new IntVec3(8, 0, 8), Rot4.North).Accepted,
                "rotation lock prevents rotation while allowing movement");
            thing.debugRotLocked = false;
            Thing blocker = MakeThing(new IntVec2(1, 1), new IntVec3(8, 0, 8));
            blocker.def.blocksAltitudes = new List<AltitudeLayer> { thing.def.altitudeLayer };
            Check(!Report(new IntVec3(8, 0, 8), Rot4.North).Accepted, "moving rejects a conflicting thing instead of replacing it");
            Check(map.thingGrid.ThingsListAt(blocker.Position).Contains(blocker), "validation preserves the conflicting thing");
            map.thingGrid.Deregister(blocker);
            Check(Report(thing.Position, Rot4.West).Accepted, "counterclockwise rotation accepts its new footprint");

            AcceptanceReport Report(IntVec3 position, Rot4 rotation)
            {
                return (AcceptanceReport)placement.Invoke(move, new object[] { thing, position, rotation })!;
            }

            Thing MakeThing(IntVec2 size, IntVec3 position)
            {
                ThingDef definition = (ThingDef)RuntimeHelpers.GetUninitializedObject(typeof(ThingDef));
                definition.defName = "CQF_Check_Transform";
                definition.label = definition.defName;
                definition.size = size;
                definition.selectable = true;
                definition.destroyable = true;
                definition.category = ThingCategory.Building;
                definition.comps = new List<CompProperties>();
                definition.building = new BuildingProperties();
                Thing result = new Thing
                {
                    def = definition,
                    Position = position,
                    Rotation = Rot4.North
                };
                typeof(Thing).GetField("mapIndexOrState", BindingFlags.NonPublic | BindingFlags.Instance)!.SetValue(result, (sbyte)0);
                map.thingGrid.Register(result);
                return result;
            }
        }
        finally
        {
            devMode.SetValue(prefs, previousDevMode);
            Current.Game = previousGame!;
            LanguageDatabase.activeLanguage = previousLanguage!;
            LanguageDatabase.defaultLanguage = previousDefaultLanguage!;
        }
    }

    private static void Check(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
        Console.WriteLine("PASS " + message);
    }
}
