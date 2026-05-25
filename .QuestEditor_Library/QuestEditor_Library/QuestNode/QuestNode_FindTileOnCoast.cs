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
            CQFEditorTools.DrawLabelAndText_SlateRef_Line(y, "StoreAsText".Translate(),ref this.storeAs, x,100f);
            y += 30f;
            CQFEditorTools.DrawIntRange(ref y, "MapDistance".Translate(),ref this.distance,ref this.buffer,ref this.bufferMin,x);
            Func<Rot4, string> GetText = r => r == Rot4.Invalid ? "Rot_Invalid".Translate().ToString() : r.ToStringHuman().Translate().ToString();
            if (Widgets.ButtonText(new Rect(x,y,350f,25f),"RequiredRotation".Translate(GetText(this.requiredRot)),false)) 
            {
                CQFEditorTools.DrawFloatMenu(new List<Rot4>() {Rot4.East,Rot4.West,Rot4.North,Rot4.South,Rot4.Invalid},r => this.requiredRot = r,r => GetText(r));
            }
            y += 30f;
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
