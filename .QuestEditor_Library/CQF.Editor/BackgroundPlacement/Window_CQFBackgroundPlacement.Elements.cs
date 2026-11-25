#if RIMWORLD_1_7
using RimWorld;
using UnityEngine;
using Verse;

namespace QuestEditor_Library;

public sealed partial class Window_CQFBackgroundPlacement
{
    private void DrawElements(Rect rect)
    {
        if (this.page == SceneryPage.Cliffs)
        {
            Rect tile = new Rect(rect.x + 1f, rect.y + 1f, Mathf.Min(122f, rect.width - 2f), 91f);
            if (this.DrawTile(tile, "CQF_SceneryCliffElement".Translate(), "CQF_BackgroundPlacementCliffTip".Translate(),
                    this.session.Cliffs && this.PlacementActive, string.Empty, new Vector2(10f, 4f), 5f, new Color(.09f, .09f, .10f)))
            {
                this.session.ChooseCliffs();
                this.ActivatePlacement();
            }
            return;
        }

        if (this.page == SceneryPage.Placed)
        {
            this.DrawPlaced(rect);
            return;
        }

        const float tileWidth = 111f;
        const float tileHeight = 91f;
        const float gap = 7f;
        float contentWidth = rect.width - 16f;
        int columns = Mathf.Max(1, Mathf.FloorToInt((contentWidth + gap) / (tileWidth + gap)));
        float width = Mathf.Min(tileWidth, contentWidth);
        int count = this.visibleStructureDefs.Length;
        int rows = (count + columns - 1) / columns;
        var view = new Rect(0f, 0f, contentWidth, Mathf.Max(rect.height, rows * (tileHeight + gap) - gap + 2f));
        Widgets.BeginScrollView(rect, ref this.paletteScroll, view);
        int firstRow = Mathf.Max(0, Mathf.FloorToInt(this.paletteScroll.y / (tileHeight + gap)));
        int lastIndex = Mathf.Min(count, Mathf.CeilToInt((this.paletteScroll.y + rect.height) / (tileHeight + gap)) * columns);
        for (int index = firstRow * columns; index < lastIndex; index++)
        {
            CQFBackgroundStructureDef def = this.visibleStructureDefs[index];
            Rect tile = new Rect(1f + index % columns * (width + gap), 1f + index / columns * (tileHeight + gap), width - 1f, tileHeight);
            bool selected = (this.session.Pending ?? this.session.Selection)?.def == def;
            if (this.DrawTile(tile, def.LabelCap, def.description.NullOrEmpty() ? def.LabelCap.ToString() : def.description,
                    selected, def.texturePath, def.size, def.height, def.color, def.wallLightRadius > 0f))
            {
                this.session.ChooseStructure(def);
                this.ActivatePlacement();
            }
        }
        Widgets.EndScrollView();
    }

    private bool DrawTile(Rect rect, string label, string tip, bool selected, string texturePath, Vector2 footprint, float height, Color color,
        bool flat = false)
    {
        CQFUIStyle.DrawBackground(rect, selected);
        this.DrawThumbnail(new Rect(rect.x + 10f, rect.y + 8f, rect.width - 20f, rect.height - 35f), texturePath, footprint, height, color, flat);
        using var scope = new CQFUIScope();
        Text.Anchor = TextAnchor.MiddleCenter;
        Rect caption = new Rect(rect.x + 4f, rect.yMax - 25f, rect.width - 8f, 22f);
        Widgets.LabelFit(caption, label);
        TooltipHandler.TipRegion(rect, tip);
        return Widgets.ButtonInvisible(rect);
    }

