using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using RimWorld.QuestGen;
using UnityEngine;
using Verse;

namespace QuestEditor_Library
{
    public class Dialog_EditQuestNode : Page
    {
        public override string PageTitle => this.node.GetType().Name.Translate().Colorize(ColorLibrary.SkyBlue);
        public override void DoWindowContents(Rect inRect)
        {
            base.DrawPageTitle(inRect);
            if (Widgets.CloseButtonFor(inRect))
            {
                this.Close();
            } 
            float y = 50f;
            string key = this.node.GetHashCode() + "QuestEditor_EditNode";
            Widgets.BeginScrollView(new Rect(4f, 40f, inRect.width - 8f, inRect.height - 83f), ref this.scrollPos, new Rect(0f, 40f, inRect.width - 32f, Page_QuestEditor.drawHeight.ContainsKey(key) ? Page_QuestEditor.drawHeight[key] : 0f));
            Page_QuestEditor.DrawQuestNodeData(this.node, ref y, inRect);
            Widgets.EndScrollView();
            Page_QuestEditor.drawHeight.SetOrAdd(key, y);
        }

        public Vector2 scrollPos = Vector2.zero;
        public QuestNode node;
    }
}
