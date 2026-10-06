using System;
using UnityEngine;
using Verse;

namespace QuestEditor_Library
{
    public sealed class Dialog_CQFSignalBookConfiguration : Window
    {
        public Dialog_CQFSignalBookConfiguration(string titleKey, Action<string> accepted)
        {
            this.titleKey = titleKey;
            this.accepted = accepted;
            this.doCloseX = true;
            this.draggable = true;
            this.forcePause = true;
            this.absorbInputAroundWindow = true;
            this.closeOnAccept = false;
        }

        public override Vector2 InitialSize => new Vector2(360f, 200f);

        protected override float Margin => 8f;

        public override void DoWindowContents(Rect inRect)
        {
            bool accept = UnityEngine.Event.current.type == EventType.KeyDown
                && (UnityEngine.Event.current.keyCode == KeyCode.Return || UnityEngine.Event.current.keyCode == KeyCode.KeypadEnter);
            if (accept)
            {
                UnityEngine.Event.current.Use();
            }
            Text.Font = GameFont.Medium;
            Widgets.Label(new Rect(0f, 0f, inRect.width - 35f, 32f), this.titleKey.Translate());
            Text.Font = GameFont.Small;
            Widgets.Label(new Rect(0f, 40f, inRect.width, 24f), "CQF_SignalBook_ConfigurationName".Translate());
            GUI.SetNextControlName("CQF_SignalBook_ConfigurationName");
            this.name = Widgets.TextField(new Rect(0f, 68f, inRect.width, 30f), this.name);
            if (!this.focused)
            {
                UI.FocusControl("CQF_SignalBook_ConfigurationName", this);
                this.focused = true;
            }
            if (this.error != null)
            {
                Widgets.Label(new Rect(0f, 104f, inRect.width, 42f), this.error.Colorize(Color.red));
            }
            if (Widgets.ButtonText(new Rect(0f, inRect.height - 32f, inRect.width, 32f), "AcceptButton".Translate()) || accept)
            {
                try
                {
                    this.accepted(this.name);
                    this.Close();
                }
                catch (Exception exception)
                {
                    this.error = exception.Message.CanTranslate() ? exception.Message.Translate().ToString() : exception.Message;
                    CQFSignalBook.ReportError(exception);
                }
            }
        }

        private readonly string titleKey;
        private readonly Action<string> accepted;
        private string name = string.Empty;
        private string? error;
        private bool focused;
    }
}
