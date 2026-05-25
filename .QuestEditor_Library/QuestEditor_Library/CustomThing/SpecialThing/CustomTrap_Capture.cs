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
    public class CustomTrap_Capture : CustomTrap,IThingHolder
	{
		public CustomTrap_Capture()
		{
			this.innerContainer = new ThingOwner<Pawn>(this, false, LookMode.Deep);
		}
		public bool HasPawn => this.innerContainer.Any();
		public Pawn Pawn => (Pawn)this.innerContainer.First();
		public ModExtension_CustomThing Extension => this.def.GetModExtension<ModExtension_CustomThing>();
		public Graphic Front 
		{
			get
			{
				if (this.frontGraphic == null)
				{
					this.frontGraphic = this.Extension.captureTrapGraphicdata_Front.GraphicColoredFor(this);
				}
				return this.frontGraphic;
			}
		}
        public override void Draw(ref float y, Rect inRect, float x)
        {
			CQFEditorTools.DrawLabelAndText_Line(y, "TrapName".Translate(), ref this.trapName, x, 250f);
			y += 30f;
			CQFEditorTools.DrawLabelAndText_Line(y, "DisarmReport".Translate(),ref this.disarmReport,x,100f);
			y += 30f; 
			CQFEditorTools.DrawLabelAndText_Line(y, "TickToDisarm".Translate(), ref this.tickToDisarm,ref this.buffer,x, 100f);
			y += 30f;
			CQFEditorTools.DrawActionList(ref y,x,this.disarmActions,inRect, "DisarmActions".Translate().Colorize(ColorLibrary.SkyBlue));
			y += 30f;
			CQFEditorTools.DrawIDrawList_UseWindow(ref y, x, this.trapComps,
				inRect, "TrapComps".Translate().Colorize(ColorLibrary.LightBlue), () =>
				{
					CQFEditorTools.DrawFloatMenu(new List<ActionTriggerMode>() { ActionTriggerMode.Signal, ActionTriggerMode.StepOn, ActionTriggerMode.Tick }, m => this.trapComps.Add(new TrapComp() { mode = m }), m => ("ActionTriggerMode_" + m.ToString()).Translate());

				}, c => ("ActionTriggerMode_" + c.mode.ToString()).Translate());

		}
        public override void DynamicDrawPhaseAt(DrawPhase phase, Vector3 drawLoc, bool flip = false)
		{
			if (this.HasPawn)
			{
				Vector3 drawLoc2 = base.Position.ToVector3ShiftedWithAltitude(AltitudeLayer.BuildingOnTop) + this.Extension.caturedDrawOffset;
				this.Pawn.Drawer.renderer.wiggler.SetToCustomRotation(this.pawnRotation);
				this.Pawn.DynamicDrawPhaseAt(phase, drawLoc2, false);
			}
			base.DynamicDrawPhaseAt(phase, drawLoc, flip);
		}
		protected override void DrawAt(Vector3 drawLoc, bool flip = false)
		{
			if (!this.HasPawn)
			{
				base.DrawAt(drawLoc, flip);
			}
			else
			{
				Vector3 pos = this.Position.ToVector3ShiftedWithAltitude(AltitudeLayer.Item);
				this.Front.Draw(pos, Rot4.North, this, 0f);
				if (this.backGraphic == null)
				{
					ModExtension_CustomThing extension = this.def.GetModExtension<ModExtension_CustomThing>();
					this.backGraphic = extension.captureTrapGraphicdata_Back.GraphicColoredFor(this);
				}
				this.backGraphic.Draw(base.Position.ToVector3ShiftedWithAltitude(AltitudeLayer.Item) + new Vector3(0, -0.5f, 0), Rot4.North, this, 0f);
			}
		}
		public override void Notify_PawnStepOn(Pawn pawn)
		{
			base.Notify_PawnStepOn(pawn);
			if (!this.HasPawn && !pawn.Dead)
			{
				if (pawn.Spawned) 
				{
					pawn.DeSpawn();
				}
				this.innerContainer.TryAddOrTransfer(pawn);
				this.pawnRotation = this.Extension.pawnAngle.RandomInRange;
			}
		}
		public void Disarm(Pawn disarmer) 
		{
			if (this.HasPawn) 
			{
				Pawn pawn = (Pawn)GenSpawn.Spawn(this.Pawn,this.Position,this.Map);
				Dictionary<string, TargetInfo> t = this.GetTargetThis();
				t.Add("Captured",pawn); 
				t.Add("Trigger", disarmer);
				this.disarmActions.ForEach(a => a.Work(t,GameTools.GetQuestFromThing(this)));
				this.Destroy();
			}
		}
        public void GetChildHolders(List<IThingHolder> outChildren)
        {
			ThingOwnerUtility.AppendThingHoldersFromThings(outChildren, this.GetDirectlyHeldThings());
		}

        public ThingOwner GetDirectlyHeldThings()
        {
			return this.innerContainer;
		}
		public override IEnumerable<FloatMenuOption> GetFloatMenuOptions(Pawn selPawn)
		{
			foreach (FloatMenuOption option in base.GetFloatMenuOptions(selPawn))
			{
				yield return option;
			}
			if (this.HasPawn)
			{
				if (selPawn.CanReserveAndReach(this, PathEndMode.Touch, Danger.Deadly))
				{
					Job job = JobMaker.MakeJob(QEDefOf.QE_DisarmTrap, this);
					job.reportStringOverride = this.disarmReport.Translate(this.Pawn.Name.ToString());
					yield return new FloatMenuOption(this.disarmReport.Translate(), () =>
					{
						selPawn.jobs.StopAll();
						selPawn.jobs.StartJob(job);
					});
				}
				else
				{
					yield return new FloatMenuOption("CantReseverveOrReachLootBox".Translate(), null);
				}
			}
			yield break;
		}
		public override void ExposeData()
		{
			base.ExposeData();
			Scribe_Collections.Look(ref this.disarmActions, "disarmActions",LookMode.Deep);
			Scribe_Deep.Look<ThingOwner>(ref this.innerContainer, "innerContainer", new object[]
			{
				this
			});
			Scribe_Values.Look<float>(ref this.pawnRotation, "pawnRotation", 0f, false);
			Scribe_Values.Look(ref this.buffer, "buffer");
			Scribe_Values.Look(ref this.disarmReport, "disarmReport");
			Scribe_Values.Look(ref this.tickToDisarm, "tickToDisarm");
		}

		public string buffer;
		public string disarmReport = "DisarmTrap";
		public int tickToDisarm = 100;
		public List<CQFAction> disarmActions = new List<CQFAction>();
		[Unsaved(false)]
		private float pawnRotation = 45f;
		private Graphic backGraphic;
		private Graphic frontGraphic;
		protected ThingOwner innerContainer;
	}
}
