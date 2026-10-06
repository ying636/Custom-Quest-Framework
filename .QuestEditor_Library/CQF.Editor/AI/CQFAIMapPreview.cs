using UnityEngine;
using Verse;

namespace QuestEditor_Library
{
    public sealed class CQFAIMapPreview
    {
        public void Draw(Rect rect, CustomMapDataDef map)
        {
            Widgets.DrawBoxSolid(rect, new Color(0.07f, 0.09f, 0.10f));
            float scale = Mathf.Min((rect.width - 16f) / map.size.x, (rect.height - 16f) / map.size.z);
            Vector2 origin = rect.center - new Vector2(map.size.x, map.size.z) * scale / 2f;
            Rect Cell(IntVec3 cell, float width = 1f, float height = 1f) => new Rect(origin.x + cell.x * scale, origin.y + (map.size.z - cell.z - height) * scale, width * scale, height * scale);
            foreach (var pair in map.terrains ?? new Dictionary<string, List<IntVec3>>())
                foreach (IntVec3 cell in pair.Value) Widgets.DrawBoxSolid(Cell(cell), TerrainColor(pair.Key));
            foreach (var pair in map.terrainsRect ?? new Dictionary<string, List<CellRect>>())
                foreach (CellRect area in pair.Value) Widgets.DrawBoxSolid(Cell(new IntVec3(area.minX, 0, area.minZ), area.Width, area.Height), TerrainColor(pair.Key));
            foreach (ThingData thing in map.thingDatas ?? new List<ThingData>())
            {
                foreach (IntVec3 position in CQFAIMapPlan.Positions(thing))
                {
                    CellRect area = GenAdj.OccupiedRect(position, thing.rotation, thing.def.size);
                    Rect box = Cell(new IntVec3(area.minX, 0, area.minZ), area.Width, area.Height);
                    Widgets.DrawBoxSolid(box, thing.def.category == ThingCategory.Building ? new Color(0.66f, 0.68f, 0.72f) : new Color(0.85f, 0.72f, 0.35f));
                    TooltipHandler.TipRegion(box, thing.def.LabelCap + " (" + position.x + ", " + position.z + ")");
                }
            }
            foreach (CustomThingData thing in (map.customThings ?? new List<CustomThingData>()).Concat(map.zoneCores ?? new List<CustomThingData>()))
            {
                CellRect area = GenAdj.OccupiedRect(thing.position, thing.rotation, thing.def.size);
                Rect box = Cell(new IntVec3(area.minX, 0, area.minZ), area.Width, area.Height);
                Widgets.DrawBoxSolid(box, new Color(0.35f, 0.85f, 0.66f));
                TooltipHandler.TipRegion(box, thing.def.LabelCap + "\n" + thing.GetType().Name);
            }
        }
        private static Color TerrainColor(string name)
        {
            uint value = 2166136261;
            foreach (char character in name) value = (value ^ character) * 16777619;
            return new Color(0.18f + (value & 255) / 900f, 0.20f + ((value >> 8) & 255) / 900f, 0.16f + ((value >> 16) & 255) / 900f);
        }
    }
}
