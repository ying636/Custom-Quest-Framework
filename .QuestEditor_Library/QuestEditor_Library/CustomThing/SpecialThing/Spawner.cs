using RimWorld;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
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

            yield return new Command_Action()
            {
                defaultLabel = "Spawn",
                action = () =>
                {
                    this.pawns.ForEach(x =>
                    {
                        PawnSpawnData pawnData = x as PawnSpawnData;
                        Lord lord = pawnData == null ? null : LordMaker.MakeNewLord(pawnData.faction == null ? null : GameTools.GetFaction(pawnData.faction, this.Map), new LordJob_Custom(), this.Map);
                        x.Spawn(this.Position, this.Map, "null", null, lord);
                    });
                }
            };
            yield break;
        }

        public void DrawTab()
        {
            using CQFUIScope scope = new CQFUIScope();
            Rect viewport = new Rect(8f, 36f, Mathf.Min(490f, CQFUIScope.ContentWidth - 16f), Mathf.Max(40f, CQFUIScope.ContentHeight - 44f));
            Rect content = new Rect(0f, 0f, viewport.width - 20f, Mathf.Max(viewport.height, this.height));
            Widgets.BeginScrollView(viewport, ref this.scrollPos, content);
            using CQFUIScope contentScope = new CQFUIScope(content.width);
            float y = 10f;
            float initY = y;
            foreach (PawnSpawnData pawnData in this.pawns)
            {
                Rect rectData = new Rect(12f, y + 3f, content.width - 24f, 25f);
                if (CQFUIStyle.ButtonText(rectData, pawnData.dataName, false))
                {
                    Find.WindowStack.Add(CQFEditorBridge.Editor.CreateDrawableWindow(pawnData));
                }

                y += 30f;
            }

            CQFUIStyle.DrawBox(new Rect(7f, initY, 350f, y - initY), 1, CQFUIStyle.AccentTexture);
            y += 10f;
            CQFEditorTools.DrawButtonForPawnData(y, this.pawns);
            y += 40f;
            this.height = y + 15f;
            Widgets.EndScrollView();
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Collections.Look(ref this.pawns, "QE_Spawner_Pawns", LookMode.Deep);
        }

        public float height = 0f;
        public Vector2 scrollPos;
        public List<PawnSpawnData> pawns = new List<PawnSpawnData>();
    }
}
