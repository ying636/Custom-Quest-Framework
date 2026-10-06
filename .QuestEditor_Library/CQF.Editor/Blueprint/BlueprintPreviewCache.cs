using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace QuestEditor_Library
{
    public static class BlueprintPreviewCache
    {
        public static void Draw(Rect rect, CustomMapDataDef blueprint)
        {
            Widgets.DrawBoxSolid(rect, PreviewBackground);
            if (blueprint == null)
            {
                return;
            }
            GUI.DrawTexture(rect.ContractedBy(2f), GetTexture(blueprint), ScaleMode.ScaleToFit, true);
            Widgets.DrawBox(rect);
        }

        public static void Remove(CustomMapDataDef blueprint)
        {
            if (blueprint != null && textures.Remove(blueprint, out Texture2D texture))
            {
                UnityEngine.Object.Destroy(texture);
            }
        }

        public static void Clear()
        {
            foreach (Texture2D texture in textures.Values)
            {
                UnityEngine.Object.Destroy(texture);
            }
            textures.Clear();
        }

        private static Texture2D GetTexture(CustomMapDataDef blueprint)
        {
            if (!textures.TryGetValue(blueprint, out Texture2D texture))
            {
                texture = CreateTexture(blueprint);
                textures.Add(blueprint, texture);
            }
            return texture;
        }

        private static Texture2D CreateTexture(CustomMapDataDef blueprint)
        {
            Texture2D texture = new Texture2D(TextureWidth, TextureHeight, TextureFormat.RGBA32, false)
            {
                name = "CQF_BlueprintPreview_" + blueprint.defName,
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp
            };
            Color32[] pixels = Enumerable.Repeat((Color32)PreviewBackground, TextureWidth * TextureHeight).ToArray();
            IntVec3 size = blueprint.size.IsValid
                ? new IntVec3(Math.Max(1, blueprint.size.x), 1, Math.Max(1, blueprint.size.z))
                : new IntVec3(1, 1, 1);

            foreach (KeyValuePair<string, List<IntVec3>> terrainData in blueprint.terrains)
            {
                TerrainDef terrain = DefDatabase<TerrainDef>.GetNamedSilentFail(terrainData.Key);
                Color color = terrain?.DrawColor ?? UnknownTerrainColor;
                foreach (IntVec3 cell in terrainData.Value)
                {
                    PaintCell(pixels, size, cell, color);
                }
            }
            foreach (KeyValuePair<string, List<CellRect>> terrainData in blueprint.terrainsRect)
            {
                TerrainDef terrain = DefDatabase<TerrainDef>.GetNamedSilentFail(terrainData.Key);
                Color color = terrain?.DrawColor ?? UnknownTerrainColor;
                foreach (CellRect cellRect in terrainData.Value)
                {
                    foreach (IntVec3 cell in cellRect.Cells)
                    {
                        PaintCell(pixels, size, cell, color);
                    }
                }
            }
            foreach (KeyValuePair<ColorDef, List<CellRect>> colorData in blueprint.terrainsColorRect)
            {
                Color color = colorData.Key?.color ?? Color.white;
                foreach (CellRect cellRect in colorData.Value)
                {
                    foreach (IntVec3 cell in cellRect.Cells)
                    {
                        PaintCell(pixels, size, cell, color, true);
                    }
                }
            }

            foreach (List<IntVec3> roofCells in blueprint.roofs.Values)
            {
                foreach (IntVec3 cell in roofCells)
                {
                    PaintCell(pixels, size, cell, RoofColor, true);
                }
            }
            foreach (List<CellRect> roofRects in blueprint.roofRects.Values)
            {
                foreach (CellRect cellRect in roofRects)
                {
                    foreach (IntVec3 cell in cellRect.Cells)
                    {
                        PaintCell(pixels, size, cell, RoofColor, true);
                    }
                }
            }

            foreach (ThingData thing in blueprint.thingDatas)
            {
                if (thing?.def == null)
                {
                    continue;
                }
                Color color = GetThingColor(thing.def);
                foreach (IntVec3 cell in GetThingPositions(thing))
                {
                    CellRect occupiedRect = GenAdj.OccupiedRect(cell, thing.rotation, thing.def.Size);
                    foreach (IntVec3 occupiedCell in occupiedRect.Cells)
                    {
                        PaintStructureCell(pixels, size, occupiedCell, color,
                            thing.def.IsEdifice());
                    }
                    if (!thing.def.IsEdifice())
                    {
                        PaintMarker(pixels, size, cell, color);
                    }
                }
            }
            foreach (CustomThingData thing in blueprint.customThings)
            {
                if (thing?.def == null)
                {
                    continue;
                }
                Color color = GetThingColor(thing.def);
                foreach (IntVec3 cell in GenAdj.OccupiedRect(
                    thing.position, thing.rotation, thing.def.Size).Cells)
                {
                    PaintStructureCell(pixels, size, cell, color, thing.def.IsEdifice());
                }
                PaintMarker(pixels, size, thing.position, color);
            }
            foreach (CustomThingData core in blueprint.zoneCores)
            {
                PaintMarker(pixels, size, core.position, ZoneCoreColor);
            }
            foreach (IntVec3 cell in blueprint.pawns.Keys)
            {
                PaintMarker(pixels, size, cell, PawnColor);
            }
            foreach (IntVec3 cell in blueprint.specialSpawnPawns.Keys)
            {
                PaintMarker(pixels, size, cell, PawnColor);
            }

            texture.SetPixels32(pixels);
            texture.Apply(false, true);
            return texture;
        }

        private static void PaintCell(Color32[] pixels, IntVec3 size, IntVec3 cell, Color color, bool blend = false)
        {
            if (cell.x < 0 || cell.z < 0 || cell.x >= size.x || cell.z >= size.z)
            {
                return;
            }
            GetDrawArea(size, out int offsetX, out int offsetY, out int drawWidth, out int drawHeight);
            int minX = offsetX + Mathf.FloorToInt((float)cell.x * drawWidth / size.x);
            int maxX = offsetX + Mathf.CeilToInt((float)(cell.x + 1) * drawWidth / size.x) - 1;
            int minY = offsetY + Mathf.FloorToInt((float)cell.z * drawHeight / size.z);
            int maxY = offsetY + Mathf.CeilToInt((float)(cell.z + 1) * drawHeight / size.z) - 1;
            for (int y = minY; y <= maxY; y++)
            {
                for (int x = minX; x <= maxX; x++)
                {
                    int index = y * TextureWidth + x;
                    pixels[index] = blend
                        ? (Color32)Color.Lerp(pixels[index], color, color.a)
                        : (Color32)color;
                }
            }
        }

        private static void PaintMarker(Color32[] pixels, IntVec3 size, IntVec3 cell, Color color)
        {
            if (cell.x < 0 || cell.z < 0 || cell.x >= size.x || cell.z >= size.z)
            {
                return;
            }
            GetDrawArea(size, out int offsetX, out int offsetY, out int drawWidth, out int drawHeight);
            int centerX = offsetX + Mathf.FloorToInt(((float)cell.x + 0.5f) * drawWidth / size.x);
            int centerY = offsetY + Mathf.FloorToInt(((float)cell.z + 0.5f) * drawHeight / size.z);
            for (int y = Math.Max(0, centerY - 1); y <= Math.Min(TextureHeight - 1, centerY + 1); y++)
            {
                for (int x = Math.Max(0, centerX - 1); x <= Math.Min(TextureWidth - 1, centerX + 1); x++)
                {
                    pixels[y * TextureWidth + x] = color;
                }
            }
        }

        private static void PaintStructureCell(Color32[] pixels, IntVec3 size, IntVec3 cell,
            Color color, bool edifice)
        {
            if (cell.x < 0 || cell.z < 0 || cell.x >= size.x || cell.z >= size.z)
            {
                return;
            }
            GetDrawArea(size, out int offsetX, out int offsetY, out int drawWidth, out int drawHeight);
            int minX = offsetX + Mathf.FloorToInt((float)cell.x * drawWidth / size.x);
            int maxX = offsetX + Mathf.CeilToInt((float)(cell.x + 1) * drawWidth / size.x) - 1;
            int minY = offsetY + Mathf.FloorToInt((float)cell.z * drawHeight / size.z);
            int maxY = offsetY + Mathf.CeilToInt((float)(cell.z + 1) * drawHeight / size.z) - 1;
            Color borderColor = Color.Lerp(color, Color.black, edifice ? 0.55f : 0.3f);
            Color fillColor = Color.Lerp(color, Color.white, edifice ? 0.05f : 0.18f);
            for (int y = minY; y <= maxY; y++)
            {
                for (int x = minX; x <= maxX; x++)
                {
                    bool border = x == minX || x == maxX || y == minY || y == maxY;
                    pixels[y * TextureWidth + x] = border ? (Color32)borderColor : (Color32)fillColor;
                }
            }
        }

        private static IEnumerable<IntVec3> GetThingPositions(ThingData thing)
        {
            if (!thing.allPositions.NullOrEmpty())
            {
                return thing.allPositions;
            }
            if (!thing.allRect.NullOrEmpty())
            {
                return thing.allRect.SelectMany(rect => rect.Cells);
            }
            return new[] { thing.position };
        }

        private static Color GetThingColor(ThingDef def)
        {
            if (def.IsDoor)
            {
                return DoorColor;
            }
            Color color = def.uiIconColor;
            if (color.a <= 0.01f || color.maxColorComponent <= 0.08f)
            {
                color = def.graphicData?.color ?? ThingColor;
            }
            if (color.a <= 0.01f || color.maxColorComponent <= 0.08f)
            {
                color = ThingColor;
            }
            return color;
        }

        private static void GetDrawArea(IntVec3 size, out int offsetX, out int offsetY, out int drawWidth,
            out int drawHeight)
        {
            float scale = Math.Min((float)(TextureWidth - PreviewPadding * 2) / Math.Max(1, size.x),
                (float)(TextureHeight - PreviewPadding * 2) / Math.Max(1, size.z));
            drawWidth = Math.Max(1, Mathf.RoundToInt(size.x * scale));
            drawHeight = Math.Max(1, Mathf.RoundToInt(size.z * scale));
            offsetX = (TextureWidth - drawWidth) / 2;
            offsetY = (TextureHeight - drawHeight) / 2;
        }

        private const int PreviewPadding = 2;
        private const int TextureHeight = 64;
        private const int TextureWidth = 96;

        private static readonly Color PreviewBackground = new Color(0.08f, 0.09f, 0.1f, 1f);
        private static readonly Color UnknownTerrainColor = new Color(0.35f, 0.35f, 0.35f, 1f);
        private static readonly Color RoofColor = new Color(0.08f, 0.1f, 0.14f, 0.65f);
        private static readonly Color ThingColor = new Color(0.9f, 0.9f, 0.9f, 1f);
        private static readonly Color DoorColor = new Color(0.72f, 0.48f, 0.22f, 1f);
        private static readonly Color CustomThingColor = new Color(0.15f, 0.85f, 0.95f, 1f);
        private static readonly Color ZoneCoreColor = new Color(0.95f, 0.35f, 0.85f, 1f);
        private static readonly Color PawnColor = new Color(1f, 0.8f, 0.15f, 1f);
        private static readonly Dictionary<CustomMapDataDef, Texture2D> textures =
            new Dictionary<CustomMapDataDef, Texture2D>();
    }
}
