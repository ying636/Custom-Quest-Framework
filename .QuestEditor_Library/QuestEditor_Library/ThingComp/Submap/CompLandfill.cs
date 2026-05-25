using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse.AI;
using Verse;

namespace QuestEditor_Library
{
    public class CompPropertiesLandFillable : CompProperties
    {
        public CompPropertiesLandFillable()
        {
            this.compClass = typeof(CompLandFillable);
        }

        public Texture2D Icon
        {
            get
            {
                if (this.icon == null)
                {
                    this.icon = ContentFinder<Texture2D>.Get(this.iconPath);
                }
                return this.icon;
            }
        }

        public int tickToFill = 120;
        public ThingDef filled;
        Texture2D icon;
        public string iconPath;
        public string landfillText = "CQF_Landfill"; 
    }
    public class CompLandFillable : ThingComp
    {
        public CompPropertiesLandFillable Props => (CompPropertiesLandFillable)this.props;
        public override IEnumerable<FloatMenuOption> CompFloatMenuOptions(Pawn selPawn)
        {
            if (this.startLandfill && selPawn.CanReserveAndReach(this.parent, PathEndMode.Touch, Danger.Deadly))
            {
                yield return new FloatMenuOption(this.Props.landfillText.Translate(), () =>
                selPawn.jobs.TryTakeOrderedJob(JobMaker.MakeJob(QEDefOf.QE_Landfill, this.parent)));
            }
            yield break;
        }
        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            yield return new Command_Action()
            {
                defaultLabel = this.startLandfill ? "CQF_CancelLandfill".Translate() : "CQF_StartLandfill".Translate(),
                defaultDesc = this.startLandfill ? "CQF_CancelLandfillDesc".Translate() : "CQF_StartLandfillDesc".Translate(),
                icon = this.Props.Icon,
                action = () => this.startLandfill = !this.startLandfill
            };
            yield break;
        }
        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref this.startLandfill, "startLandfill");
        }

        public bool startLandfill;
    }
}
