using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using UnityEngine;
using Verse;

namespace QuestEditor_Library
{
	public class CustomContainer : Building_Casket, IDrawTabable, ICustomThing
	{
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
        public override bool CanOpen => 
			base.CanOpen &&
			!this.openingConditions.Exists(c => 
			!c.Satisfied(new Dictionary<string, TargetInfo>() 
			{ ["CustomThing"] = this, ["Inner"] = this.ContainedThing },
				out string r,GameTools.GetQuestFromThing(this)));
        public override int OpenTicks => this.tickToOpen;
		public override void Open()
		{
			Thing t = this.ContainedThing;
			base.Open();
			this.openingActions.ForEach(a => a.Work(new Dictionary<string, TargetInfo>()
			{ ["CustomThing"] = this,["Inner"] = t },GameTools.GetQuestFromThing(this)));
		}
        protected override void DrawAt(Vector3 drawLoc, bool flip = false)
		{
			if (!this.HasAnyContents)
			{
				base.DrawAt(drawLoc, flip);
			}
			else
			{
				Vector3 pos = this.DrawPos;
				this.Front.Draw(pos, Rot4.North, this, 0f);
				if (this.HasAnyContents && this.Extension.showInnerThings)
				{
					Vector3 drawLoc2 = DrawPos + this.Extension.caturedDrawOffset;
					this.ContainedThing.DynamicDrawPhaseAt(DrawPhase.Draw, drawLoc2, false);
				}
				if (this.backGraphic == null)
				{
					ModExtension_CustomThing extension = this.def.GetModExtension<ModExtension_CustomThing>();
					this.backGraphic = extension.captureTrapGraphicdata_Back.GraphicColoredFor(this);
				}
				this.backGraphic.Draw(base.DrawPos - new Vector3(0,1.5f,0), Rot4.North, this, 0f);
			}
		}
		public override IEnumerable<Gizmo> GetGizmos()
		{
			if (DebugSettings.ShowDevGizmos)
			{
				yield return new Command_Action()
				{
					defaultLabel = "DEV:Spawn inner things",
					action = () =>
					{
						this.innerThings.RandomElementByWeight(t => t.chance).SpawnLoots(this.Map, this.InteractionCell, null, this).ForEach(t =>
						{
							t.Rotation = this.def.rotatable ? this.Rotation : Rot4.South;
							t.DeSpawn();
							this.TryAcceptThing(t);
							t.Rotation = this.def.rotatable ? this.Rotation : Rot4.South;
						});
					}
				};
			}
			yield break;
		}
		public CustomThingData GetData(IntVec3 pos)
		{
			return new CustomThingData_CustomContainer(this, pos);
        }

        public void DrawTab()

        {
            object[] arguments = new object[]
            {
            };
            CQFEditorBridge.Invoke("QuestEditor_Library.CustomContainer.DrawTab()", this, arguments);
        }
        public override void ExposeData()
		{
			base.ExposeData();
			Scribe_Values.Look(ref this.tickToOpen, "tickToOpen");
			Scribe_Values.Look(ref this.buffer, "buffer");
			Scribe_Collections.Look(ref this.innerThings, "innerThings", LookMode.Deep);
			Scribe_Collections.Look(ref this.openingActions, "openingActions", LookMode.Deep);
			Scribe_Collections.Look(ref this.openingConditions, "openingConditions", LookMode.Deep);
		}

		public float height;
		public Vector2 pos = Vector2.zero;
		public string buffer;
        public int tickToOpen = 100;
		public List<LootData> innerThings = new List<LootData>();
		public List<CQFAction> openingActions = new List<CQFAction>();
		public List<DialogCondition> openingConditions = new List<DialogCondition>();
		[Unsaved(false)]
		private Graphic backGraphic;
		private Graphic frontGraphic;
	}
}
