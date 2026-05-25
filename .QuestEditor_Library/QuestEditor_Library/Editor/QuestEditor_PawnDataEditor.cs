using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using UnityEngine;
using Verse;

namespace QuestEditor_Library
{
    [StaticConstructorOnStartup]
    public class QuestEditor_PawnDataEditor : Page
    {
        public ComplexPawnData CurPawn => QuestEditor_PawnDataEditor.pawnData;
        public override string PageTitle => "PawnDataEditor".Translate();
        public override void DoWindowContents(Rect inRect)
        {
            base.DrawPageTitle(inRect);
            if (Widgets.CloseButtonFor(inRect))
            {
                this.Close();
            }
            float y = 50f;
            CQFEditorTools.DrawFieldAndText(ref y, "DataName".Translate(), ref this.CurPawn.dataName);
            y += 30f;
            Widgets.CheckboxLabeled(new Rect(0f, y, 130f, 25f), "IsUnique".Translate(), ref this.CurPawn.unique);
            y += 30f;
            Rect nameRect = new Rect(0f, y, 80f, 25f);
            string name = "Name".Translate();
            Widgets.Label(nameRect, name);
            nameRect.x += Text.CalcSize(name).x + 5f;
            this.CurPawn.firstName = Widgets.TextField(nameRect, this.CurPawn.firstName);
            nameRect.x += nameRect.width + 5f;
            Widgets.Label(nameRect, "·");
            nameRect.x +=  5f;
            this.CurPawn.lastName = Widgets.TextField(nameRect, this.CurPawn.lastName);
            nameRect.x += nameRect.width + 5f;
            Widgets.Label(nameRect, "·");
            nameRect.x += 5f;
            this.CurPawn.nickName = Widgets.TextField(nameRect, this.CurPawn.nickName);
            nameRect.x += nameRect.width + 5f;
            y += 30f;
            Rect rect = new Rect(0f, y, 120f, 100f);
            StringBuilder text = new StringBuilder();
            if (this.CurPawn.bodyType is BodyTypeDef body && QuestEditor_PawnDataEditor.body != null) 
            {
                GUI.color = (Color)this.CurPawn.skinColor;
                Widgets.DrawTextureFitted(new Rect(0f, y + 10f, 120f, 150f), QuestEditor_PawnDataEditor.body, 1f);
                GUI.color = Color.white;
                text.AppendLine("CurBodyType".Translate(body.defName));
            }
            if (this.CurPawn.head is HeadTypeDef head && QuestEditor_PawnDataEditor.head != null)
            {
                GUI.color =  (Color)this.CurPawn.skinColor;
                Widgets.DrawTextureFitted(new Rect(0f, y - 3f, 120f, 110f), QuestEditor_PawnDataEditor.head, 1f);
                GUI.color = Color.white;
                text.AppendLine("CurHeadType".Translate(head.defName));
            }
            if (this.CurPawn.hair is HairDef def)
            {
                GUI.color = (Color)this.CurPawn.hairColor;
                Widgets.DefIcon(rect, def,null,1,null,false, this.CurPawn.hairColor == null ? Color.white : (Color)this.CurPawn.hairColor);
                GUI.color = Color.white;
                text.AppendLine("CurHair".Translate(def.label));
            }
            TooltipHandler.TipRegion(rect, new TipSignal(text.ToString().Trim()));
            if (Widgets.ButtonText(new Rect(200f, y, 130f, 25f), "SelectHair".Translate()))
            {
                CQFEditorTools.DrawFloatMenu<HairDef>(DefDatabase<HairDef>.AllDefs.ToList(), (h) => this.CurPawn.hair = h, (h) => h.label);
            }
            this.DrawSelectColorButtons(ref y, "SelectHairColor".Translate(), this.CurPawn.hairColor ?? Color.white, (c) => this.CurPawn.hairColor = c,350f);
            y += 30f;          
            this.DrawSelectColorButtons(ref y, "SelectSkinColor".Translate(), this.CurPawn.skinColor ?? Color.white, (c) => this.CurPawn.skinColor = c,350f);
            if (Widgets.ButtonText(new Rect(200f, y, 130f, 25f), "SelectHeadType".Translate()))
            {
                CQFEditorTools.DrawFloatMenu<HeadTypeDef>(DefDatabase<HeadTypeDef>.AllDefs.ToList(), (h) =>
                {
                    this.CurPawn.head = h;
                    QuestEditor_PawnDataEditor.head = ContentFinder<Texture2D>.Get(h.graphicPath + "_south");
                }, (h) => h.defName);
            }
            y += 30f;
            if (Widgets.ButtonText(new Rect(200f, y, 130f, 25f), "SelectBodyType".Translate()))
            {
                CQFEditorTools.DrawFloatMenu<BodyTypeDef>(DefDatabase<BodyTypeDef>.AllDefs.ToList(), (h) =>
                {
                    this.CurPawn.bodyType = h;
                    QuestEditor_PawnDataEditor.body = ContentFinder<Texture2D>.Get(h.bodyNakedGraphicPath + "_south");
                }, (h) => h.defName.Translate());
            }
            y += 30f;
        }

        public void DrawSelectColorButtons(ref float y,string label,Color color,Action<Color> apply,float x = 200f) 
        {
            if (Widgets.ButtonText(new Rect(x, y, 130f, 25f), label))
            {
                List<FloatMenuOption> options = new List<FloatMenuOption>();
                options.Add(new FloatMenuOption("Colorbase".Translate(),() =>
                Find.WindowStack.Add(new Dialog_ChooseColor(label, color, (from c in DefDatabase<ColorDef>.AllDefsListForReading
                select c.color).ToList<Color>(),apply))
                ));
                options.Add(new FloatMenuOption("Hex".Translate(), () =>
                Find.WindowStack.Add(new Dialog_RGB(color,apply))
                ));
                Find.WindowStack.Add(new FloatMenu(options));
            }
        }

        public static Texture2D body = null;
        public static Texture2D head = null;
        public static ComplexPawnData pawnData = new ComplexPawnData();
        public static Pawn pawn;
    }
}
