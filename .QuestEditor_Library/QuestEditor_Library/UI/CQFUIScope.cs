using System;
using UnityEngine;
using Verse;

namespace QuestEditor_Library
{
    public sealed class CQFUIScope : IDisposable
    {
        public CQFUIScope(float contentWidth = 0f, float contentHeight = 0f)
        {
            this.contentWidth = ContentWidth;
            this.contentHeight = ContentHeight;
            if (contentWidth > 0f) ContentWidth = contentWidth;
            if (contentHeight > 0f) ContentHeight = contentHeight;
            this.font = Text.Font;
            this.anchor = Text.Anchor;
            this.wordWrap = Text.WordWrap;
            this.color = GUI.color;
            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.UpperLeft;
            Text.WordWrap = true;
        }

        public static float ContentWidth { get; private set; } = 620f;
        public static float ContentHeight { get; private set; } = 620f;

        public void Dispose()
        {
            ContentWidth = this.contentWidth;
            ContentHeight = this.contentHeight;
            Text.Font = this.font;
            Text.Anchor = this.anchor;
            Text.WordWrap = this.wordWrap;
            GUI.color = this.color;
        }

        private readonly GameFont font;
        private readonly float contentWidth;
        private readonly float contentHeight;
        private readonly TextAnchor anchor;
        private readonly bool wordWrap;
        private readonly Color color;
    }
}
