using RimWorld;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace QuestEditor_Library
{
    public class Dialog_QuestEditorMisc : Window
    {
        public Dialog_QuestEditorMisc() 
        {
            this.doCloseX = true;
        }
        public override void DoWindowContents(Rect inRect)
        {
            float y = 10f;
            float x = 5f;
            Widgets.BeginScrollView(new Rect(0f, 0f, inRect.width, inRect.height), ref this.pos, new Rect(0f, 0f, inRect.width, this.height + 10f));
            Rect output = new Rect(x, y, 520f, 30f);
            if (Widgets.ButtonText(output, "SelectOutputMod".Translate(Page_QuestEditor.ModData.Name),false)) 
            {
                Find.WindowStack.Add(new Dialog_Select<ModMetaData>(ModLister.AllInstalledMods.ToList(),null,m => m.Name,"Select".Translate(),m =>
                {
                    string path = m.RootDir.FullName;
                    if (!Directory.Exists(path + @"\Quests"))
                    {
                        Directory.CreateDirectory(path + @"\Quests");
                    }
                    string questPath = path + @"\Quests";
                    if (!Directory.Exists(questPath + @"\Map"))
                    {
                        Directory.CreateDirectory(questPath + @"\Map");
                    }
                    if (!Directory.Exists(questPath + @"\Rule"))
                    {
                        Directory.CreateDirectory(questPath + @"\Rule");
                    }
                    if (!Directory.Exists(questPath + @"\Group"))
                    {
                        Directory.CreateDirectory(questPath + @"\Group");
                    }
                    if (!Directory.Exists(questPath + @"\DialogTree"))
                    {
                        Directory.CreateDirectory(questPath + @"\DialogTree");
                    }
                    if (!Directory.Exists(questPath + @"\Data"))
                    {
                        Directory.CreateDirectory(questPath + @"\Data");
                    }
                    Page_QuestEditor.modData = m;
                }));
            }
            TooltipHandler.TipRegion(output, "OutputModTip".Translate());

            Widgets.EndScrollView();
            this.height = y + 5f;
        }
        public string buffer;
        public float height;
        public Vector2 pos = Vector2.zero;
    }
}
