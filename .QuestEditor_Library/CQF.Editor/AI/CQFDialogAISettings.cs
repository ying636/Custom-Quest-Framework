using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace QuestEditor_Library
{
    public sealed class CQFDialogAISettings
    {
        public float ContentHeight { get; private set; } = 500f;

        public void Poll(CustomQuestFramework_ModSetting setting)
        {
            if (this.task == null || !this.task.IsCompleted) return;
            Task<string> completed = this.task;
            this.task = null;
            try
            {
                completed.GetAwaiter().GetResult();
                this.status = "CQF_DialogAI_TestPassed".Translate();
                this.statusIsError = false;
            }
            catch (OperationCanceledException)
            {
                this.status = (this.cancellation?.IsCancellationRequested == true ? "CQF_DialogAI_Cancelled" : "CQF_DialogAI_TimedOut").Translate();
                this.statusIsError = this.cancellation?.IsCancellationRequested != true;
                if (this.statusIsError) Log.Error("CQF dialog AI connection: " + this.status);
            }
            catch (Exception error) { this.Error(error, setting); }
            this.cancellation?.Dispose();
            this.cancellation = null;
        }

        public void Draw(Rect rect, CustomQuestFramework_ModSetting setting)
        {
            using CQFUIScope cqfUIScope = new CQFUIScope(rect.width, rect.height);
            GameFont previousFont = Text.Font;
            TextAnchor previousAnchor = Text.Anchor;
            bool previousWrap = Text.WordWrap;
            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.UpperLeft;
            Text.WordWrap = true;
            try
            {
                this.Poll(setting);
                string[] tabs = { "CQF_AI_TabConnection", "CQF_AI_TabBehavior", "CQF_AI_TabPrompts", "CQF_AI_TabUsage" };
                int columns = rect.width < 440f ? 2 : 4;
                float tabWidth = (rect.width - 6f * (columns - 1)) / columns;
                for (int i = 0; i < tabs.Length; i++)
                {
                    Rect tab = new Rect(rect.x + i % columns * (tabWidth + 6f), rect.y + i / columns * 38f, tabWidth, 32f);
                    if (CQFAIIconButton.DrawText(tab, tabs[i].Translate(), selectedTab == i) && selectedTab != i)
                    {
                        selectedTab = i;
                        bodyScroll = Vector2.zero;
                    }
                }
                float top = tabs.Length / columns * 38f + 6f;
                Rect body = new Rect(rect.x, rect.y + top, rect.width, rect.height - top);
                Rect content = new Rect(0f, 0f, body.width - 20f, Mathf.Max(body.height, ContentHeight));
                Widgets.BeginScrollView(body, ref bodyScroll, content);
                using CQFUIScope cqfContentScope1 = new CQFUIScope(content.width);
                try
                {
                    float width = content.width - 8f;
                    ContentHeight = (selectedTab switch
                    {
                        0 => DrawConnection(width, setting),
                        1 => DrawBehavior(width, setting),
                        2 => DrawPrompts(width, setting),
                        _ => DrawUsage(width)
                    }) + 16f;
                }
                finally { Widgets.EndScrollView(); }
            }
            finally { Text.Font = previousFont; Text.Anchor = previousAnchor; Text.WordWrap = previousWrap; }
        }

        public void Cancel()
        {
            this.cancellation?.Cancel();
            this.task?.ContinueWith(completed => { _ = completed.Exception; }, TaskContinuationOptions.OnlyOnFaulted);
            this.task = null;
            this.cancellation?.Dispose();
            this.cancellation = null;
        }

        private float DrawConnection(float width, CustomQuestFramework_ModSetting setting)
        {
            float y = 4f;
            DrawCheckbox(width, ref y, "CQF_DialogAI_Enabled", ref setting.dialogAIEnabled);
            y += 10f;
            Widgets.Label(new Rect(0f, y, width, 26f), "CQF_DialogAI_Endpoint".Translate());
            y += 28f;
            setting.dialogAIEndpoint = Widgets.TextField(new Rect(0f, y, width, 30f), setting.dialogAIEndpoint);
            y += 36f;
            DrawHint(width, ref y, "CQF_DialogAI_EndpointHint");
            Widgets.Label(new Rect(0f, y, width, 26f), "CQF_DialogAI_Model".Translate());
            y += 28f;
            setting.dialogAIModel = Widgets.TextField(new Rect(0f, y, width, 30f), setting.dialogAIModel);
            y += 42f;
            Widgets.Label(new Rect(0f, y, width, 26f), "CQF_DialogAI_Key".Translate());
            y += 28f;
            Rect keyRect = new Rect(0f, y, width - 100f, 30f);
            setting.dialogAIKey = this.showKey ? Widgets.TextField(keyRect, setting.dialogAIKey) : GUI.PasswordField(keyRect, setting.dialogAIKey, '*', Text.CurTextFieldStyle);
            Widgets.CheckboxLabeled(new Rect(width - 92f, y, 92f, 30f), "CQF_DialogAI_ShowKey".Translate(), ref this.showKey);
            y += 42f;
            string timeoutLabel = "CQF_DialogAI_Timeout".Translate();
            float timeoutHeight = Mathf.Max(26f, Text.CalcHeight(timeoutLabel, width));
            Widgets.Label(new Rect(0f, y, width, timeoutHeight), timeoutLabel);
            y += timeoutHeight + 6f;
            if (this.timeoutBuffer == null) this.timeoutBuffer = setting.dialogAITimeout.ToString();
            Widgets.TextFieldNumeric(new Rect(0f, y, Mathf.Min(160f, width), 32f), ref setting.dialogAITimeout, ref this.timeoutBuffer, 10, 600);
            y += 36f;
            DrawHint(width, ref y, "CQF_AI_IdleTimeoutHint");
            bool enabled = GUI.enabled;
            try
            {
                GUI.enabled = enabled && this.task == null;
                if (CQFAIIconButton.DrawText(new Rect(0f, y, (width - 8f) * 0.65f, 32f), "CQF_DialogAI_Test".Translate()))
                {
                    try
                    {
                        CQFDialogAIClient client = new CQFDialogAIClient(setting.dialogAIEndpoint, setting.dialogAIModel, setting.dialogAIKey, setting.dialogAITimeout);
                        this.cancellation = new CancellationTokenSource();
                        this.task = client.CompleteAsync("Return only the token OK.", "Connection test.", this.cancellation.Token);
                        this.status = "CQF_DialogAI_Working".Translate();
                        this.statusIsError = false;
                    }
                    catch (Exception error) { this.Error(error, setting); }
                }
                GUI.enabled = enabled && this.task != null;
                if (CQFAIIconButton.DrawText(new Rect((width - 8f) * 0.65f + 8f, y, (width - 8f) * 0.35f, 32f), "Cancel".Translate())) this.cancellation?.Cancel();
            }
            finally { GUI.enabled = enabled; }
            y += 42f;
            Color previousColor = GUI.color;
            try
            {
                if (statusIsError) GUI.color = Color.red;
                float height = Text.CalcHeight(this.status, width);
                Widgets.Label(new Rect(0f, y, width, height), this.status);
                y += height;
            }
            finally { GUI.color = previousColor; }
            return y;
        }

        private float DrawBehavior(float width, CustomQuestFramework_ModSetting setting)
        {
            float y = 4f;
            DrawCheckbox(width, ref y, "CQF_AI_Edit", ref setting.dialogAIAllowEditing);
            DrawHint(width, ref y, "CQF_AI_EditHint");
            Widgets.Label(new Rect(0f, y, width, 26f), "CQF_AI_TokenBudget".Translate());
            y += 30f;
            tokenBudgetBuffer ??= setting.dialogAITokenBudget.ToString();
            Widgets.TextFieldNumeric(new Rect(0f, y, Mathf.Min(180f, width), 32f), ref setting.dialogAITokenBudget, ref tokenBudgetBuffer, 1000, 10000000);
            y += 42f;
            Widgets.Label(new Rect(0f, y, width, 26f), "CQF_AI_RequestBudget".Translate());
            y += 30f;
            requestBudgetBuffer ??= setting.dialogAIRequestBudget.ToString();
            Widgets.TextFieldNumeric(new Rect(0f, y, Mathf.Min(180f, width), 32f), ref setting.dialogAIRequestBudget, ref requestBudgetBuffer, 1, 1000);
            y += 38f;
            DrawHint(width, ref y, "CQF_AI_BudgetHint");
            Widgets.Label(new Rect(0f, y, width, 26f), "CQF_AI_ExecutionSpeed".Translate());
            y += 30f;
            int speed = Mathf.Clamp(setting.dialogAIExecutionSpeed, 0, 3);
            if (CQFAIIconButton.DrawText(new Rect(0f, y, width, 32f), ("CQF_AI_ExecutionSpeed_" + speed).Translate(), tip: "CQF_AI_ExecutionSpeedHint".Translate()))
                Find.WindowStack.Add(new FloatMenu(Enumerable.Range(0, 4).Select(value => new FloatMenuOption(("CQF_AI_ExecutionSpeed_" + value).Translate(), () => setting.dialogAIExecutionSpeed = value)).ToList()));
            y += 40f;
            y += 10f;
            DrawCheckbox(width, ref y, "CQF_AI_GenerateText", ref setting.dialogAIAllowTextGeneration);
            DrawHint(width, ref y, "CQF_AI_GenerateTextHint");
            y += 10f;
            DrawCheckbox(width, ref y, "CQF_AI_NativeTools", ref setting.dialogAIUseTools);
            DrawHint(width, ref y, "CQF_AI_NativeToolsHint");
            y += 10f;
            DrawCheckbox(width, ref y, "CQF_AI_Planning", ref setting.dialogAIPlanning);
            DrawHint(width, ref y, "CQF_AI_PlanningHint");
            DrawCheckbox(width, ref y, "CQF_AI_Agents", ref setting.dialogAIAgents);
            DrawHint(width, ref y, "CQF_AI_AgentsHint");
            Widgets.Label(new Rect(0f, y, width, 26f), "CQF_AI_ParallelAgents".Translate());
            y += 30f;
            if (parallelBuffer == null) parallelBuffer = setting.dialogAIParallelAgents.ToString();
            Widgets.TextFieldNumeric(new Rect(0f, y, Mathf.Min(120f, width), 32f), ref setting.dialogAIParallelAgents, ref parallelBuffer, 1, 4);
            y += 44f;
            Widgets.Label(new Rect(0f, y, width, 26f), "CQF_AI_AgentModel".Translate());
            y += 30f;
            setting.dialogAIAgentModel = Widgets.TextField(new Rect(0f, y, width, 32f), setting.dialogAIAgentModel);
            y += 38f;
            DrawHint(width, ref y, "CQF_AI_AgentModelHint");
            return y;
        }

        private float DrawPrompts(float width, CustomQuestFramework_ModSetting setting)
        {
            float y = 4f;
            DrawCheckbox(width, ref y, "CQF_AI_AdditionalPrompt", ref setting.dialogAIAdditionalPromptEnabled);
            DrawHint(width, ref y, "CQF_AI_AdditionalPromptHint");
            float promptWidth = width - 20f;
            float promptHeight = Mathf.Max(240f, Text.CalcHeight(setting.dialogAIAdditionalPrompt, promptWidth) + 12f);
            Widgets.BeginScrollView(new Rect(0f, y, width, 240f), ref this.promptScroll, new Rect(0f, 0f, promptWidth, promptHeight));
            try { setting.dialogAIAdditionalPrompt = Widgets.TextArea(new Rect(0f, 0f, promptWidth, promptHeight), setting.dialogAIAdditionalPrompt); }
            finally { Widgets.EndScrollView(); }
            if (setting.dialogAIAdditionalPrompt.Length > 16000) setting.dialogAIAdditionalPrompt = setting.dialogAIAdditionalPrompt.Substring(0, 16000);
            y += 248f;
            Text.Font = GameFont.Tiny;
            Widgets.Label(new Rect(0f, y, width, 24f), "CQF_AI_PromptLength".Translate(setting.dialogAIAdditionalPrompt.Length));
            Text.Font = GameFont.Small;
            y += 28f;
            Rect example = new Rect(0f, y, width - 40f, 32f);
            TooltipHandler.TipRegion(example, "CQF_AI_LayoutPromptExample".Translate());
            if (CQFAIIconButton.DrawText(example, "CQF_AI_LayoutPromptExample".Translate()))
            {
                if (string.IsNullOrWhiteSpace(setting.dialogAIAdditionalPrompt)) setting.dialogAIAdditionalPrompt = "CQF_AI_LayoutPromptText".Translate();
                else Find.WindowStack.Add(Dialog_MessageBox.CreateConfirmation("CQF_AI_ReplacePromptConfirm".Translate(),
                    () => setting.dialogAIAdditionalPrompt = "CQF_AI_LayoutPromptText".Translate(), destructive: true));
            }
            if (CQFAIIconButton.Draw(new Rect(width - 32f, y, 30f, 30f), CQFAIIcon.Clear, "CQF_AI_ClearPrompt".Translate())) setting.dialogAIAdditionalPrompt = string.Empty;
            y += 44f;
            Widgets.Label(new Rect(0f, y, width, 26f), "CQF_AI_AgentPrompt".Translate());
            y += 30f;
            DrawHint(width, ref y, "CQF_AI_AgentPromptHint");
            float agentHeight = Mathf.Max(180f, Text.CalcHeight(setting.dialogAIAgentPrompt, width - 20f) + 12f);
            Widgets.BeginScrollView(new Rect(0f, y, width, 180f), ref agentPromptScroll, new Rect(0f, 0f, width - 20f, agentHeight));
            try { setting.dialogAIAgentPrompt = Widgets.TextArea(new Rect(0f, 0f, width - 20f, agentHeight), setting.dialogAIAgentPrompt); }
            finally { Widgets.EndScrollView(); }
            if (setting.dialogAIAgentPrompt.Length > 16000) setting.dialogAIAgentPrompt = setting.dialogAIAgentPrompt.Substring(0, 16000);
            return y + 188f;
        }

        private float DrawUsage(float width)
        {
            float y = 4f;
            Widgets.Label(new Rect(0f, y, width, 28f), "CQF_AI_UsageTitle".Translate());
            y += 38f;
            if (Find.WindowStack.TryGetWindow<CQFAIWindow>(out CQFAIWindow chat))
            {
                float height = Text.CalcHeight(chat.UsageSummary, width);
                Widgets.Label(new Rect(0f, y, width, height), chat.UsageSummary);
                y += height + 12f;
                Text.Font = GameFont.Tiny;
                height = Text.CalcHeight(chat.UsageDetails, width);
                Widgets.Label(new Rect(0f, y, width, height), chat.UsageDetails);
                Text.Font = GameFont.Small;
                y += height;
                if (chat.BudgetSummary.Length > 0)
                {
                    y += 16f;
                    height = Text.CalcHeight(chat.BudgetSummary, width);
                    Widgets.Label(new Rect(0f, y, width, height), chat.BudgetSummary);
                    y += height;
                }
            }
            else
            {
                float height = Text.CalcHeight("CQF_AI_UsageEmpty".Translate(), width);
                Widgets.Label(new Rect(0f, y, width, height), "CQF_AI_UsageEmpty".Translate());
                y += height;
            }
            return y;
        }

        private void DrawHint(float width, ref float y, string key)
        {
            TooltipHandler.TipRegion(new Rect(0f, Mathf.Max(0f, y - 36f), width, 32f), key.Translate());
            y += 8f;
        }

        private void DrawCheckbox(float width, ref float y, string key, ref bool value)
        {
            string label = key.Translate();
            float height = Mathf.Max(32f, Text.CalcHeight(label, width - 40f) + 6f);
            Widgets.CheckboxLabeled(new Rect(0f, y, width, height), label, ref value);
            y += height + 4f;
        }

        private void Error(Exception error, CustomQuestFramework_ModSetting setting)
        {
            string key = error.Message.Split(new[] { ':', ' ' }, 2)[0];
            string message = key.CanTranslate() ? key.Translate() + error.Message.Substring(key.Length) : error.Message;
            if (!string.IsNullOrEmpty(setting.dialogAIKey)) message = message.Replace(setting.dialogAIKey, "***");
            this.status = "CQF_DialogGraph_Error".Translate(message);
            this.statusIsError = true;
            Log.Error("CQF dialog AI connection: " + message);
        }

        private Task<string>? task;
        private CancellationTokenSource? cancellation;
        private string? timeoutBuffer;
        private string? parallelBuffer;
        private string? tokenBudgetBuffer;
        private string? requestBudgetBuffer;
        private string status = string.Empty;
        private bool showKey;
        private bool statusIsError;
        private int selectedTab;
        private Vector2 bodyScroll;
        private Vector2 promptScroll;
        private Vector2 agentPromptScroll;
    }
}
