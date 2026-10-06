using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace QuestEditor_Library
{
    public static class SpawnerEditor
    {
        public static void DrawTab_0(QuestEditor_Library.Spawner cqfReceiver)
        {
            Widgets.BeginScrollView(new Rect(7f, 25f, 475f, 590f), ref cqfReceiver.scrollPos, new Rect(7f, 10f, 475f, cqfReceiver.height));
            float y = 10f;
            float initY = y;
            foreach (PawnSpawnData pawnData in cqfReceiver.pawns)
            {
                Rect rectData = new Rect(17f, y + 3f, 450f, 25f);
                if (Widgets.ButtonText(rectData, pawnData.dataName, false))
                {
                    Find.WindowStack.Add(new Dialog_EditIDrawable(pawnData));
                }

                y += 30f;
            }

            Widgets.DrawBox(new Rect(7f, initY, 350f, y - initY), 1, QuestEditor_Dialog.blueTex);
            y += 10f;
            CQFEditorTools.DrawButtonForPawnData(y, cqfReceiver.pawns);
            y += 40f;
            cqfReceiver.height = y + 15f;
            Widgets.EndScrollView();
        }
    }
}
