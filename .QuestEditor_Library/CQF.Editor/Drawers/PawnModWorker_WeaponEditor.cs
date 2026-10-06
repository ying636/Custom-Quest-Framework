using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace QuestEditor_Library
{
    public static class PawnModWorker_WeaponEditor
    {
        public static void Draw_0(QuestEditor_Library.PawnModWorker_Weapon cqfReceiver, ComplexPawnDef pawnDef, ref float y, Rect inRect, float x)
        {
            PawnModData_Weapon modData = pawnDef.DataFor<PawnModData_Weapon>();
            if (modData.weapon == null)
            {
                modData.weapon = new ThingData();
            }

            Rect row = new Rect(x, y, inRect.width - x - 20f, 30f);
            Rect iconRect = new Rect(row.x, row.y + 3f, 24f, 24f);
            if (modData.weapon.def?.uiIcon != null)
            {
                Widgets.DrawTextureFitted(iconRect, modData.weapon.def.uiIcon, 1f);
            }

            if (cqfReceiver.DrawTextButton(new Rect(iconRect.xMax + 8f, row.y, row.width - 120f, 30f), "CQF_PawnEditor_Weapon".Translate(cqfReceiver.ThingLabel(modData.weapon))))
            {
                cqfReceiver.OpenSelectDialog(modData.weapon);
            }

            if (cqfReceiver.DrawCommandText(new Rect(row.xMax - 100f, row.y, 100f, 30f), "CQF_PawnEditor_Delete".Translate()))
            {
                modData.weapon = null;
            }

            cqfReceiver.EndRow(ref y);
        }
    }
}
