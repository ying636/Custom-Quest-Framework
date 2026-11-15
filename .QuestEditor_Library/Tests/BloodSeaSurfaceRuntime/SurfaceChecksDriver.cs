using System.Text.Json;
using QuestEditor_Library;
using RimWorld;
using UnityEngine;
using Verse;

namespace CQF.BloodSeaSurfaceChecks;

public sealed class SurfaceChecksDriver : MonoBehaviour
{
    private void Update()
    {
        if (finished || LongEventHandler.AnyEventNowOrWaiting || Current.ProgramState != ProgramState.Playing || Find.CurrentMap == null) return;
        try
        {
            routine ??= Run().GetEnumerator();
            if (routine.MoveNext()) return;
            routine.Dispose();
            routine = null;
            finished = true;
            File.AppendAllText(Path.Combine(Bootstrap.Output, "Checks.txt"), "COMPLETE\n");
            Application.Quit(0);
        }
        catch (Exception error)
        {
            File.AppendAllText(Path.Combine(Bootstrap.Output, "Checks.txt"), "FAIL " + error + "\n");
            Log.Error("[CQF.BloodSeaSurfaceChecks] " + error);
            finished = true;
            routine?.Dispose();
            Application.Quit(1);
        }
    }

    private static IEnumerable<object?> Run()
    {
        Find.TickManager.Pause();
        Map source = Find.CurrentMap;
        Pawn caster = PawnGenerator.GeneratePawn(new PawnGenerationRequest(PawnKindDefOf.Colonist, Faction.OfPlayer));
        GenSpawn.Spawn(caster, source.Center, source);
        DomainDef definition = DefDatabase<DomainDef>.GetNamed("RSH_BloodSeaDomain");
        MapComponent_Domain domain = CQFDomainRuntime.Instance.EnterOrContest(definition, caster, Array.Empty<Pawn>());
        Current.Game.CurrentMap = domain.map;
        Find.CameraDriver.SetRootPosAndSize(domain.map.Center.ToVector3Shifted(), 32f);
        for (int frame = 0; frame < 90; frame++) yield return null;
        Check(domain.Active && domain.Worker != null && caster.Map == domain.map, "blood sea enters with a live worker and caster");
        TerrainDef terrain = domain.map.terrainGrid.TerrainAt(domain.map.Center);
        Check(terrain.defName == "RSH_BloodSeaFloor", "blood sea center uses the real blood sea terrain");
        Type layerType = domain.Worker!.GetType().Assembly.GetType("RSH.SectionLayer_BloodSeaDomain", true)!;
        SectionLayer layer = domain.map.mapDrawer.SectionAt(domain.map.Center).GetLayer(layerType);
        Material material = domain.Worker.GetTerrainMaterial(terrain)!;
        File.WriteAllText(Path.Combine(Bootstrap.Output, "SurfaceState.json"), JsonSerializer.Serialize(new
        {
            DomainActive = domain.Active, LayerVisible = layer.Visible, DebugViewSettings.drawTerrain, DebugViewSettings.drawTerrainWater,
            Terrain = terrain.defName, material.name, Shader = material.shader.name, Color = material.color.ToString(),
            Dissolve = material.GetFloat("_Dissolve"), State = material.GetVector("_SurfaceState").ToString(),
            Fullness = material.GetFloat("_RSHDomainFullness"),
            Meshes = layer.subMeshes.Select(mesh => new { mesh.finalized, mesh.disabled, Vertices = mesh.mesh.vertexCount }).ToArray()
        }, new JsonSerializerOptions { WriteIndented = true }));
        ScreenCapture.CaptureScreenshot(Path.Combine(Bootstrap.Output, "01-FirstEntry.png"));
        for (int frame = 0; frame < 4; frame++) yield return null;
        Check(layer.Visible && layer.subMeshes.Any(mesh => mesh.finalized && !mesh.disabled && mesh.mesh.vertexCount > 0), "entry rebuilds the blood sea ground mesh without a manual refresh");
        int mapId = domain.map.uniqueID;
        GameDataSaveLoader.SaveGame("CQF_BloodSeaSurface");
        yield return null;
        Game previous = Current.Game;
        GameDataSaveLoader.LoadGame("CQF_BloodSeaSurface");
        yield return null;
        Check(Current.Game != previous && Find.CurrentMap.uniqueID == mapId, "native reload restores the active blood sea map");
        Find.TickManager.Pause();
        domain = Find.CurrentMap.GetComponent<MapComponent_Domain>();
        for (int frame = 0; frame < 90; frame++) yield return null;
        ValidateTerrain(domain, layerType, "reloaded blood sea");
        ScreenCapture.CaptureScreenshot(Path.Combine(Bootstrap.Output, "02-Reloaded.png"));
        for (int frame = 0; frame < 5; frame++) yield return null;
        source = domain.SourceMap!;
        caster = domain.Caster!;
        Current.Game.CurrentMap = source;
        domain.RequestReturn();
        int limit = Time.frameCount + 360;
        while ((!domain.Available || !GameComponent_MapManager.Instance!.IsHibernating(domain.map)) && Time.frameCount < limit) yield return null;
        Check(domain.Available && caster.Map == source && GameComponent_MapManager.Instance!.IsHibernating(domain.map), "blood sea returns its caster and hibernates");
        definition = DefDatabase<DomainDef>.GetNamed("RSH_BloodSeaDomain");
        domain = CQFDomainRuntime.Instance.EnterOrContest(definition, caster, Array.Empty<Pawn>());
        Check(domain.map.uniqueID == mapId, "blood sea reuses the same prepared map");
        Current.Game.CurrentMap = domain.map;
        Find.CameraDriver.SetRootPosAndSize(domain.map.Center.ToVector3Shifted(), 32f);
        for (int frame = 0; frame < 90; frame++) yield return null;
        ValidateTerrain(domain, layerType, "reused blood sea");
        ScreenCapture.CaptureScreenshot(Path.Combine(Bootstrap.Output, "03-Reused.png"));
        for (int frame = 0; frame < 5; frame++) yield return null;
        Pawn basicCaster = PawnGenerator.GeneratePawn(new PawnGenerationRequest(PawnKindDefOf.Colonist, Faction.OfPlayer));
        GenSpawn.Spawn(basicCaster, source.Center, source);
        DomainDef basic = DefDatabase<DomainDef>.GetNamed("CQF_TestDomain");
        DomainVisualDef? visuals = basic.visuals;
        basic.visuals = null;
        MapComponent_Domain basicDomain;
        try { basicDomain = CQFDomainRuntime.Instance.EnterOrContest(basic, basicCaster, Array.Empty<Pawn>()); }
        finally { basic.visuals = visuals; }
        Current.Game.CurrentMap = basicDomain.map;
        Find.CameraDriver.SetRootPosAndSize(basicDomain.map.Center.ToVector3Shifted(), 24f);
        for (int frame = 0; frame < 90; frame++) yield return null;
        Type basicLayer = typeof(MapComponent_Domain).Assembly.GetType("QuestEditor_Library.SectionLayer_BasicDomainTerrain", true)!;
        ValidateTerrain(basicDomain, basicLayer, "single basic domain");
        ScreenCapture.CaptureScreenshot(Path.Combine(Bootstrap.Output, "04-BasicDomain.png"));
        for (int frame = 0; frame < 5; frame++) yield return null;
    }

    private static void ValidateTerrain(MapComponent_Domain domain, Type layerType, string name)
    {
        SectionLayer layer = domain.map.mapDrawer.SectionAt(domain.map.Center).GetLayer(layerType);
        Check(domain.Active && layer.Visible && layer.subMeshes.Any(mesh => mesh.finalized && !mesh.disabled && mesh.mesh.vertexCount > 0), name + " has its ground mesh before any forced refresh");
    }

    private static void Check(bool condition, string name)
    {
        if (!condition) throw new InvalidOperationException(name);
        File.AppendAllText(Path.Combine(Bootstrap.Output, "Checks.txt"), "PASS " + name + "\n");
    }

    private IEnumerator<object?>? routine;
    private bool finished;
}
