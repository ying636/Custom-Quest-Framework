using UnityEngine;
using Verse;

namespace QuestEditor_Library;

public sealed class DomainContestOverlay : IDisposable
{
    public DomainContestOverlay(MapComponent_Domain battlefield)
    {
        try
        {
            firstHasTerrain = battlefield.Worker?.RendersSurfaceTerrain == true;
            secondHasTerrain = battlefield.Challenger?.Worker?.RendersSurfaceTerrain == true;
            if (battlefield.Definition.surface != null) firstSurface = new DomainSurfaceVisuals(battlefield.Definition.surface);
            if (battlefield.Challenger?.Definition.surface != null) secondSurface = new DomainSurfaceVisuals(battlefield.Challenger.Definition.surface);
#if RIMWORLD_1_7
            boundaryMaterial = new Material(DefDatabase<ShaderDef>.GetNamed("CQF_DomainContestFront").Shader) { renderQueue = 2951 };
#else
            boundaryMaterial = new Material(ShaderDatabase.Transparent) { mainTexture = Texture2D.whiteTexture, renderQueue = 2951 };
#endif
            boundaryMaterial.color = battlefield.Definition.contestColor;
            boundaryMaterial.SetColor("_SecondaryColor", battlefield.Challenger?.Definition.contestColor ?? Color.white);
            if (!firstHasTerrain && firstSurface != null) firstMesh = new Mesh { name = "CQF_FirstContestTerritory" };
            if (!secondHasTerrain && secondSurface != null) secondMesh = new Mesh { name = "CQF_SecondContestTerritory" };
        }
        catch { Dispose(); throw; }
    }

    public void Draw(MapComponent_Domain battlefield, DomainTerritoryPartition partition)
    {
        float width = battlefield.map.Size.x;
        float height = battlefield.map.Size.z;
        float boundary = partition.BoundaryX;
        if (geometry?.Partition != partition) { geometry = new DomainTerritoryGeometry(partition); meshRevision = -1; }
        if (meshRevision != partition.Revision)
        {
            if (firstMesh != null) geometry.Rebuild(firstMesh, true, battlefield.map.Size.x, battlefield.map.Size.z, AltitudeLayer.FloorCoverings.AltitudeFor());
            if (secondMesh != null) geometry.Rebuild(secondMesh, false, battlefield.map.Size.x, battlefield.map.Size.z, AltitudeLayer.FloorCoverings.AltitudeFor());
            if (partition.HasOverrides)
            {
                contactMesh ??= new Mesh { name = "CQF_ContestContact" };
                float left = Math.Max(0f, boundary - 1.6f), right = Math.Min(width, boundary + 1.6f);
                geometry.RebuildUnclaimed(contactMesh, new Rect(left, 0f, right - left, height), AltitudeLayer.FloorCoverings.AltitudeFor() + 0.001f);
                if (partition.Invasion?.Strength > 0f)
                    for (int index = 0; index < partition.Invasion.Shapes.Count; index++)
                    {
                        Vector4 shape = partition.Invasion.Shapes[index];
                        holeContacts[index] ??= new Mesh { name = "CQF_ContestHoleContact" };
                        geometry.RebuildUnclaimed(holeContacts[index]!, new Rect(shape.x - shape.z - 0.75f, shape.y - shape.w - 0.75f,
                            shape.z * 2f + 1.5f, shape.w * 2f + 1.5f), AltitudeLayer.FloorCoverings.AltitudeFor() + 0.001f);
                    }
            }
            meshRevision = partition.Revision;
        }
        if (firstMesh?.vertexCount > 0) firstSurface!.Draw(firstMesh, Matrix4x4.identity, GenTicks.TicksGame / 60f);
        if (secondMesh?.vertexCount > 0) secondSurface!.Draw(secondMesh, Matrix4x4.identity, GenTicks.TicksGame / 60f);
        if (boundary <= 0f || boundary >= width || !battlefield.Active || battlefield.Challenger?.Active != true) return;
        float halfWidth = 1.6f;
        float from = Mathf.Max(0f, boundary - halfWidth);
        float to = Mathf.Min(width, boundary + halfWidth);
        float direction = partition.Reversed ? 1f : -1f;
        float target = DomainTerritoryPartition.CalculateFraction(battlefield.Health, battlefield.Challenger.Health);
        float targetBoundary = width * (partition.Reversed ? 1f - target : target);
        float pressure = Mathf.Clamp((targetBoundary - boundary) * 0.35f, -1f, 1f);
        float time = GenTicks.TicksGame / 60f;
        DomainContest contest = battlefield.Contest!;
        Vector4 clash = new(pressure, contest.Recoil, 0f, 2f);
        Vector4 circle = new(boundary, direction, halfWidth, 0f);
        Mesh? contact = partition.HasOverrides ? contactMesh : null;
        DrawSurface(from, to, height, firstSurface, circle, clash, contact);
        circle.y = -direction;
        DrawSurface(from, to, height, secondSurface, circle, clash, contact);
        properties.SetVector("_FrontState", new Vector4(boundary, time, pressure, contest.Recoil));
        properties.SetVector("_FrontShape", new Vector4(halfWidth, direction, target, contest.RecoilAge));
        properties.SetVector("_FrontHole", default);
        Vector3 center = new((from + to) * 0.5f, AltitudeLayer.FloorCoverings.AltitudeFor() + 0.001f, height * 0.5f);
        if (contact == null || contact.vertexCount > 0)
            GenDraw.DrawMeshNowOrLater(contact ?? MeshPool.plane10,
                contact == null ? Matrix4x4.TRS(center, Quaternion.identity, new Vector3(to - from, 1f, height)) : Matrix4x4.identity,
                boundaryMaterial!, false, properties);
        if (partition.Invasion?.Strength > 0f)
            for (int index = 0; index < partition.Invasion.Shapes.Count; index++)
            {
                Vector4 shape = partition.Invasion.Shapes[index];
                if (shape.z <= 0.01f || shape.w <= 0.01f) continue;
                properties.SetVector("_FrontHole", shape);
                properties.SetVector("_FrontWarp", new Vector4(partition.Invasion.Phases[index], 0f, 0f, 0f));
                properties.SetVector("_FrontShape", new Vector4(0.7f, partition.Invasion.First ? -1f : 1f, target, contest.RecoilAge));
                Vector3 holeCenter = new(shape.x, center.y, shape.y);
                Mesh? holeContact = partition.HasOverrides ? holeContacts[index] : null;
                if (holeContact == null || holeContact.vertexCount > 0)
                    GenDraw.DrawMeshNowOrLater(holeContact ?? MeshPool.plane10,
                        holeContact == null ? Matrix4x4.TRS(holeCenter, Quaternion.identity, new Vector3(shape.z * 2f + 1.5f, 1f, shape.w * 2f + 1.5f)) : Matrix4x4.identity,
                        boundaryMaterial!, false, properties);
            }
    }

