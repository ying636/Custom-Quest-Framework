using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace QuestEditor_Library
{
    public sealed class CQFDialogAISettings
    {
        public void Poll(CustomQuestFramework_ModSetting setting)
        {
            if (this.task != null && this.task.IsCompleted)
            {
                Task<string> completed = this.task;
                this.task = null;
                try
                {
                    completed.GetAwaiter().GetResult();
                    this.status = "CQF_DialogAI_TestPassed".Translate();
                }
                catch (OperationCanceledException)
                {
                    this.status = (this.cancellation?.IsCancellationRequested == true ? "CQF_DialogAI_Cancelled" : "CQF_DialogAI_TimedOut").Translate();
                    Log.Error("CQF dialog AI connection: " + this.status);
                }
                catch (Exception error)
                {
                    this.Error(error, setting);
                }
                this.cancellation?.Dispose();
                this.cancellation = null;
            }
        }

        public void Draw(Rect rect, CustomQuestFramework_ModSetting setting)
        {
            this.Poll(setting);
            float y = rect.y;
            Widgets.CheckboxLabeled(new Rect(rect.x, y, rect.width, 30f), "CQF_DialogAI_Enabled".Translate(), ref setting.dialogAIEnabled);
            y += 40f;
            Rect editingRect = new Rect(rect.x, y, rect.width, 30f);
            Widgets.CheckboxLabeled(editingRect, "CQF_AI_Edit".Translate(), ref setting.dialogAIAllowEditing);
            TooltipHandler.TipRegion(editingRect, "CQF_AI_EditHint".Translate());
            y += 34f;
            Rect textRect = new Rect(rect.x, y, rect.width, 30f);
            Widgets.CheckboxLabeled(textRect, "CQF_AI_GenerateText".Translate(), ref setting.dialogAIAllowTextGeneration);
            TooltipHandler.TipRegion(textRect, "CQF_AI_GenerateTextHint".Translate());
            y += 34f;
            Rect toolsRect = new Rect(rect.x, y, rect.width, 30f);
            Widgets.CheckboxLabeled(toolsRect, "CQF_AI_NativeTools".Translate(), ref setting.dialogAIUseTools);
            TooltipHandler.TipRegion(toolsRect, "CQF_AI_NativeToolsHint".Translate());
            y += 34f;
            Widgets.Label(new Rect(rect.x, y, 180f, 28f), "CQF_DialogAI_Endpoint".Translate());
            setting.dialogAIEndpoint = Widgets.TextField(new Rect(rect.x + 185f, y, rect.width - 185f, 28f), setting.dialogAIEndpoint);
            y += 34f;
            Widgets.Label(new Rect(rect.x, y, rect.width, 58f), "CQF_DialogAI_EndpointHint".Translate());
            y += 63f;
            Widgets.Label(new Rect(rect.x, y, 180f, 28f), "CQF_DialogAI_Model".Translate());
            setting.dialogAIModel = Widgets.TextField(new Rect(rect.x + 185f, y, rect.width - 185f, 28f), setting.dialogAIModel);
            y += 40f;
            Widgets.Label(new Rect(rect.x, y, 180f, 28f), "CQF_DialogAI_Key".Translate());
            Rect keyRect = new Rect(rect.x + 185f, y, rect.width - 280f, 28f);
            setting.dialogAIKey = this.showKey ? Widgets.TextField(keyRect, setting.dialogAIKey) : GUI.PasswordField(keyRect, setting.dialogAIKey, '*');
            Widgets.CheckboxLabeled(new Rect(rect.xMax - 90f, y, 90f, 28f), "CQF_DialogAI_ShowKey".Translate(), ref this.showKey);
            y += 40f;
            Widgets.Label(new Rect(rect.x, y, 180f, 28f), "CQF_DialogAI_Timeout".Translate());
            if (this.timeoutBuffer == null) this.timeoutBuffer = setting.dialogAITimeout.ToString();
            Widgets.TextFieldNumeric(new Rect(rect.x + 185f, y, 100f, 28f), ref setting.dialogAITimeout, ref this.timeoutBuffer, 10, 600);
            y += 42f;
            bool enabled = GUI.enabled;
            GUI.enabled = enabled && this.task == null;
            if (Widgets.ButtonText(new Rect(rect.x, y, 180f, 32f), "CQF_DialogAI_Test".Translate()))
            {
                try
                {
                    CQFDialogAIClient client = new CQFDialogAIClient(setting.dialogAIEndpoint, setting.dialogAIModel, setting.dialogAIKey, setting.dialogAITimeout);
                    this.cancellation = new CancellationTokenSource();
                    this.task = client.CompleteAsync("Return only the token OK.", "Connection test.", this.cancellation.Token);
                    this.status = "CQF_DialogAI_Working".Translate();
                }
                catch (Exception error)
                {
                    this.Error(error, setting);
                }
            }
            GUI.enabled = enabled && this.task != null;
            if (Widgets.ButtonText(new Rect(rect.x + 190f, y, 120f, 32f), "Cancel".Translate())) this.cancellation?.Cancel();
            GUI.enabled = enabled;
            y += 42f;
            Widgets.Label(new Rect(rect.x, y, rect.width, 70f), this.status);
        }

        public void Cancel()
        {
            this.cancellation?.Cancel();
            this.task?.ContinueWith(completed => { _ = completed.Exception; }, TaskContinuationOptions.OnlyOnFaulted);
            this.task = null;
            this.cancellation?.Dispose();
            this.cancellation = null;
        }

        private void Error(Exception error, CustomQuestFramework_ModSetting setting)
        {
            string key = error.Message.Split(new[] { ':', ' ' }, 2)[0];
            string message = key.CanTranslate() ? key.Translate() + error.Message.Substring(key.Length) : error.Message;
            if (!string.IsNullOrEmpty(setting.dialogAIKey)) message = message.Replace(setting.dialogAIKey, "***");
            this.status = "CQF_DialogGraph_Error".Translate(message);
            Log.Error("CQF dialog AI connection: " + message);
        }

        private Task<string>? task;
        private CancellationTokenSource? cancellation;
        private string? timeoutBuffer;
        private string status = string.Empty;
        private bool showKey;
    }
}
