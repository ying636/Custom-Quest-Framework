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
            using CQFUIScope scope = new CQFUIScope();
            Rect viewport = new Rect(8f, 36f, Mathf.Min(490f, CQFUIScope.ContentWidth - 16f), Mathf.Max(40f, CQFUIScope.ContentHeight - 44f));
            Rect content = new Rect(0f, 0f, viewport.width - 20f, Mathf.Max(viewport.height, cqfReceiver.height));
            Widgets.BeginScrollView(viewport, ref cqfReceiver.scrollPos, content);
            using CQFUIScope contentScope = new CQFUIScope(content.width);
            float y = 10f;
            float initY = y;
            foreach (PawnSpawnData pawnData in cqfReceiver.pawns)
            {
                Rect rectData = new Rect(12f, y + 3f, content.width - 24f, 25f);
                if (CQFUIStyle.ButtonText(rectData, pawnData.dataName, false))
                {
                    Find.WindowStack.Add(new Dialog_EditIDrawable(pawnData));
                }

                y += 30f;
            }

            CQFUIStyle.DrawBox(new Rect(7f, initY, 350f, y - initY), 1, QuestEditor_Dialog.blueTex);
            y += 10f;
            CQFEditorTools.DrawButtonForPawnData(y, cqfReceiver.pawns);
            y += 40f;
            cqfReceiver.height = y + 15f;
            Widgets.EndScrollView();
        }
    }
}
