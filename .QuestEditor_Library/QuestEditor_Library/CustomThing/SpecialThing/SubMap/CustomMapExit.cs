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
        public override void OnEntered(Pawn pawn)
        {
            base.OnEntered(pawn);
            this.TriggerEnterActions(pawn);
        }
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
            if (!(thing is Pawn))
            {
                this.TriggerEnterActions(thing);
            }
        }
        public void DrawTab()

        {
            object[] arguments = new object[]
            {
            };
            CQFEditorBridge.Invoke("QuestEditor_Library.CustomMapExit.DrawTab()", this, arguments);
        } //public override IEnumerable<FloatMenuOption> GetFloatMenuOptions(Pawn selPawn)
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
            Scribe_Collections.Look(ref this.enterActions, "enterActions", LookMode.Deep);
            if (Scribe.mode == LoadSaveMode.PostLoadInit && this.enterActions == null)
            {
                this.enterActions = new List<CQFAction>();
            }
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
        internal void DrawActionSection(ref float y, float x, float width)

        {
            object[] arguments = new object[]
            {
                y,
                x,
                width
            };
            CQFEditorBridge.Invoke("QuestEditor_Library.CustomMapExit.DrawActionSection(Ref:float,None:float,None:float)", this, arguments);
            y = (float)arguments[0];
        }
        internal void DrawSectionHeader(ref float y, float x, float width, string label, string tip = null,
            Action addAction = null, Action removeAction = null, bool canRemove = false)

        {
            object[] arguments = new object[]
            {
                y,
                x,
                width,
                label,
                tip,
                addAction,
                removeAction,
                canRemove
            };
            CQFEditorBridge.Invoke("QuestEditor_Library.CustomMapExit.DrawSectionHeader(Ref:float,None:float,None:float,None:string,None:string,None:System.Action,None:System.Action,None:bool)", this, arguments);
            y = (float)arguments[0];
        }
        private void TriggerEnterActions(Thing thing)
        {
            Dictionary<string, TargetInfo> targets = new Dictionary<string, TargetInfo>
            {
                ["Trigger"] = thing,
                ["CustomThing"] = this
            };
            Quest quest = GameTools.GetQuestFromThing(this);
            foreach (CQFAction action in this.enterActions)
            {
                if (action == null)
                {
                    Log.Error("CQF custom map exit contains a null enter action: " + this.ThingID);
                    continue;
                }
                action.Work(targets, quest);
            }
        }

        [NoTranslate]
        public string exitName = "undefined";
        public CustomMapEntrance entrance;
        public float height;
        public Vector2 scrollPos = Vector2.zero;
        public List<CQFAction> enterActions = new List<CQFAction>();
        public bool thereIsPawnIsEntering = false;
        private CompCustomText textComp = null;
    }
}
