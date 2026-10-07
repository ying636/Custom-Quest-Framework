using RimWorld;
using RimWorld.Planet;
using RimWorld.QuestGen;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace QuestEditor_Library
{
    public static class QuestNode_FindTileOnCoastEditor
    {
        public static void Draw_0(QuestEditor_Library.QuestNode_FindTileOnCoast cqfReceiver, ref float y, Rect inRect, float x)
        {
            using CQFUIScope cqfUIScope = new CQFUIScope(inRect.width);
            CQFEditorTools.DrawLabelAndText_SlateRef_Line(y, "StoreAsText".Translate(), ref cqfReceiver.storeAs, x, 100f);
            y += 30f;
            CQFEditorTools.DrawIntRange(ref y, "MapDistance".Translate(), ref cqfReceiver.distance, ref cqfReceiver.buffer, ref cqfReceiver.bufferMin, x);
            Func<Rot4, string> GetText = r => r == Rot4.Invalid ? "Rot_Invalid".Translate().ToString() : r.ToStringHuman().Translate().ToString();
            if (CQFUIStyle.ButtonText(new Rect(x, y, 350f, 25f), "RequiredRotation".Translate(GetText(cqfReceiver.requiredRot)), false))
            {
                CQFEditorTools.DrawFloatMenu(new List<Rot4>() { Rot4.East, Rot4.West, Rot4.North, Rot4.South, Rot4.Invalid }, r => cqfReceiver.requiredRot = r, r => GetText(r));
            }

            y += 30f;
        }
    }
}
