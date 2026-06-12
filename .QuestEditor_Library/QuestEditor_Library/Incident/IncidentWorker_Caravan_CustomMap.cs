using RimWorld;
using RimWorld.Planet;
using RimWorld.QuestGen;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;
using UnityEngine.Tilemaps;
using Verse;
using Verse.Grammar;
using Verse.Noise;

namespace QuestEditor_Library
{
    public class IncidentWorker_Caravan_CustomMap : IncidentWorker
    {
        protected override bool CanFireNowSub(IncidentParms parms)
        {
            if (this.def.GetModExtension<ModExtension_Caravan_CustomMap>() is ModExtension_Caravan_CustomMap extension) 
            {
                Faction faction = null;
                if (extension.factionDef != null)
                {
                    faction = Find.FactionManager.FirstFactionOfDef(extension.factionDef);
                }
                if(extension.needRandomHostileFaction) 
                {
                    faction = Find.FactionManager.RandomEnemyFaction();
                }
                return base.CanFireNowSub(parms) && parms.target as Caravan != null && (!extension.needFaction || faction != null) && CaravanIncidentUtility.CanFireIncidentWhichWantsToGenerateMapAt(parms.target.Tile);
            }
            return false;
        }

        protected override bool TryExecuteWorker(IncidentParms parms)
        {
            if (this.def.GetModExtension<ModExtension_Caravan_CustomMap>() is ModExtension_Caravan_CustomMap extension)
            {
                CustomMapDataDef mapDef = extension.set.GetMap();
                Caravan c = parms.target as Caravan;
                Pawn p = c.pawns.InnerListForReading.First();
                Faction faction = null;
                if (extension.factionDef != null)
                {
                    faction = Find.FactionManager.FirstFactionOfDef(extension.factionDef);
                }
                if (extension.needRandomHostileFaction)
                {
                    faction = Find.FactionManager.RandomEnemyFaction();
                }
                CustomSitePartParams siteParms = new CustomSitePartParams
                {
                    mapData = mapDef, 
                };
                CustomSite site = this.GenerateCustomSite(Gen.YieldSingle<SitePartDefWithParams>(new SitePartDefWithParams(DefDatabase<SitePartDef>.GetNamed("QE_CustomSite"), siteParms))
                    , c.Tile, faction, false);
                site.siteIconPath = extension.siteIconPath;
                site.expandingIconPath = extension.expandingIconPath;
                site.disdestroyBecauseOfNoColonist = false;
                site.customLabel = mapDef.label;
                site.customDescription = mapDef.description;
                Find.WorldObjects.Add(site);
                Map map = MapGenerator.GenerateMap(new IntVec3(200,1,200), site, site.MapGeneratorDef, site.ExtraGenStepDefs, null, false);     
                if (mapDef.TryGetEnterSpot(map, out IntVec3 enterSpot))
                {
                    CaravanEnterMapUtility.Enter(c, map, pawn => CellFinder.RandomSpawnCellForPawnNear(enterSpot, map, 4), CaravanDropInventoryMode.DoNotDrop, true);
                }
                else
                {
                    IntVec3 playerStartingSpot;
                    IntVec3 root;
                    MultipleCaravansCellFinder.FindStartingCellsFor2Groups(map, out playerStartingSpot, out root);
                    CaravanEnterMapUtility.Enter(c, map, pawn => CellFinder.RandomSpawnCellForPawnNear(playerStartingSpot, map, 4), CaravanDropInventoryMode.DoNotDrop, true);
                }
                base.SendStandardLetter(this.def.letterLabel, this.def.letterText, extension.letterDef, parms,p, Array.Empty<NamedArgument>());
            }
            return false;
        }

        public CustomSite GenerateCustomSite(IEnumerable<SitePartDefWithParams> sitePartsParams, int tile, Faction faction, bool hiddenSitePartsPossible = false)
        {
            Slate slate = QuestGen.slate;
            bool flag = false;
            using (IEnumerator<SitePartDefWithParams> enumerator = sitePartsParams.GetEnumerator())
            {
                while (enumerator.MoveNext())
                {
                    if (enumerator.Current.def.defaultHidden)
                    {
                        flag = true;
                        break;
                    }
                }
            }
            if (flag || hiddenSitePartsPossible)
            {
                SitePartParams parms = SitePartDefOf.PossibleUnknownThreatMarker.Worker.GenerateDefaultParams(0f, tile, faction);
                SitePartDefWithParams val = new SitePartDefWithParams(SitePartDefOf.PossibleUnknownThreatMarker, parms);
                sitePartsParams = sitePartsParams.Concat(Gen.YieldSingle<SitePartDefWithParams>(val));
            }
            CustomSite site = QuestNode_Root_CustomMap.MakeCustomSite(sitePartsParams, tile, faction, true);
            return site;
        }

    }
    public class ModExtension_Caravan_CustomMap : DefModExtension 
    {
        public LetterDef letterDef = LetterDefOf.NegativeEvent;
        public CustomMapGenerationSet set = null;
        public FactionDef factionDef = null;
        public string siteIconPath;
        public string expandingIconPath;
        public bool needRandomHostileFaction = false;
        public bool needFaction = false;
    }
}
