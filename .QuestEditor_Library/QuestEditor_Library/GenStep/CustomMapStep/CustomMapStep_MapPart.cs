using System.Xml.Linq;
using UnityEngine;
using Verse;

namespace QuestEditor_Library;

public class CustomMapStep_MapPart : CustomMapStep
{
    public override void Generate(Map map, CustomMapDataDef def, CustomSitePartParams param)
    {
        List<IntVec3> cells = map.AllCells.Where(c => !GenStep_CustomMap.disgenerate.Contains(c)).ToList();
        int c = this.count.RandomInRange;
        for (int i = 0; i < c; i++)
        {
            if (this.set.GetMap() is {} mapDef)
            {
                if (Prefs.DevMode)
                {
                    Log.Message("Spawn Part:" + mapDef.defName);
                }
                var cellsAvailable = new List<IntVec3>();
                foreach (var intVec3 in cells)
                {
                    var size = mapDef.size;
                    size.y = 0;
                    if (CellRect.FromLimits(intVec3,intVec3 +size ).Cells.All(c2 => 
                          c2.InBounds(map) && !GenStep_CustomMap.disgenerate.Contains(c2)))
                    {
                        cellsAvailable.Add(intVec3);
                    }
                }

                if (cellsAvailable.Any())
                {    
                    var pos =cellsAvailable.RandomElement();
                    if (Prefs.DevMode)
                    {
                        Log.Message("Spawn Pos:" + pos);
                    }
                    mapDef.GenerateByCore(pos, map, GameTools.GetQuestFromMap(map));
                    foreach (var intVec3 in CellRect.FromLimits(pos,pos + mapDef.size).Cells)
                    {
                        if (cells.Contains(intVec3))
                        {
                            cells.Remove(intVec3);
                        }
                    }
                }
            }
            else
            {
                break;
            }
        }
    }

    public override void Draw(ref float y, Rect inRect, float x)
    {
        CQFEditorTools.DrawIntRange(ref y, "GenerationCount".Translate(), ref count, ref buffer, ref buffer2, x, 60f);
        this.set.Draw(ref y, inRect, x);
    }
    public override XElement SaveToXElement(string nodeName)
    {
        XElement result = base.SaveToXElement(nodeName);
        result.Add(new XElement("count", this.count));
        result.Add(this.set.SaveToXElement("set"));
        return result;
    }

    public string buffer;
    public string buffer2;
    public IntRange count = IntRange.One;
    public CustomMapGenerationSet set = new CustomMapGenerationSet();
}