using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace QuestEditor_Library
{
    public class Window_CustomLord : Window
    {
        public Window_CustomLord(Map map)
        {
            this.map = map;
            this.doCloseX = true;
        }
        public override Vector2 InitialSize => new Vector2(600f,500f);
        public override void DoWindowContents(Rect inRect)
        {
            Widgets.BeginScrollView(new Rect(0f, 0f, inRect.width, inRect.height), ref this.pos, new Rect(0f, 0f, inRect.width, this.height + 10f));
            Text.Font = GameFont.Medium;
            Widgets.Label(new Rect(0f,0f,900f,45f), "DesignatorLord".Translate().Colorize(ColorLibrary.SkyBlue));
            Text.Font = GameFont.Small;
            MapComponent_CustomMapData comp = this.map.GetComponent<MapComponent_CustomMapData>();
            float y = 0f;
            Rect button = new Rect(inRect.width - 100f,y,30f,30f);
            if (Widgets.ButtonImage(button,TexButton.Plus)) 
            {
                comp.Lords.Add(new LordWithName());
            }
            button.x += 40f;
            if (Widgets.ButtonImage(button, TexButton.Delete))
            {
               CQFEditorTools.DrawFloatMenu(comp.Lords,l => comp.Lords.Remove(l),l => l.name);
            }
            y = 50f;
            comp.Lords.ForEach(l => 
            {
                l.data?.Draw(ref y,inRect,0f);
            });
            Widgets.EndScrollView();
            this.height = y + 5f;
        }

        public Map map; 
        public float height;
        public Vector2 pos = Vector2.zero;
    }
}
