using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using RimWorld;
using UnityEngine;
using Verse;

namespace QuestEditor_Library
{
    public class Window_AddMapWithChance : Window
    {
        public override Vector2 InitialSize => new Vector2(250f,250f);
        public override void DoWindowContents(Rect inRect)
        {
            if (Widgets.CloseButtonFor(inRect)) 
            {
                this.Close();
            }
            float y = 10f;
            Widgets.Label(new Rect(5f,y,150f,25f),"MapData".Translate(this.data));
            y += 30f;
            if (Widgets.ButtonText(new Rect(5f, y, 150f, 35f), "SelectMapData".Translate()))
            {
                CQFEditorTools.DrawFloatMenu(DefDatabase<CustomMapDataDef>.AllDefsListForReading, (x) => this.data = x.defName, (x) => x.label);
            }
            y += 60f;       
            Widgets.Label(new Rect(5f, y, 150f, 25f), "chance".Translate());
            y += 30f;
            Widgets.TextFieldPercent(new Rect(5f, y, 150f, 25f),ref this.chance,ref this.buffer);
            y += 40f;
            if (Widgets.ButtonText(new Rect(5f, y, 150f, 35f), "OK".Translate()) && this.data != null) 
            {
                this.action(this.data,this.chance);
                this.Close();
            }
        }

        public string data;
        public string buffer;
        public Action<string, float> action;
        public float chance = 1f;
    }
}
