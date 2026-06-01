using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace QuestEditor_Library
{
    public class Spawner : ThingWithComps, IDrawTabable
    {
        public override IEnumerable<Gizmo> GetGizmos()
        {
            foreach (Gizmo gizmo in base.GetGizmos())
            {
                yield return gizmo;
            }
            yield return new Command_Action() {
                defaultLabel = "Spawn"
                ,
                action = () =>
            {
                this.pawns.ForEach(x =>
                {
                    PawnSpawnData pawnData = x as PawnSpawnData;
                    Lord lord = pawnData == null ? null : LordMaker.MakeNewLord(pawnData.faction == null ? null : GameTools.GetFaction(pawnData.faction, this.Map), new LordJob_Custom(), this.Map);
                    x.Spawn(this.Position, this.Map, "null",null, lord);
                }
                );
            } };
            yield break;
        }

        public void DrawTab()
        {
            Widgets.BeginScrollView(new Rect(7f, 25f, 475f, 590f), ref this.scrollPos, new Rect(7f, 10f, 475f, this.height));
            float y = 10f;
            float initY = y;
            foreach (PawnSpawnData pawnData in this.pawns)
            {
                Rect rectData = new Rect(17f, y + 3f, 450f, 25f);
                if (Widgets.ButtonText(rectData, pawnData.dataName, false))
                {
                    Find.WindowStack.Add(new Dialog_EditIDrawable(pawnData));
                }
                y += 30f;
            }
            Widgets.DrawBox(new Rect(7f, initY,350f, y - initY), 1, QuestEditor_Dialog.blueTex);
            y += 10f;
            CQFEditorTools.DrawButtonForPawnData(y, this.pawns);
            y += 40f;
            this.height = y + 15f;
            Widgets.EndScrollView();
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Collections.Look(ref this.pawns, "QE_Spawner_Pawns",LookMode.Deep);
        }    

        public float height = 0f;
        public Vector2 scrollPos;
        public List<PawnSpawnData> pawns = new List<PawnSpawnData>();
    }
}


