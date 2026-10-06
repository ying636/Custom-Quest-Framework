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
            object[] arguments = new object[]
            {
            };
            CQFEditorBridge.Invoke("QuestEditor_Library.Spawner.DrawTab()", this, arguments);
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
