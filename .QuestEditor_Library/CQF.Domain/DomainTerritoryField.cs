using UnityEngine;
using Verse;

namespace QuestEditor_Library;

public sealed class DomainTerritoryField : IDisposable
{
    public DomainTerritoryField(DomainTerritoryPartition partition) { Partition = partition; }

    public DomainTerritoryPartition Partition { get; }
    public Texture2D Texture
    {
        get { Refresh(); return texture!; }
    }

    public void Configure(Material material, bool first)
        => Configure(material, first ? 0 : 1);

    public void Configure(Material material, int owner)
    {
        side = owner;
        material.SetVector("_TerritoryProtectionState", new Vector4(Partition.Width, Partition.Height, Partition.IsMultilateral || side == 0 ? 1f : 0f, 1f));
        material.SetTexture("_TerritoryProtection", Texture);
    }

    public void Configure(MaterialPropertyBlock properties, bool first)
        => Configure(properties, first ? 0 : 1);

    public void Configure(MaterialPropertyBlock properties, int owner)
    {
        side = owner;
        properties.SetVector("_TerritoryProtectionState", new Vector4(Partition.Width, Partition.Height, Partition.IsMultilateral || side == 0 ? 1f : 0f, 1f));
        properties.SetTexture("_TerritoryProtection", Texture);
    }

    public void Dispose()
    {
        if (disposed) return;
        disposed = true;
        if (texture != null) UnityEngine.Object.Destroy(texture);
        texture = null;
        pixels = null;
    }

    private void Refresh()
    {
        if (disposed) throw new ObjectDisposedException(nameof(DomainTerritoryField));
        if (texture == null)
        {
            texture = new Texture2D(Partition.Width, Partition.Height, TextureFormat.RGBA32, false, true)
            { name = "CQF_DomainTerritory", filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
            pixels = new Color[Partition.CellCount];
            revision = -1;
        }
        if (revision == Partition.Revision && encodedSide == side) return;
        for (int z = 0; z < Partition.Height; z++)
        for (int x = 0; x < Partition.Width; x++)
        {
            IntVec3 cell = new(x, 0, z);
            pixels![z * Partition.Width + x] = new Color((Partition.IsMultilateral ? Partition.OwnerIndex(cell) == side : Partition.IsFirst(cell)) ? 1f : 0f,
                Partition.OverrideAt(x, z) != 0 ? 1f : 0f,
                Mathf.Clamp01(0.5f + (Partition.IsMultilateral ? Partition.DistanceFor(side, x + 0.5f, z + 0.5f)
                    : Partition.BaseSignedDistance(x + 0.5f, z + 0.5f)) / 16f), Partition.UsesCurvedFront ? 1f : 0f);
        }
        texture.SetPixels(pixels);
        texture.Apply(false);
        revision = Partition.Revision;
        encodedSide = side;
    }

    private Texture2D? texture;
    private Color[]? pixels;
    private int revision = -1;
    private int side;
    private int encodedSide = -1;
    private bool disposed;
}
