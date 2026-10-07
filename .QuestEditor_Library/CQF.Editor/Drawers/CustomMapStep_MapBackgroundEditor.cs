using System.Collections.Generic;
using System.Xml.Linq;
using UnityEngine;
using Verse;

namespace QuestEditor_Library
{
    public static class CustomMapStep_MapBackgroundEditor
    {
        public static void Draw_0(QuestEditor_Library.CustomMapStep_MapBackground cqfReceiver, ref float y, Rect inRect, float x)
        {
            using CQFUIScope cqfUIScope = new CQFUIScope(inRect.width);
            cqfReceiver.background ??= new CustomMapBackgroundData();
            cqfReceiver.background.Draw(ref y, inRect, x);
        }
    }
}
