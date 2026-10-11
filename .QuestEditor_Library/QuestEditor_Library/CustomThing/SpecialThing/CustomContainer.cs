using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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

        public override bool CanOpen => base.CanOpen && !this.openingConditions.Exists(c => !c.Satisfied(new Dictionary<string, TargetInfo>() { ["CustomThing"] = this, ["Inner"] = this.ContainedThing }, out string r, GameTools.GetQuestFromThing(this)));
        public override int OpenTicks => this.tickToOpen;

        public override void Open()
        {
            Thing t = this.ContainedThing;
            base.Open();
            this.openingActions.ForEach(a => a.Work(new Dictionary<string, TargetInfo>() { ["CustomThing"] = this, ["Inner"] = t }, GameTools.GetQuestFromThing(this)));
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

                this.backGraphic.Draw(base.DrawPos - new Vector3(0, 1.5f, 0), Rot4.North, this, 0f);
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
            using CQFUIScope scope = new CQFUIScope(Mathf.Min(500f, CQFUIScope.ContentWidth));
            Rect inRect = new Rect(0f, 0f, CQFUIScope.ContentWidth, Mathf.Min(500f, CQFUIScope.ContentHeight));
            Rect viewRect = new Rect(0f, 0f, inRect.width - 20f, Mathf.Max(inRect.height, this.height + 10f));
            Widgets.BeginScrollView(new Rect(0f, 0f, inRect.width, inRect.height), ref this.pos, viewRect);
            using CQFUIScope contentScope = new CQFUIScope(viewRect.width);
            float x = 10f;
            float y = 15f;
            Widgets.Label(new Rect(x, y, 250f, 25f), "InnerThings".Translate());
            y += 30f;
            Rect rectData = new Rect(x, y + 3f, viewRect.width - x - 12f, 25f);
            foreach (LootData data in this.innerThings)
            {
                if (CQFUIStyle.ButtonText(rectData, data.dataName + "  " + data.chance * 100f + "%", false))
                {
                    Find.WindowStack.Add(CQFEditorBridge.Editor.CreateDrawableWindow(data));
                }

                y += 30f;
                rectData.y += 30f;
            }

            y += 10f;
            float buttonWidth = Mathf.Min(120f, (viewRect.width - x - 28f) / 3f);
            if (CQFUIStyle.ButtonText(new Rect(x, y, buttonWidth, 38f), "AddNewLootData".Translate()))
            {
                this.innerThings.Add(new LootData());
            }

            if (CQFUIStyle.ButtonText(new Rect(x + buttonWidth + 8f, y, buttonWidth, 38f), "Paste".Translate()) && CQFEditorTools.lootData != null)
            {
                this.innerThings.Add(CQFEditorTools.lootData.Copy());
            }

            if (CQFUIStyle.ButtonText(new Rect(x + (buttonWidth + 8f) * 2f, y, buttonWidth, 38f), "DeleteLootData".Translate()) && this.innerThings.Any())
            {
                CQFEditorTools.DrawFloatMenu(this.innerThings, (x2) => this.innerThings.Remove(x2), (x2) => x2.dataName);
            }

            y += 45f;
            CQFEditorTools.DrawLabelAndText_Line(y, "TickToOpen".Translate(), ref this.tickToOpen, ref this.buffer, x, 100f);
            y += 30f;
            CQFEditorTools.DrawActionList(ref y, x, this.openingActions, viewRect, "OpeningActions".Translate().Colorize(CQFUIStyle.Accent), true, "OpeningActionsTip".Translate().ToString());
            Widgets.Label(new Rect(x, y, 150f, 25f), "OpeningConditions".Translate().Colorize(CQFUIStyle.Accent));
            CQFEditorTools.DrawButtonWithIcon(y, () => Find.WindowStack.Add(new Dialog_Select<Type>(new TextSelectDrawer<Type>(typeof(DialogCondition).AllSubclassesNonAbstract(), c => c.Name.Translate(), c => this.openingConditions.Add((DialogCondition)Activator.CreateInstance(c)), null, null, null, null, null, null), "Select".Translate())), () => CQFEditorTools.DrawFloatMenu(this.openingConditions, c => this.openingConditions.Remove(c), c => c.GetType().Name.Translate()), viewRect.width - 76f, 28);
            y += 30f;
            foreach (DialogCondition c in this.openingConditions)
            {
                c.Draw(ref y, viewRect, x);
            }

            this.height = y;
            Widgets.EndScrollView();
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
