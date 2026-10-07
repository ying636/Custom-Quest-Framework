using UnityEngine;
using Verse;

namespace QuestEditor_Library
{
    public static class CQFAIButton
    {
        public static bool Draw(Rect rect, bool framed = false)
        {
            return CQFAIIconButton.DrawImage(rect, ContentFinder<Texture2D>.Get("UI/QuestEditor/DialogManager"), "CQF_AI_OpenHint".Translate(), framed);
        }
    }
}
