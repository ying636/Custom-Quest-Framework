#if RIMWORLD_1_7
using UnityEngine;
using Verse;

namespace QuestEditor_Library
{
    internal sealed class CQFAIExternalWindow : Window
    {
        public CQFAIExternalWindow()
        {
            layer = WindowLayer.Super;
            draggable = true;
            doCloseX = true;
            closeOnAccept = false;
            absorbInputAroundWindow = false;
            preventCameraMotion = false;
        }

        public override Vector2 InitialSize => new Vector2(Mathf.Min(UI.screenWidth - 40f, 500f), 230f);

        public override void DoWindowContents(Rect inRect)
        {
            using CQFUIScope scope = new CQFUIScope(inRect.width, inRect.height);
            float width = inRect.width - 20f;
            if (pipe == null)
            {
                if (CustomQuestFramework_ModSetting.setting.dialogAIAllowEditing)
                    Widgets.CheckboxLabeled(new Rect(0f, 0f, width, 32f), "CQF_AI_Edit".Translate(), ref editing);
                if (CQFAIIconButton.DrawText(new Rect(0f, 42f, width, 32f), "CQF_AI_ExternalStart".Translate()))
                    Start();
                return;
            }

            Widgets.Label(new Rect(0f, 0f, width, 32f), (pipe.Completion.IsFaulted ? "CQF_AI_ExternalError" :
                session?.IsBusy == true ? "CQF_AI_ExternalBusy" : editing ? "CQF_AI_ExternalEditing" : "CQF_AI_ExternalReadOnly").Translate());
            if (CQFAIIconButton.DrawText(new Rect(0f, 42f, width, 32f), pipe.Name, tip: "CQF_AI_ExternalCopy".Translate()))
                GUIUtility.systemCopyBuffer = pipe.Name;
            if (session?.CanUndo == true && !session.IsBusy &&
                CQFAIIconButton.DrawText(new Rect(0f, 84f, width, 32f), "CQF_AI_ExternalUndo".Translate()))
            {
                try { session.Undo(); }
                catch (Exception error) { Report(error); }
            }
        }

        public override void WindowUpdate()
        {
            base.WindowUpdate();
            if (pipe == null) return;
            if (!ReferenceEquals(game, Current.Game) || editing && !CustomQuestFramework_ModSetting.setting.dialogAIAllowEditing)
            {
                Close();
                return;
            }
            if (pipe.Completion.IsFaulted)
            {
                if (!reported)
                {
                    reported = true;
                    Report(pipe.Completion.Exception!);
                }
                return;
            }
            session!.Update(pipe);
        }

        public override void PostClose()
        {
            try { session?.Close(); }
            catch (Exception error) { Report(error); }
            finally { pipe?.Dispose(); }
            base.PostClose();
        }

        private void Start()
        {
            try
            {
                game = Current.Game;
                CQFAIModel model = new CQFAIModel();
                CustomQuestFramework_ModSetting settings = CustomQuestFramework_ModSetting.setting;
                editing &= settings.dialogAIAllowEditing;
                session = new CQFAIExternalSession(new CQFAITargetCatalog(() => CQFAITargetCatalog.Discover(model)),
                    model, editing, settings.dialogAIAllowTextGeneration);
                pipe = new CQFAIExternalPipe();
            }
            catch (Exception error) { Report(error); }
        }

        private void Report(Exception error)
        {
            Log.Error("CQF MCP: " + error);
            Messages.Message("CQF_DialogGraph_Error".Translate(error.Message), RimWorld.MessageTypeDefOf.RejectInput);
        }

        private CQFAIExternalPipe? pipe;
        private CQFAIExternalSession? session;
        private Game? game;
        private bool editing;
        private bool reported;
    }
}
#endif
