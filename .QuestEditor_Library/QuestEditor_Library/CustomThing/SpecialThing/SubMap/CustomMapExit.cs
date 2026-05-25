using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;
using Verse.AI;

namespace QuestEditor_Library
{
    public class CustomMapExit : CQFMapPortal, IDrawTabable, ICustomThing
    {
        public override string Label => this.TextComp == null || !this.textComp.useCustomName ? base.Label : this.textComp.customName;
        public override string DescriptionFlavor => this.TextComp == null 
                                                    || 
                                                    !this.textComp.useCustomDescription ? 
            (this.Desc ?? base.DescriptionFlavor) : this.textComp.customDescription;
        
        public string Desc
        {
            get
            { 
                if (this.entrance is { opended: true } && this.def.GetModExtension<ModExtension_CustomThing>() is {} ex
                                                       && !ex.openedDesc.NullOrEmpty()) 
                {
                    return ex.openedDesc;
                }
                return null;
            }
        } 
        public override Graphic Graphic
        {
            get
            {
                if (this.entrance is { opended: true } &&
                    this.def.GetModExtension<ModExtension_CustomThing>() is { openedGraphicdata: { } data } 
                    && data.GraphicColoredFor(this) is { } g)
                {
                    return g;
                } 
                return  base.Graphic;
            }
        }
        public CompCustomText TextComp
        {
            get
            {
                if (this.textComp == null)
                {
                    this.textComp = this.TryGetComp<CompCustomText>();
                }
                return this.textComp;
            }
        }  
        public virtual string GetExitText => "Exit".Translate();
        public override bool IsEnterable(out string reason)
        {
            if (!base.IsEnterable(out reason)) 
            {
                return false;
            } 
            if (this.entrance != null && (!this.entrance.opended || !this.entrance.Spawned))
            {
                reason = "EntranceIsBlocked".Translate();
                return false;
            }
            reason = null;
            return true;    
        }
        public virtual new void Exit(Thing thing)
        {
            if (thing == null || this.entrance == null || 
                this.entrance.Position == null || this.entrance.Map == null)
            {
                return;
            }
            bool moveToRoot = this.Map.designationManager.DesignationOn(thing)?.def == QEDefOf.QE_MoveToRoot;
            if (thing.Spawned)
            {
                this.thereIsPawnIsEntering = true;
                thing.DeSpawn();
            }
            GenSpawn.Spawn(thing, this.entrance.Position, this.entrance.Map);
            if (thing is Pawn pawn)
            {
                this.OnEntered(pawn);
            }
            this.thereIsPawnIsEntering = false;
            if (moveToRoot && thing.Map.IsPocketMap)
            {
                this.entrance.Map.designationManager.AddDesignation(new Designation(thing, QEDefOf.QE_MoveToRoot));
            }
        }
        public void DrawTab()
        {
            float y = 20f;
            Text.Font = GameFont.Medium;
            Widgets.Label(new Rect(15f, y, 900f, 38f), "CustomMapExit".Translate().Colorize(ColorLibrary.SkyBlue));
            Text.Font = GameFont.Small;
            y += 40f;
            CQFEditorTools.DrawLabelAndText_Line(y, "ExitName".Translate(), ref this.exitName, 15f, 150f);
        }
        //public override IEnumerable<FloatMenuOption> GetFloatMenuOptions(Pawn selPawn)
        //{
        //    yield return new FloatMenuOption(this.GetExitText, delegate
        //    {
        //        Job job = JobMaker.MakeJob(QEDefOf.QE_EnterOrExitSubMap, this);
        //        job.reportStringOverride = "Exiting".Translate();
        //        selPawn.jobs.TryTakeOrderedJob(job);
        //    });
        //    yield break;
        //}
        //public override IEnumerable<FloatMenuOption> GetMultiSelectFloatMenuOptions(List<Pawn> selPawns)
        //{
        //    List<Pawn> pawns = selPawns.FindAll(p => p.CanReach(this, Verse.AI.PathEndMode.Touch, Danger.Deadly));
        //    if (pawns.Any())
        //    {
        //        yield return new FloatMenuOption(this.GetExitText, delegate
        //        {
        //            pawns.ForEach(p =>
        //            {
        //                Job job = JobMaker.MakeJob(QEDefOf.QE_EnterOrExitSubMap, this);
        //                job.reportStringOverride = "Exiting".Translate();
        //                p.jobs.TryTakeOrderedJob(job);
        //            });
        //        });
        //    }
        //    yield break;
        //}
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref this.thereIsPawnIsEntering, "thereIsPawnIsEntering");
            Scribe_Values.Look(ref this.exitName, "CQF_CustomMapExit_exitName");
            Scribe_References.Look(ref this.entrance, "CQF_CustomMapExit_entrance");
        }

        public CustomThingData GetData(IntVec3 pos)
        {
            return new CustomThingData_CustomMapExit(this,pos);
        }

        public override Map GetOtherMap()
        {
            return this.entrance.Map;
        }

        public override IntVec3 GetDestinationLocation()
        {
            return this.entrance == null ? IntVec3.Invalid : this.entrance.Position;
        }

        [NoTranslate]
        public string exitName = "undefined";
        public CustomMapEntrance entrance;
        private CompCustomText textComp = null;

        public bool thereIsPawnIsEntering = false;
    }
}
