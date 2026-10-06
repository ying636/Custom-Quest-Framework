using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using UnityEngine;
using Verse;

namespace QuestEditor_Library
{
    public static class CompActionWorkerEditor
    {
        public static void PasteSingleComp_0(QuestEditor_Library.CompActionWorker cqfReceiver)
        {
            if (CQFEditorTools.actionComp != null)
            {
                cqfReceiver.comps.Add(CQFEditorTools.actionComp.Copy());
            }
        }
    }
}
