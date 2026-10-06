using RimWorld;
using RimWorld.Planet;
using RimWorld.QuestGen;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace QuestEditor_Library
{
    public class QuestNode_FindTileOnCoast : QuestNode, IDrawable
    {
        public void Draw(ref float y, Rect inRect, float x)

        {
            object[] arguments = new object[]
            {
                y,
                inRect,
                x
            };
            CQFEditorBridge.Invoke("QuestEditor_Library.QuestNode_FindTileOnCoast.Draw(Ref:float,None:UnityEngine.Rect,None:float)", this, arguments);
            y = (float)arguments[0];
        }
        protected override void RunInt()
        {
            if (TileFinder.TryFindPassableTileWithTraversalDistance(Find.AnyPlayerHomeMap.Tile, this.distance.min, this.distance.max, out PlanetTile tile,
                (x) => 
                {
                    Rot4 rot = Find.World.CoastDirectionAt(x);
                    if (rot.IsValid && (!this.requiredRot.IsValid || rot == this.requiredRot))
                    {
                        return true;
                    }
                    return false;
                })) 
            {
                
                Slate slate = QuestGen.slate;
                slate.Set(this.storeAs.GetValue(slate),tile);
            }
        }
        protected override bool TestRunInt(Slate slate)
        {
            return true;
        }
        public string buffer;
        public string bufferMin;
        public SlateRef<string> storeAs;
        public Rot4 requiredRot = Rot4.Invalid;
        public IntRange distance = new IntRange(10, 20);
    }
}
