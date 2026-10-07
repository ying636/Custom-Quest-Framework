using UnityEngine;
using Verse;

namespace QuestEditor_Library
{
    public sealed class CQFAISettingsWindow : Window
    {
        public CQFAISettingsWindow()
        {
            layer = WindowLayer.Super;
            draggable = true;
            doCloseX = true;
            closeOnAccept = false;
            absorbInputAroundWindow = false;
            preventCameraMotion = false;
        }

        public override Vector2 InitialSize => new Vector2(Mathf.Min(UI.screenWidth - 40f, 600f), Mathf.Min(UI.screenHeight - 40f, 600f));

        public override void DoWindowContents(Rect inRect)
        {
            GameFont previousFont = Text.Font;
            TextAnchor previousAnchor = Text.Anchor;
            bool previousWrap = Text.WordWrap;
            Text.Font = GameFont.Medium;
            Text.Anchor = TextAnchor.UpperLeft;
            Text.WordWrap = true;
            try
            {
                Widgets.Label(new Rect(0f, 0f, inRect.width - 30f, 30f), "CQF_DialogAI_SettingsTab".Translate());
                Text.Font = GameFont.Small;
                Rect body = new Rect(0f, 38f, inRect.width, inRect.height - 38f);
                settings.Draw(body, CustomQuestFramework_ModSetting.setting);
            }
            finally { Text.Font = previousFont; Text.Anchor = previousAnchor; Text.WordWrap = previousWrap; }
        }

        public override void PostClose()
        {
            settings.Cancel();
            try { CustomQuestFramework_ModSetting.setting.Write(); }
            catch (Exception error) { Log.Error("CQF AI settings: " + error); Messages.Message("CQF_DialogGraph_Error".Translate(error.Message), RimWorld.MessageTypeDefOf.RejectInput); }
            base.PostClose();
        }

        private readonly CQFDialogAISettings settings = new CQFDialogAISettings();
    }
}
