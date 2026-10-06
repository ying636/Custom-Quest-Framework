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
using Verse.AI;
using Verse.AI.Group;

namespace QuestEditor_Library
{
    public static class PawnSpawnData_FactionEditor
    {
        public static void DrawKind_0(QuestEditor_Library.PawnSpawnData_Faction cqfReceiver, float x, ref float y)
        {
            Rect rect = new Rect(20f + x, y, 250f, 25f);
            if (Widgets.ButtonText(rect, "CQF_PawnGroupMaker".Translate(cqfReceiver.kindDef?.defName), false) && !cqfReceiver.faction.NullOrEmpty() && FactionDef.Named(cqfReceiver.faction)is FactionDef factionDef && !factionDef.pawnGroupMakers.NullOrEmpty())
            {
                CQFEditorTools.DrawFloatMenu(factionDef.pawnGroupMakers, (k) => cqfReceiver.kindDef = k.kindDef, (k) =>
                {
                    return k.kindDef.defName + ":" + k.commonality;
                });
            }

            TooltipHandler.TipRegion(rect, "CQF_PawnGroupMaker_Tip".Translate());
            y += 30f;
            CQFEditorTools.DrawIntRange(ref y, "SpawmPoint".Translate(), ref cqfReceiver.point, ref cqfReceiver.buffer1, ref cqfReceiver.buffer2, x + 20f, 80f);
        }
    }
}
