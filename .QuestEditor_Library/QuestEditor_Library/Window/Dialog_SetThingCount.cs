using System;
using RimWorld;
using UnityEngine;
using Verse;

namespace QuestEditor_Library
{
    public class Dialog_SetThingCount : Window
    {
        public Dialog_SetThingCount(Action<int> confirmAction, int startingValue)
        {
            this.confirmAction = confirmAction;
            this.value = Mathf.Clamp(startingValue, MinValue, MaxValue);
            this.valueBuffer = this.value.ToString();
            this.forcePause = true;
            this.closeOnClickedOutside = true;
        }

        public override Vector2 InitialSize => new Vector2(300f, 190f);

        public override void DoWindowContents(Rect inRect)
        {
            GameFont oldFont = Text.Font;
            TextAnchor oldAnchor = Text.Anchor;
            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.UpperCenter;
            Widgets.Label(new Rect(inRect.x, inRect.y, inRect.width, 28f), "CQF_SetThingCountCustomPrompt".Translate(this.value));
            Text.Anchor = TextAnchor.UpperLeft;

            Rect inputRect = new Rect(inRect.x + 55f, inRect.y + 34f, inRect.width - 110f, 30f);
            GUI.SetNextControlName("CQFThingCountField");
            Widgets.TextFieldNumeric(inputRect, ref this.value, ref this.valueBuffer, MinValue, MaxValue);
            if (!this.focusedInput)
            {
                UI.FocusControl("CQFThingCountField", this);
                this.focusedInput = true;
            }

            Rect sliderRect = new Rect(inRect.x, inRect.y + 76f, inRect.width, 24f);
            int sliderValue = (int)Widgets.HorizontalSlider(sliderRect, this.value, MinValue, MaxValue, middleAlignment: true);
            if (sliderValue != this.value)
            {
                this.value = sliderValue;
                this.valueBuffer = this.value.ToString();
            }

            GUI.color = ColoredText.SubtleGrayColor;
            Text.Font = GameFont.Tiny;
            Rect rangeLabelRect = new Rect(inRect.x, sliderRect.yMax + 2f, inRect.width, Text.LineHeight);
            Widgets.Label(new Rect(rangeLabelRect.x, rangeLabelRect.y, rangeLabelRect.width / 2f, rangeLabelRect.height), MinValue.ToString());
            Text.Anchor = TextAnchor.UpperRight;
            Widgets.Label(new Rect(rangeLabelRect.x + rangeLabelRect.width / 2f, rangeLabelRect.y, rangeLabelRect.width / 2f, rangeLabelRect.height), MaxValue.ToString());
            Text.Anchor = TextAnchor.UpperLeft;
            GUI.color = Color.white;

            float buttonWidth = (inRect.width - 10f) / 2f;
            if (Widgets.ButtonText(new Rect(inRect.x, inRect.yMax - 30f, buttonWidth, 30f), "CancelButton".Translate()))
            {
                this.Close();
            }
            if (Widgets.ButtonText(new Rect(inRect.x + buttonWidth + 10f, inRect.yMax - 30f, buttonWidth, 30f), "OK".Translate()))
            {
                this.Close();
                this.confirmAction(this.value);
            }

            Text.Font = oldFont;
            Text.Anchor = oldAnchor;
        }

        private const int MinValue = 1;
        private const int MaxValue = 9999;

        private readonly Action<int> confirmAction;
        private int value;
        private string valueBuffer;
        private bool focusedInput;
    }
}
