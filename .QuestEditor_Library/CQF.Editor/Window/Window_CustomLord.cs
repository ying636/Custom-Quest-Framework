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
            using CQFUIScope cqfUIScope = new CQFUIScope(inRect.width);
            Text.Font = GameFont.Medium;
            Widgets.Label(new Rect(0f, 0f, inRect.width - 100f, 36f), "DesignatorLord".Translate().Colorize(CQFUIStyle.Accent));
            Text.Font = GameFont.Small;
            MapComponent_CustomMapData comp = this.map.GetComponent<MapComponent_CustomMapData>();
            float y = 0f;
            Rect button = new Rect(inRect.width - 100f,y,30f,30f);
            if (CQFUIStyle.ButtonImage(button,TexButton.Plus))
            {
                comp.Lords.Add(new LordWithName());
            }
            button.x += 40f;
            if (CQFUIStyle.ButtonImage(button, TexButton.Delete))
            {
               CQFEditorTools.DrawFloatMenu(comp.Lords,l => comp.Lords.Remove(l),l => l.name);
            }
            Rect viewport = new Rect(0f, 44f, inRect.width, inRect.height - 44f);
            Rect content = new Rect(0f, 0f, viewport.width - 20f, Mathf.Max(viewport.height, this.height + 10f));
            Widgets.BeginScrollView(viewport, ref this.pos, content);
            using CQFUIScope contentScope = new CQFUIScope(content.width);
            y = 8f;
            comp.Lords.ForEach(l =>
            {
                l.data?.Draw(ref y, content, 8f);
                y += 12f;
            });
            Widgets.EndScrollView();
            this.height = y + 5f;
        }

        public Map map;
        public float height;
        public Vector2 pos = Vector2.zero;
    }
}
