using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace QuestEditor_Library
{
    public class QuestEditor_PawnDataMisc : Page
    {
        public QuestEditor_PawnDataMisc(PawnSpawnData data) 
        {
            this.data = data;
            this.doCloseX = true;
            this.closeOnClickedOutside = false;
            this.draggable = true;
            this.forcePause = true;
            this.absorbInputAroundWindow = true;
            this.onlyOneOfTypeAllowed = false;
            this.forceCatchAcceptAndCancelEventEvenIfUnfocused = true;
            this.closeOnAccept = false;
            this.closeOnCancel = false;
        }
        public override string PageTitle => "Misc".Translate().Colorize(ColorLibrary.SkyBlue);
        public override Vector2 InitialSize => QuestEditor_PawnDataMisc.size;
        public override void DoWindowContents(Rect inRect)
        {
            Widgets.BeginScrollView(new Rect(0f, 0f, this.InitialSize.x, this.InitialSize.y), ref this.pos, new Rect(0f, 0f, this.InitialSize.x, this.InitialSize.y > height ? this.InitialSize.y : height));
            base.DrawPageTitle(inRect);
            float y = 50f;
            CQFEditorTools.DrawLabelAndText_Line(y, "GenerationChance".Translate(), ref this.data.generationChance, ref this.data.buffer_chance,10f, 150f);
            y += 30f;
            Text.Font = GameFont.Medium;
            Widgets.Label(new Rect(10f, y, 100f, 30f), "PawnHediffs".Translate().Colorize(ColorLibrary.SkyBlue));
            Text.Font = GameFont.Small;
            y += 40f;
            for (int i = 0; i < this.data.hediffs.Count; i++)
            {
                HediffInformation hediff = this.data.hediffs[i];
                if (Widgets.ButtonText(new Rect(10f, y, 150f, 25f), "PawnSpawnDataHediff".Translate(hediff.hediff?.label),false))
                {
                    CQFEditorTools.DrawFloatMenu(DefDatabase<HediffDef>.AllDefsListForReading, x => hediff.hediff = x, x => x.label);
                }
                y += 30f;
                CQFEditorTools.DrawLabelAndText_Line<float>(y, "SeverityOfHediff".Translate(), ref hediff.severity,ref hediff.buffer,10f,80f);
                y += 35f;
                if (this.data.kind != null)
                {
                    if (Widgets.ButtonText(new Rect(10f, y, 150f, 25f), "PawnSpawnDataHediffPart".Translate(hediff.part == null ? "FullBody".Translate().ToString() : hediff.partLabelForSeeing), false))
                    {
                        CQFEditorTools.DrawFloatMenu(this.data.kind.race.race.body.AllParts, x =>
                        {
                            hediff.part = x.def;
                            hediff.partLabel = x.untranslatedCustomLabel;
                            hediff.partLabelForSeeing = x.Label;
                        }, x => x.Label,new List<FloatMenuOption>() {new FloatMenuOption("FullBody".Translate(),() => hediff.part = null)});
                    }
                }
                else 
                {
                    Widgets.Label(new Rect(10f,y,250f,25f),"NoPawnKindToGetBody".Translate());
                }
                y += 30f;
            }
            Rect button = new Rect(10f, y, 100f, 30f);
            if (Widgets.ButtonText(button, "Add".Translate()))
            {
                CQFEditorTools.DrawFloatMenu(DefDatabase<HediffDef>.AllDefsListForReading,(x) =>
                {
                    this.data.hediffs.Add(new HediffInformation(x,null,0.1f,""));
                }, x => x.label);
            }
            button.x += 110f;
            if (Widgets.ButtonText(button, "Delete".Translate()) && this.data.hediffs.Any())
            {
                CQFEditorTools.DrawFloatMenu(this.data.hediffs, (x) =>
                {
                    this.data.hediffs.Remove(x);
                }, x => x.hediff.label);
            }
            y += 35f;
            Widgets.DrawLine(new Vector2(10f,y),new Vector2(inRect.width - 10f,y),ColorLibrary.SkyBlue,1f);
            y += 15f;
            CQFEditorTools.DrawActionList_UseWindow(ref y,10f
                ,this.data.actions,inRect,"PawnSpawnActions".Translate(),a => a.GetType().Name.Translate());
            Text.Font = GameFont.Medium;
            Rect extraKindRect = new Rect(10f, y, 125f, 30f);
            Widgets.Label(extraKindRect, "ExtraKinds".Translate().Colorize(ColorLibrary.SkyBlue));
            Text.Font = GameFont.Small;
            TooltipHandler.TipRegion(extraKindRect, "ExtraKindsTip".Translate());
            y += 35f;
            float width = inRect.width;
            Widgets.DrawBox(new Rect(10f, y, width - 16f,this.heightKinds - y), 1, QuestEditor_Dialog.blueTex);
            y += 5f;
            Rect rectKind = new Rect(15f, y, width, 25f);
            for (int i = 0; i < this.data.extraKinds.Count; i++)
            {
                PawnKindDef kind = this.data.extraKinds[i];
                if (Widgets.ButtonText(rectKind, kind.label, false))
                {
                    CQFEditorTools.DrawFloatMenu<PawnKindDef>(DefDatabase<PawnKindDef>.AllDefs.ToList(), (k) => this.data.extraKinds[i] = k, (k) =>
                    {
                        string result = k.label;
                        return result;
                    });
                }
                y += 30f;
                rectKind.y += 30f;
            }
            y += 5f;
            this.heightKinds = y; 
            y += 10f;
            CQFEditorTools.DrawButtonForList(ref y, this.data.extraKinds, d => d.label, () => CQFEditorTools.DrawFloatMenu<PawnKindDef>(DefDatabase<PawnKindDef>.AllDefs.ToList(), (k) => this.data.extraKinds.Add(k), (k) =>
            {
                string result = k.label;
                return result;
            }));
            if (Widgets.ButtonText(new Rect(10f, y, 150f, 25f), "CurArrivingWay".Translate(this.data.way.GetType().Name.Translate()), false)) 
            {
                List<Type> ts = new List<Type>();
                ts.Add(typeof(ArrivingWay));
                ts.AddRange(typeof(ArrivingWay).AllSubclassesNonAbstract());
                CQFEditorTools.DrawFloatMenu<Type>(ts, (k) => this.data.way = (ArrivingWay)Activator.CreateInstance(k), (k) =>
                {
                    return k.Name.Translate();
                });
            }
            y += 30f;
            this.data.way.Draw(ref y,inRect,10f);
            Widgets.EndScrollView();
            this.height = y;
        }

        public PawnSpawnData data;
        public static Vector2 size = new Vector2(640f,600f);
        public Vector2 pos = Vector2.zero;
        public float height = 0f; 
        public float heightKinds = 0f;
    }
}
