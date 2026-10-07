using System.Xml.Linq;
using UnityEngine;
using Verse;

namespace QuestEditor_Library
{
    public static class CustomMapStep_MapPartEditor
    {
        public static void Draw_0(QuestEditor_Library.CustomMapStep_MapPart cqfReceiver, ref float y, Rect inRect, float x)
        {
            using CQFUIScope cqfUIScope = new CQFUIScope(inRect.width);
            Widgets.Label(new Rect(x, y, inRect.width - x - 12f, 30f), "CustomMapStep_MapPart".Translate().Colorize(CQFUIStyle.Accent));
            y += 35f;
            CQFEditorTools.DrawIntRange(ref y, "GenerationCount".Translate(), ref cqfReceiver.count, ref cqfReceiver.buffer, ref cqfReceiver.buffer2, x, 60f);
            cqfReceiver.set.Draw(ref y, inRect, x);
        }
    }
}