    private void DrawThumbnail(Rect rect, string path, Vector2 footprint, float height, Color color, bool flat)
    {
        if (Event.current.type != EventType.Repaint) return;
        if (!this.thumbnailTextures.TryGetValue(path, out var texture))
        {
            texture = path.NullOrEmpty() ? BaseContent.WhiteTex : ContentFinder<Texture2D>.Get(path) ?? BaseContent.WhiteTex;
            this.thumbnailTextures.Add(path, texture);
        }
        if (flat)
        {
            Widgets.DrawTextureFitted(rect, texture, 1f);
            return;
        }
        float frontWidth = Mathf.Max(.8f, footprint.x);
        float frontHeight = Mathf.Max(.8f, height);
        float depthX = Mathf.Max(.85f, footprint.y * .48f);
        float depthY = Mathf.Max(.5f, footprint.y * .29f);
        float scale = Mathf.Min(rect.width / (frontWidth + depthX), rect.height / (frontHeight + depthY));
        float w = frontWidth * scale;
        float h = frontHeight * scale;
        float dx = depthX * scale;
        float dy = depthY * scale;
        var start = new Vector2(rect.center.x - (w + dx) / 2f, rect.center.y - (h + dy) / 2f + dy);
        Color previousColor = GUI.color;
        void DrawFace(Rect face, float brightness)
        {
            GUI.color = previousColor * new Color(color.r * brightness, color.g * brightness, color.b * brightness, color.a);
            Widgets.DrawTextureFitted(face, texture, 1f, face.size, new Rect(0f, 0f, 1f, 1f));
        }

        try
        {
            DrawFace(new Rect(start.x + dx, start.y - dy, w, h + dy), .58f);
            DrawFace(new Rect(start.x, start.y - dy, w, dy + 1f), 1.12f);
            DrawFace(new Rect(start.x, start.y, w, h), .87f);
        }
        finally
        {
            GUI.color = previousColor;
        }
    }

    private void DrawPlaced(Rect rect)
    {
        List<CQFBackgroundCliff> cliffs = this.session.Draft.cliffs;
        List<CQFBackgroundStructure> structures = this.session.Draft.structures;
        int count = cliffs.Count + structures.Count;
        if (count == 0)
        {
            Widgets.Label(rect, "CQF_BackgroundPlacementEmpty".Translate());
            return;
        }

        const float stride = 32f;
        float width = rect.width - 16f;
        var view = new Rect(0f, 0f, width, Mathf.Max(rect.height, count * stride));
        Widgets.BeginScrollView(rect, ref this.paletteScroll, view);
        int firstIndex = Mathf.Max(0, Mathf.FloorToInt(this.paletteScroll.y / stride));
        int lastIndex = Mathf.Min(count, Mathf.CeilToInt((this.paletteScroll.y + rect.height) / stride));
        for (int index = firstIndex; index < lastIndex; index++)
        {
            Rect row = new Rect(0f, index * stride, width, 29f);
            CQFBackgroundCliff? cliff = index < cliffs.Count ? cliffs[index] : null;
            CQFBackgroundStructure? item = index < cliffs.Count ? null : structures[index - cliffs.Count];
            string label = cliff != null ? "CQF_BackgroundPlacementCliffArea".Translate(cliff.area.Min, cliff.area.Max)
                : "CQF_SceneryPlacedStructure".Translate(item!.def.LabelCap, item.position);
            CQFUIStyle.DrawBackground(row,
                cliff != null ? cliff == this.session.CliffSelection : item == this.session.Selection);
            if (!CQFUIStyle.ButtonText(row, label, drawBackground: false)) continue;
            if (this.PlacementActive) Find.DesignatorManager.Deselect();
            if (cliff != null) this.session.SelectCliff(cliff);
            else this.session.Select(item!);
            this.inspectorScroll = Vector2.zero;
        }
        Widgets.EndScrollView();
    }

