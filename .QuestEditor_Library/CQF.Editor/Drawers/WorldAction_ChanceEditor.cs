using JetBrains.Annotations;
using RimWorld;
using RimWorld.Planet;
using RimWorld.QuestGen;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using UnityEngine.Tilemaps;
using UnityEngine;
using Verse;

namespace QuestEditor_Library
{
    public static class WorldAction_ChanceEditor
    {
        public static void Draw_0(QuestEditor_Library.WorldAction_Chance cqfReceiver, ref float y, Rect inRect, float x)
        {
            using CQFUIScope cqfUIScope = new CQFUIScope(inRect.width);
            WorldActionEditor.Draw_0(cqfReceiver, ref y, inRect, x);
            CQFEditorTools.DrawIDrawList(ref y, x, cqfReceiver.actions.Keys.ToList(), inRect, "TriggerActions".Translate(), () => CQFEditorTools.DrawFloatMenu(typeof(WorldAction).AllSubclassesNonAbstract(), a => cqfReceiver.actions.Add((WorldAction)Activator.CreateInstance(a), 1f), a => a.Name.Translate()), a => a.GetType().Name.Translate());
        }
    }
}
