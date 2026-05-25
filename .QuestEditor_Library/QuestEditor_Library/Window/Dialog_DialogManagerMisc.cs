using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace QuestEditor_Library
{
    public class Dialog_DialogManagerMisc : Window
    {
        public Dialog_DialogManagerMisc(DialogManagerDef manager) 
        {
            this.manager = manager; 
            this.doCloseX = true;
        }
        public override void DoWindowContents(Rect inRect)
        {
            float y = 10f;
            float x = 5f;
            Widgets.BeginScrollView(new Rect(0f, 0f, inRect.width, inRect.height), ref this.pos, new Rect(0f, 0f, inRect.width, this.height + 10f));
            float y2 = y;
            CQFEditorTools.DrawEditableStringList(this.manager.tags,ref y,"Tags".Translate(),null,true,x);
            TraitData.DrawList(this.manager.forcedTraits,ref y2, "ForcedTraits".Translate(),null,true,x + 185f);
            y += 5f;
            string colorText = "QuestIconColor".Translate();
            Rect rect = new Rect(x, y,Text.CalcSize(colorText).x, 25f);
            if (Widgets.ButtonText(rect, colorText,false))
            {
                List<FloatMenuOption> options = new List<FloatMenuOption>();
                options.Add(new FloatMenuOption("Colorbase".Translate(), () =>
                 Find.WindowStack.Add(new Dialog_ChooseColor("Select".Translate(),this.manager.iconColor, (from c in DefDatabase<ColorDef>.AllDefsListForReading
                                                                            select c.color).ToList<Color>(),c => this.manager.iconColor = c))
                ));
                options.Add(new FloatMenuOption("Hex".Translate(), () =>
                Find.WindowStack.Add(new Dialog_RGB(this.manager.iconColor,c => this.manager.iconColor = c))
                ));
                Find.WindowStack.Add(new FloatMenu(options));
            }
            rect.x += rect.width + 5f;
            rect.width = 25f;
            Widgets.ColorBox(rect, ref this.manager.iconColor, this.manager.iconColor);
            y += 30f;
            CQFEditorTools.DrawIDrawList_UseWindow(ref y, x, this.manager.genrationConditions, inRect, "genrationConditions".Translate(), a => a.GetType().Name.Translate());
            Widgets.CheckboxLabeled(new Rect(x,y,250f,25f),"RemoveWhenThingDespawned".Translate(),ref this.manager.removeWhenThingDespawned);
            y += 30f;
            Widgets.CheckboxLabeled(new Rect(x, y, 250f, 25f), "RemoveWhenPawnDied".Translate(), ref this.manager.removeWhenPawnDied);
            Widgets.EndScrollView();
            this.height = y + 5f;
        }
        public string buffer;
        public float height;
        public DialogManagerDef manager;
        public Vector2 pos = Vector2.zero;
    }
}