    private void DrawInspector(Rect rect)
    {
        float width = rect.width - 16f;
        Widgets.BeginScrollView(rect, ref this.inspectorScroll, new Rect(0f, 0f, width, Mathf.Max(rect.height, 170f)));
        using var scope = new CQFUIScope(width);
        if (this.page == SceneryPage.Cliffs || (this.page == SceneryPage.Placed && this.session.CliffSelection != null))
        {
            float y = 0f;
            CQFBackgroundCliff? selected = this.session.CliffSelection;
            Widgets.LabelFit(new Rect(0f, y, width, 25f), "CQF_SceneryCliffProperties".Translate());
            y += 28f;
            float edgeWidth = width / 4f;
            for (int index = 0; index < EdgeNames.Length; index++)
            {
                CQFCliffEdges edge = (CQFCliffEdges)(1 << index);
                if (Toggle(new Rect(index * edgeWidth, y, edgeWidth - 4f, 25f), EdgeNames[index],
                        (this.session.CliffEdges & edge) != 0))
                    this.session.ToggleEdge(edge);
            }
            y += 31f;
            Adjust(ref y, width, "CQF_BackgroundPlacementDepth", this.session.CliffDepth, 1f, 1f, 32f, this.session.SetCliffDepth);
            if (selected != null)
            {
                float third = (width - 10f) / 3f;
                if (Button(new Rect(0f, y, third, 26f), "CQF_SceneryRedrawCliff")) this.ActivatePlacement();
                if (Button(new Rect(third + 5f, y, third, 26f), "CQF_SceneryLocate"))
                    Find.CameraDriver.JumpToCurrentMapLoc(selected.area.CenterCell);
                if (Button(new Rect(2f * (third + 5f), y, third, 26f), "CQF_SceneryDelete"))
                {
                    this.session.RemoveCliff();
                    this.ActivateSelection();
                }
            }
        }
        else
        {
            CQFBackgroundStructure? item = this.session.MovingStructure ?? this.session.Selection ?? this.session.Pending;
            if (item == null || (this.page == SceneryPage.Structures && item.def.category != this.category))
                Widgets.Label(new Rect(0f, 0f, width, 48f), "CQF_ScenerySelectElement".Translate());
            else this.DrawStructureInspector(width, item);
        }

        Widgets.EndScrollView();
    }

    private void DrawStructureInspector(float width, CQFBackgroundStructure item)
    {
        Rect title = new Rect(0f, 0f, width, 25f);
        Widgets.LabelFit(title, item.def.LabelCap);
        if (!item.def.description.NullOrEmpty()) TooltipHandler.TipRegion(title, item.def.description);
        float y = 28f;
        Adjust(ref y, width, "CQF_BackgroundPlacementRotation", item.rotation, 15f, -15f, 360f,
            value => this.session.AlterStructure(selected => selected.rotation = Mathf.Repeat(value, 360f)));
        Adjust(ref y, width, "CQF_BackgroundPlacementScale", item.scale, .25f, .25f, 24f,
            value => this.session.AlterStructure(selected => selected.scale = value));
        Adjust(ref y, width, "CQF_BackgroundPlacementDepth", item.depth, 1f, -128f, 128f,
            value => this.session.AlterStructure(selected => selected.depth = value));
        if (this.session.Selection == null) return;
        float part = (width - 15f) / 4f;
        if (Button(new Rect(0f, y, part, 26f), "CQF_BackgroundPlacementMove"))
        {
            this.session.StartMove();
            this.ActivatePlacement();
        }
        if (Button(new Rect(part + 5f, y, part, 26f), "Copy"))
        {
            this.session.Duplicate();
            this.ActivatePlacement();
        }
        if (Button(new Rect(2f * (part + 5f), y, part, 26f), "CQF_SceneryLocate"))
            Find.CameraDriver.JumpToCurrentMapLoc(item.position);
        if (Button(new Rect(3f * (part + 5f), y, part, 26f), "CQF_SceneryDelete"))
        {
            this.session.DeleteSelection();
            this.ActivateSelection();
        }
    }

    private static void Adjust(ref float y, float width, string key, float value, float increment, float min, float max, Action<float> update)
    {
        Widgets.LabelFit(new Rect(0f, y, width - 78f, 26f), key.Translate() + "   " + value.ToString("0.##"));
        if (CQFUIStyle.ButtonText(new Rect(width - 70f, y, 32f, 26f), "−"))
            update(Mathf.Clamp(value - increment, min, max));
        if (CQFUIStyle.ButtonText(new Rect(width - 32f, y, 32f, 26f), "+"))
            update(Mathf.Clamp(value + increment, min, max));
        y += 30f;
    }

    private static readonly string[] EdgeNames = { "CQF_BackgroundPlacementNorth", "CQF_BackgroundPlacementEast",
        "CQF_BackgroundPlacementSouth", "CQF_BackgroundPlacementWest" };
    private readonly Dictionary<string, Texture2D> thumbnailTextures = new();
}
#endif
