using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using UnityEngine;
using Verse;

namespace QuestEditor_Library
{
    public class Window_StringAndChance : Window
    {
        public Window_StringAndChance(Action<string, float> action) 
        {
            this.action = action;
        }
        public override Vector2 InitialSize => new Vector2(250f, 250f);
        public override void DoWindowContents(Rect inRect)
        {
            if (Widgets.CloseButtonFor(inRect))
            {
                this.Close();
            }
            float y = 10f;
            this.data = Widgets.TextField(new Rect(5f, y, 150f, 25f),this.data);
            y += 30f;
            Widgets.Label(new Rect(5f, y, 150f, 25f), "chance".Translate());
            y += 30f;
            Widgets.TextFieldPercent(new Rect(5f, y, 150f, 25f), ref this.chance, ref this.buffer);
            y += 40f;
            if (Widgets.ButtonText(new Rect(5f, y, 150f, 35f), "OK".Translate()) && this.data != null)
            {
                this.action(this.data, this.chance);
                this.Close();
            }
        }

        public string data;
        public string buffer;
        public Action<string, float> action;
        public float chance = 1f;
    }
}
