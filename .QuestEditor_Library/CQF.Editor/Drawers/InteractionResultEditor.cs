using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI.Group;
using Verse.AI;
using System.Xml.Linq;
using System.Xml;

namespace QuestEditor_Library
{
    public static class InteractionResultEditor
    {
        public static void Draw_0(QuestEditor_Library.InteractionResult cqfReceiver, ref float y, Rect inRect, float x = 0f)
        {
            using CQFUIScope cqfUIScope = new CQFUIScope(inRect.width);
            CQFInteractionResultEditor.DrawResult(ref y, x, inRect.width - x - 35f, inRect, cqfReceiver);
        }
    }
}
