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
            Rect outRect = new Rect(0f, 0f, 540f, 590f);
            float width = outRect.width - 40f;
            Rect viewRect = new Rect(0f, 0f, outRect.width - 20f, Mathf.Max(outRect.height, this.height + 10f));
            Widgets.BeginScrollView(outRect, ref this.scrollPos, viewRect);
            float x = 10f;
            float y = 10f;

            this.DrawSectionHeader(ref y, x, width, "CQF_PortalSettingsSection".Translate(), "CQF_PortalSettingsSectionTip".Translate());
            CQFEditorTools.DrawLabelAndText_Line(y, "ExitName".Translate(), ref this.exitName, x + 8f, 150f);
            y += 30f;

            this.DrawActionSection(ref y, x, width);
            this.height = y + 10f;
            Widgets.EndScrollView();
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

        private void DrawActionSection(ref float y, float x, float width)
        {
            this.DrawSectionHeader(ref y, x, width, "CQF_PortalEnterActions".Translate(), "CQF_PortalEnterActionsTip".Translate(),
                () => CQFEditorTools.OpenCQFActionSelect(type => this.enterActions.Add((CQFAction)Activator.CreateInstance(type))),
                () => CQFEditorTools.DrawFloatMenu(this.enterActions, action => this.enterActions.Remove(action), action => action.GetType().Name.Translate()),
                this.enterActions.Any());
            if (this.enterActions.Any())
            {
                foreach (CQFAction action in this.enterActions)
                {
                    Rect rowRect = new Rect(x + 8f, y, width - 16f, 28f);
                    Widgets.DrawHighlightIfMouseover(rowRect);
                    if (Widgets.ButtonText(rowRect, action.GetType().Name.Translate(), false))
                    {
                        Find.WindowStack.Add(new Dialog_EditIDrawable(action));
                    }
                    y += 32f;
                }
            }
            else
            {
                Widgets.Label(new Rect(x + 8f, y + 4f, width - 16f, 25f), "CQF_PortalNoActions".Translate().Colorize(Color.gray));
                y += 32f;
            }
            y += 8f;
        }

        private void DrawSectionHeader(ref float y, float x, float width, string label, string tip = null,
            Action addAction = null, Action removeAction = null, bool canRemove = false)
        {
            Rect headerRect = new Rect(x + 4f, y - 2f, width - 8f, 32f);
            Widgets.DrawHighlight(headerRect);
            Rect labelRect = new Rect(x + 8f, y + 4f, width - 84f, 25f);
            Widgets.Label(labelRect, label.Colorize(ColorLibrary.SkyBlue));
            if (!tip.NullOrEmpty())
            {
                TooltipHandler.TipRegion(labelRect, tip);
            }
            if (addAction != null)
            {
                Rect buttonRect = new Rect(x + width - 66f, y + 2f, 25f, 25f);
                if (Widgets.ButtonImage(buttonRect, TexButton.Plus))
                {
                    addAction();
                }
                TooltipHandler.TipRegion(buttonRect, "Add".Translate());
                buttonRect.x += 30f;
                if (Widgets.ButtonImage(buttonRect, TexButton.Delete) && canRemove)
                {
                    removeAction?.Invoke();
                }
                TooltipHandler.TipRegion(buttonRect, "Remove".Translate());
            }
            y += 38f;
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
