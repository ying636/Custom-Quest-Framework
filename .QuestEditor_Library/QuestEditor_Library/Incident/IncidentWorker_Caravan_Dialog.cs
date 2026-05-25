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
    public class IncidentWorker_Caravan_Dialog : IncidentWorker
    {
        protected override bool CanFireNowSub(IncidentParms parms)
        {
            return base.CanFireNowSub(parms);
        }

        protected override bool TryExecuteWorker(IncidentParms parms)
        {
            if (this.def.GetModExtension<ModExtension_Caravan_Dialog>() is ModExtension_Caravan_Dialog extension)
            {
                Caravan c = parms.target as Caravan;
                Pawn p = c.pawns.InnerListForReading.First();
                extension.dialog.CreateCQFDialog(p,p, null);
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
    public class ModExtension_Caravan_Dialog : DefModExtension 
    {
        public LetterDef letterDef = LetterDefOf.NegativeEvent;
        public DialogTreeDef dialog = null;
    }
}
