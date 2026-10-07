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
        public override string PageTitle => this.node.GetType().Name.Translate().Colorize(CQFUIStyle.Accent);
        public override void DoWindowContents(Rect inRect)
        {
            using CQFUIScope cqfUIScope = new CQFUIScope(inRect.width, inRect.height);
            base.DrawPageTitle(inRect);
            if (Widgets.CloseButtonFor(inRect))
            {
                this.Close();
            }
            float y = 8f;
            string key = this.node.GetHashCode() + "QuestEditor_EditNode";
            Rect viewport = new Rect(4f, 40f, inRect.width - 8f, inRect.height - 48f);
            Rect content = new Rect(0f, 0f, viewport.width - 20f, Mathf.Max(viewport.height, Page_QuestEditor.drawHeight.TryGetValue(key, out float previousHeight) ? previousHeight : 0f));
            Widgets.BeginScrollView(viewport, ref this.scrollPos, content);
            using CQFUIScope contentScope = new CQFUIScope(content.width);
            Page_QuestEditor.DrawQuestNodeData(this.node, ref y, content);
            Widgets.EndScrollView();
            Page_QuestEditor.drawHeight.SetOrAdd(key, y);
        }

        public Vector2 scrollPos = Vector2.zero;
        public QuestNode node;
    }
}
