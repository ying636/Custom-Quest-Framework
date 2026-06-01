using RimWorld;
using RimWorld.Planet;
using System.Collections.Generic;
using Verse;

namespace QuestEditor_Library
{
    public class MainSite : CustomSite
    {
        public string MainMapKey
        {
            get
            {
                return "MainMap:" + this.ID;
            }
        }

        public bool TryGetMainPawn(string pawnName, out Pawn pawn)
        {
            pawn = null;
            return !pawnName.NullOrEmpty() && this.mainPawns != null && this.mainPawns.TryGetValue(pawnName, out pawn);
        }

        public void SetMainPawn(string pawnName, Pawn pawn)
        {
            if (pawnName.NullOrEmpty() || pawn == null)
            {
                return;
            }
            if (this.mainPawns == null)
            {
                this.mainPawns = new Dictionary<string, Pawn>();
            }
            this.mainPawns.SetOrAdd(pawnName, pawn);
        }

        public bool RemoveMainPawnCache(string pawnName)
        {
            if (pawnName.NullOrEmpty() || this.mainPawns == null)
            {
                return false;
            }
            return this.mainPawns.Remove(pawnName);
        }

        public override void PostMapGenerate()
        {
            base.PostMapGenerate();
            this.lastGenerateTick = Find.TickManager.TicksGame;
            this.lastLeaveTick = -1;
            this.visitCount++;
        }

        public override void Notify_MyMapRemoved(Map map)
        {
            this.lastLeaveTick = Find.TickManager.TicksGame;
            base.Notify_MyMapRemoved(map);
        }

        public override IEnumerable<FloatMenuOption> GetFloatMenuOptions(Caravan caravan)
        {
            foreach (FloatMenuOption floatMenuOption in base.GetFloatMenuOptions(caravan))
            {
                yield return floatMenuOption;
            }
            if (!this.HasMap)
            {
                foreach (FloatMenuOption floatMenuOption in CaravanArrivalAction_VisitMainSite.GetFloatMenuOptions(caravan, this))
                {
                    yield return floatMenuOption;
                }
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Defs.Look(ref this.mainMapDef, "mainMapDef");
            Scribe_Collections.Look(ref this.mainPawns, "mainPawns", LookMode.Value, LookMode.Reference, ref this.tmpMainPawnNames, ref this.tmpMainPawns);
            Scribe_Values.Look(ref this.lastLeaveTick, "lastLeaveTick", -1);
            Scribe_Values.Look(ref this.lastGenerateTick, "lastGenerateTick", -1);
            Scribe_Values.Look(ref this.visitCount, "visitCount");
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                MainMapWorldComponent.Component?.RegisterMainSite(this);
            }
        }

        public MainMapDef mainMapDef;
        public Dictionary<string, Pawn> mainPawns = new Dictionary<string, Pawn>();
        public int lastLeaveTick = -1;
        public int lastGenerateTick = -1;
        public int visitCount;

        private List<string> tmpMainPawnNames;
        private List<Pawn> tmpMainPawns;
    }
}
