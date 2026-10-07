using UnityEngine;
using Verse;

namespace QuestEditor_Library
{
    public sealed class CQFAIRenameSessionWindow : Window
    {
        public CQFAIRenameSessionWindow(string name, Action<string> apply)
        {
            this.name = name; this.apply = apply; doCloseX = true; closeOnAccept = false; forcePause = false;
        }
        public override Vector2 InitialSize => new Vector2(360f, 150f);
        public override void DoWindowContents(Rect rect)
        {
            GameFont previousFont = Text.Font;
            TextAnchor previousAnchor = Text.Anchor;
            bool previousWrap = Text.WordWrap;
            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.UpperLeft;
            Text.WordWrap = true;
            try
            {
                Widgets.Label(new Rect(0f, 0f, rect.width - 28f, 26f), "CQF_AI_RenameChat".Translate());
                name = Widgets.TextField(new Rect(0f, 35f, rect.width, 30f), name, 100);
                bool enabled = GUI.enabled;
                GUI.enabled = enabled && !string.IsNullOrWhiteSpace(name);
                if (CQFAIIconButton.DrawText(new Rect(0f, rect.height - 32f, rect.width, 32f), "Confirm".Translate())) { apply(name.Trim()); Close(); }
                GUI.enabled = enabled;
            }
            finally { Text.Font = previousFont; Text.Anchor = previousAnchor; Text.WordWrap = previousWrap; }
        }
        private readonly Action<string> apply;
        private string name;
    }
}
