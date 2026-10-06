using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace QuestEditor_Library
{
    public static class ITab_CustomThingEditor
    {
        public static void FillTab_0(QuestEditor_Library.ITab_CustomThing cqfReceiver)
        {
            using (new CQFEditorContext(cqfReceiver.Thing as Thing))
            {
                cqfReceiver.Thing?.DrawTab();
            }
        }
    }
}
