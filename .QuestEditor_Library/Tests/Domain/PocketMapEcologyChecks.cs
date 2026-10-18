using System.Collections;
using System.Reflection;
using System.Runtime.CompilerServices;
using RimWorld;
using RimWorld.Planet;
using Verse;

internal static class PocketMapEcologyChecks
{
    public static void Run(BiomeDef biome)
    {
        Game previous = Current.Game;
        Game game = (Game)RuntimeHelpers.GetUninitializedObject(typeof(Game));
        typeof(Game).GetField("maps", BindingFlags.NonPublic | BindingFlags.Instance)!.SetValue(game, new List<Map>());
        World world = (World)RuntimeHelpers.GetUninitializedObject(typeof(World));
        world.grid = new WorldGrid();
        world.factionManager = new FactionManager();
        PlanetLayer layer = (PlanetLayer)RuntimeHelpers.GetUninitializedObject(typeof(PlanetLayer));
        typeof(PlanetLayer).GetField("def", BindingFlags.NonPublic | BindingFlags.Instance)!.SetValue(layer,
            new PlanetLayerDef { defName = "CQF_Check_PocketLayer" });
        ((Dictionary<int, PlanetLayer>)typeof(WorldGrid).GetField("planetLayers", BindingFlags.NonPublic | BindingFlags.Instance)!
            .GetValue(world.grid)!).Add(0, layer);
        world.tileTemperatures = new TileTemperaturesComp(world);
        game.World = world;
        Current.Game = game;
        try
        {
            Map map = (Map)RuntimeHelpers.GetUninitializedObject(typeof(Map));
            map.info = new MapInfo { isPocketMap = true };
            ThingDef? grass = DefDatabase<ThingDef>.GetNamedSilentFail("CQF_Check_GrazingPlant");
            if (grass == null)
            {
                grass = new ThingDef
                {
                    defName = "CQF_Check_GrazingPlant",
                    category = ThingCategory.Plant,
                    ingestible = new IngestibleProperties { preferability = FoodPreferability.RawBad, foodType = FoodTypeFlags.VegetarianRoughAnimal },
                    plant = new PlantProperties()
                };
                DefDatabase<ThingDef>.Add(grass);
            }
            BiomeDef control = new() { defName = "CQF_Check_GrazingBiome" };
            control.wildPlants.Add(new BiomePlantRecord { plant = grass, commonality = 1f });
            map.pocketTileInfo = new Tile { PrimaryBiome = control };
            bool reproduced = false;
            try { new MapPlantGrowthRateCalculator().BuildFor(map); }
            catch (IndexOutOfRangeException) { reproduced = true; }
            if (!reproduced) throw new InvalidOperationException("The control biome did not reproduce the native pocket-map temperature overflow.");
            world.tileTemperatures.ClearCaches();
            typeof(BiomeDef).GetField("cachedWildPlants", BindingFlags.NonPublic | BindingFlags.Instance)!.SetValue(biome, null);
            map.pocketTileInfo = new Tile { PrimaryBiome = biome };
            MapPlantGrowthRateCalculator calculator = new();
            calculator.BuildFor(map);
            int days = ((ICollection)typeof(MapPlantGrowthRateCalculator).GetField("dailyGrowthRates", BindingFlags.NonPublic | BindingFlags.Instance)!
                .GetValue(calculator)!).Count;
            int cachedTiles = ((IDictionary)typeof(TileTemperaturesComp).GetField("cache", BindingFlags.NonPublic | BindingFlags.Instance)!
                .GetValue(world.tileTemperatures)!).Count;
            if (map.Tile.Valid || calculator.WildGrazingPlants.Count != 0 || days != 60 || cachedTiles != 0)
                throw new InvalidOperationException("Test pocket-map ecology still depends on a world tile or temperature cache.");
        }
        finally { Current.Game = previous; }
    }
}
