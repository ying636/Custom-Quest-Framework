using RimWorld;
using RimWorld.Planet;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;

namespace QuestEditor_Library
{
    public class CaravanArrivalAction_VisitMainSite : CaravanArrivalAction
    {
        public CaravanArrivalAction_VisitMainSite()
        {
        }

        public CaravanArrivalAction_VisitMainSite(MainSite site)
        {
            this.site = site;
        }

        public override string Label
        {
            get
            {
                return this.site.ApproachOrderString;
            }
        }

        public override string ReportString
        {
            get
            {
                return this.site.ApproachingReportString;
            }
        }

        public override FloatMenuAcceptanceReport StillValid(Caravan caravan, PlanetTile destinationTile)
        {
            FloatMenuAcceptanceReport result = base.StillValid(caravan, destinationTile);
            if (!result)
            {
                return result;
            }
            if (this.site != null && this.site.Tile != destinationTile)
            {
                return false;
            }
            return CaravanArrivalAction_VisitSite.CanVisit(caravan, this.site);
        }

        public override void Arrived(Caravan caravan)
        {
            if (!this.site.HasMap)
            {
                LongEventHandler.QueueLongEvent(delegate
                {
                    this.DoEnter(caravan, this.site);
                }, "GeneratingMapForNewEncounter", false, null, true, false, null);
                return;
            }
            this.DoEnter(caravan, this.site);
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_References.Look(ref this.site, "site");
        }

        public static IEnumerable<FloatMenuOption> GetFloatMenuOptions(Caravan caravan, MainSite site)
        {
            return CaravanArrivalActionUtility.GetFloatMenuOptions(() => CaravanArrivalAction_VisitSite.CanVisit(caravan, site), () => new CaravanArrivalAction_VisitMainSite(site), site.ApproachOrderString, caravan, site.Tile, site);
        }

        private void DoEnter(Caravan caravan, MainSite site)
        {
            LookTargets lookTargets = new LookTargets(caravan.PawnsListForReading);
            bool draftColonists = site.Faction == null || site.Faction.HostileTo(Faction.OfPlayer);
            bool generated = !site.HasMap;
            Map map = GetOrGenerateMapUtility.GetOrGenerateMap(site.Tile, site.PreferredMapSize, null, null, false);
            if (generated)
            {
                Find.TickManager.Notify_GeneratedPotentiallyHostileMap();
                PawnRelationUtility.Notify_PawnsSeenByPlayer_Letter_Send(map.mapPawns.AllPawns, "LetterRelatedPawnsSite".Translate(Faction.OfPlayer.def.pawnsPlural), LetterDefOf.NeutralEvent, true, true);
                StringBuilder stringBuilder = new StringBuilder();
                stringBuilder.Append("LetterCaravanEnteredMap".Translate(caravan.Label, site).CapitalizeFirst());
                this.AppendThreatInfo(stringBuilder, site, map, out LetterDef letterDef, out LookTargets allLookTargets);
                Find.LetterStack.ReceiveLetter("LetterLabelCaravanEnteredMap".Translate(site), stringBuilder.ToString(), letterDef ?? LetterDefOf.NeutralEvent, allLookTargets.IsValid() ? allLookTargets : lookTargets);
            }
            else
            {
                Find.LetterStack.ReceiveLetter("LetterLabelCaravanEnteredMap".Translate(site), "LetterCaravanEnteredMap".Translate(caravan.Label, site).CapitalizeFirst(), LetterDefOf.NeutralEvent, lookTargets);
            }
            if (site.mapDef != null)
            {
                site.mapDef.EnterCaravan(caravan, map, CaravanDropInventoryMode.DoNotDrop, draftColonists);
            }
            else
            {
                CaravanEnterMapUtility.Enter(caravan, map, CaravanEnterMode.Edge, CaravanDropInventoryMode.DoNotDrop, draftColonists);
            }
        }

        private void AppendThreatInfo(StringBuilder sb, MainSite site, Map map, out LetterDef letterDef, out LookTargets allLookTargets)
        {
            allLookTargets = new LookTargets();
            letterDef = null;
            foreach (SitePartDef def in site.parts.Select(part => part.def).Distinct())
            {
                string arrivedLetterPart = def.Worker.GetArrivedLetterPart(map, out LetterDef partLetterDef, out LookTargets partLookTargets);
                if (arrivedLetterPart != null)
                {
                    if (sb.Length > 0)
                    {
                        sb.AppendLine();
                        sb.AppendLine();
                    }
                    sb.Append(arrivedLetterPart);
                    letterDef = letterDef ?? partLetterDef;
                    if (partLookTargets.IsValid())
                    {
                        allLookTargets = new LookTargets(allLookTargets.targets.Concat(partLookTargets.targets));
                    }
                }
            }
        }

        private MainSite site;
    }
}