    public void Dispose()
    {
        firstSurface?.Dispose();
        secondSurface?.Dispose();
        if (boundaryMaterial != null) UnityEngine.Object.Destroy(boundaryMaterial);
        if (firstMesh != null) UnityEngine.Object.Destroy(firstMesh);
        if (secondMesh != null) UnityEngine.Object.Destroy(secondMesh);
        if (contactMesh != null) UnityEngine.Object.Destroy(contactMesh);
        foreach (Mesh? mesh in holeContacts) if (mesh != null) UnityEngine.Object.Destroy(mesh);
    }

    private void DrawSurface(float from, float to, float height, DomainSurfaceVisuals? surface, Vector4 circle, Vector4 clash, Mesh? contact)
    {
        if (surface == null || from >= to) return;
        if (contact?.vertexCount == 0) return;
        Vector3 center = new((from + to) * 0.5f, AltitudeLayer.FloorCoverings.AltitudeFor(), height * 0.5f);
        surface.Draw(contact ?? MeshPool.plane10, contact == null ? Matrix4x4.TRS(center, Quaternion.identity, new Vector3(to - from, 1f, height)) : Matrix4x4.identity,
            GenTicks.TicksGame / 60f, circle, opacity: 1f, clash: clash);
    }

    private readonly DomainSurfaceVisuals? firstSurface;
    private readonly DomainSurfaceVisuals? secondSurface;
    private readonly bool firstHasTerrain;
    private readonly bool secondHasTerrain;
    private readonly Material? boundaryMaterial;
    private readonly MaterialPropertyBlock properties = new();
    private readonly Mesh? firstMesh;
    private readonly Mesh? secondMesh;
    private DomainTerritoryGeometry? geometry;
    private int meshRevision = -1;
    private Mesh? contactMesh;
    private readonly Mesh?[] holeContacts = new Mesh?[3];
}
