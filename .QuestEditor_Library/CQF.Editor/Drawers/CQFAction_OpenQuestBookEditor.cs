using System.Collections.Generic;
using RimWorld;
using RimWorld.QuestGen;
using UnityEngine;
using Verse;

namespace QuestEditor_Library
{
    public static class CQFAction_OpenQuestBookEditor
    {
        public static void Draw_0(QuestEditor_Library.CQFAction_OpenQuestBook cqfReceiver, ref float y, Rect inRect, float x)
        {
            CQFActionEditor.Draw_0(cqfReceiver, ref y, inRect, x);
            Rect descriptionRect = new Rect(x, y, Mathf.Max(280f, inRect.width - x - 12f), 38f);
            Widgets.DrawMenuSection(descriptionRect);
            Widgets.Label(new Rect(descriptionRect.x + 12f, descriptionRect.y + 9f, descriptionRect.width - 24f, 22f), "CQF_QuestBook_ActionOpenDescription".Translate().Colorize(ColorLibrary.PaleBlue));
            y += descriptionRect.height + 8f;
        }
    }
}
