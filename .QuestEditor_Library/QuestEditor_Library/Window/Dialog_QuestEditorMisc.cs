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
                Find.WindowStack.Add(new Dialog_Select<ModMetaData>(new TextSelectDrawer<ModMetaData>(ModLister.AllInstalledMods.ToList(), m => m.Name, m =>
                {
                    string questPath = Path.Combine(m.RootDir.FullName, "Quests");
                    Directory.CreateDirectory(questPath);
                    Directory.CreateDirectory(Path.Combine(questPath, "Map"));
                    Directory.CreateDirectory(Path.Combine(questPath, "Rule"));
                    Directory.CreateDirectory(Path.Combine(questPath, "Group"));
                    Directory.CreateDirectory(Path.Combine(questPath, "DialogTree"));
                    Directory.CreateDirectory(Path.Combine(questPath, "Data"));
                    Page_QuestEditor.modData = m;
                }, null, null, null, null, null, null), "Select".Translate()));
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
