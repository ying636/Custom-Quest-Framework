using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace QuestEditor_Library
{
    public class Dialog_RGB : Window
    {
        public Dialog_RGB(Color cur,Action<Color> apply) 
        {
            this.curColor = cur;
            this.apply = apply;
        }
        public override Vector2 InitialSize => new Vector2(500f,225f);
        public override void DoWindowContents(Rect inRect)
        {
            Rect rect = new Rect(5f, 3f, 50f, 50f);
            Widgets.ColorBox(rect, ref this.curColor, this.curColor,46);
            TooltipHandler.TipRegion(rect, new TipSignal(this.curColor.ToString()));
            float y = 60f;
            CQFEditorTools.DrawLabelAndText_Line(y, "Hex:", ref this.hex,5f, 150f);
            if (Widgets.ButtonText(new Rect(200f, y, Window.CloseButSize.x, Window.CloseButSize.y), "OK".Translate(), true, true, true, null))
            {
                if (this.hex != null && this.hex.Length >= 6)
                {
                    string hex = this.hex;
                    ColorUtility.TryParseHtmlString(hex,out this.curColor);
                }
            }
            if (Widgets.ButtonText(new Rect(inRect.x, inRect.height - Window.CloseButSize.y, Window.CloseButSize.x, Window.CloseButSize.y), "CloseButton".Translate(), true, true, true, null))
            {
                this.Close(true);
            }
            if (Widgets.ButtonText(new Rect(inRect.width - Window.CloseButSize.x, inRect.height - Window.CloseButSize.y, Window.CloseButSize.x, Window.CloseButSize.y), "OK".Translate(), true, true, true, null))
            {
                Action<Color> action = this.apply;
                if (action != null)
                {
                    action(this.curColor);
                }
                this.Close(true);
            }
        }

        public Action<Color> apply;
        public string hex = "";
        public float r = 0;
        public float g = 0;
        public float b = 0;
        public float a = 0;
        public Color curColor = Color.white;
    }
}
