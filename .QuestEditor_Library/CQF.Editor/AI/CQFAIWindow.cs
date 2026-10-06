using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace QuestEditor_Library
{
    public sealed class CQFAIWindow : Window
    {
        public CQFAIWindow(CQFAIEditorContext? context = null)
        {
            layer = WindowLayer.Super;
            draggable = true;
            resizeable = true;
            absorbInputAroundWindow = false;
            preventCameraMotion = false;
            doCloseX = false;
            closeOnAccept = false;
            closeOnCancel = false;
            model = new CQFAIModel();
            catalog = new CQFAIResourceCatalog(model);
            conversation = new CQFAIConversation(model, catalog);
            UseContext(context);
        }

        public override Vector2 InitialSize => new Vector2(Mathf.Min(UI.screenWidth - 40f, 420f), Mathf.Min(UI.screenHeight - 40f, 510f));

        public static void Open(CQFAIEditorContext? context)
        {
            if (Find.WindowStack.TryGetWindow<CQFAIWindow>(out CQFAIWindow window))
            {
                if (context != null) window.UseContext(context);
                if (window.collapsed) window.ToggleCollapsed();
                Find.WindowStack.Notify_ManuallySetFocus(window);
                return;
            }
            Find.WindowStack.Add(new CQFAIWindow(context));
        }

        public override void WindowUpdate()
        {
            base.WindowUpdate();
            try { SyncContext(); Poll(); }
            catch (Exception error) { Error(error); }
            windowRect.width = Mathf.Clamp(windowRect.width, Mathf.Min(340f, UI.screenWidth - 20f), UI.screenWidth - 20f);
            if (!collapsed) windowRect.height = Mathf.Clamp(windowRect.height, Mathf.Min(360f, UI.screenHeight - 20f), UI.screenHeight - 20f);
            windowRect.x = Mathf.Clamp(windowRect.x, 0f, Mathf.Max(0f, UI.screenWidth - windowRect.width));
            windowRect.y = Mathf.Clamp(windowRect.y, 0f, Mathf.Max(0f, UI.screenHeight - windowRect.height));
        }

        public override void PostClose()
        {
            Cancel();
            base.PostClose();
        }

        public override void DoWindowContents(Rect inRect)
        {
            Text.Font = GameFont.Small;
            DrawHeader(inRect);
            if (collapsed) return;
            float composerHeight = Mathf.Clamp(Text.CalcHeight(command, inRect.width - 24f), 40f, 96f) + 48f;
            float statusHeight = task != null || statusIsError ? 24f : 0f;
            Rect composer = new Rect(0f, inRect.height - composerHeight, inRect.width, composerHeight);
            Rect history = new Rect(0f, 40f, inRect.width, Mathf.Max(30f, composer.y - 50f - statusHeight));
            DrawHistory(history);
            if (statusHeight > 0f)
            {
                Rect statusRect = new Rect(4f, composer.y - 25f, inRect.width - 8f, 22f);
                Color previous = GUI.color;
                GUI.color = statusIsError ? ColorLibrary.RedReadable : new Color(0.65f, 0.72f, 0.68f);
                Text.Font = GameFont.Tiny;
                Widgets.Label(statusRect, (statusIsError ? "CQF_AI_RequestFailed".Translate().ToString() : status).Truncate(statusRect.width));
                TooltipHandler.TipRegion(statusRect, status);
                Text.Font = GameFont.Small;
                GUI.color = previous;
            }
            DrawComposer(composer);
            if (UnityEngine.Event.current.type == EventType.KeyDown && UnityEngine.Event.current.control && UnityEngine.Event.current.keyCode == KeyCode.Return
                && GUI.GetNameOfFocusedControl() == "CQF_AI_Command" && task == null)
            {
                Start();
                UnityEngine.Event.current.Use();
            }
        }

        protected override void SetInitialSizeAndPosition()
        {
            Vector2 size = InitialSize;
            windowRect = new Rect(UI.screenWidth - size.x - 12f, Mathf.Min(90f, Mathf.Max(0f, UI.screenHeight - size.y - 12f)), size.x, size.y);
        }

        private void DrawHistory(Rect rect)
        {
            CQFAIMessage[] messages = conversation.VisibleMessages.ToArray();
            if (messages.Length == 0)
            {
                Color previous = GUI.color;
                GUI.color = new Color(0.56f, 0.63f, 0.60f);
                Text.Anchor = TextAnchor.MiddleCenter;
                Widgets.Label(rect, "CQF_AI_EmptyChat".Translate());
                Text.Anchor = TextAnchor.UpperLeft;
                GUI.color = previous;
                return;
            }
            float width = rect.width - 20f;
            float height = messages.Sum(message => Text.CalcHeight(message.DisplayContent, (message.Role == "user" ? width * 0.87f : width) - 20f) + 32f);
            if (scrollToEnd) { chatScroll.y = Mathf.Max(0f, height - rect.height); scrollToEnd = false; }
            Widgets.BeginScrollView(rect, ref chatScroll, new Rect(0f, 0f, width, Mathf.Max(rect.height, height)));
            float y = 0f;
            foreach (CQFAIMessage message in messages)
            {
                bool player = message.Role == "user";
                float cardWidth = player ? Mathf.Clamp(Text.CalcSize(message.DisplayContent).x + 24f, 50f, width * 0.87f) : width;
                float textHeight = Text.CalcHeight(message.DisplayContent, cardWidth - 20f);
                Rect card = new Rect(player ? width - cardWidth : 0f, y, cardWidth, textHeight + 18f);
                if (player) CQFAIIconButton.DrawSurface(card, new Color(0.20f, 0.27f, 0.23f), 8);
                Color previous = GUI.color;
                if (message.Role == "system") GUI.color = ColorLibrary.RedReadable;
                Widgets.Label(new Rect(card.x + 10f, card.y + 8f, card.width - 20f, textHeight), message.DisplayContent);
                GUI.color = previous;
                y += card.height + 14f;
            }
            Widgets.EndScrollView();
        }

        private void DrawHeader(Rect rect)
        {
            Widgets.Label(new Rect(0f, 2f, Mathf.Max(50f, rect.width - 164f), 26f), "CQF_AI_Open".Translate());
            float x = rect.width - 28f;
            if (CQFAIIconButton.Draw(new Rect(x, 0f, 28f, 28f), CQFAIIcon.Close, "CloseButton".Translate())) Close();
            x -= 32f;
            if (CQFAIIconButton.Draw(new Rect(x, 0f, 28f, 28f), collapsed ? CQFAIIcon.Expand : CQFAIIcon.Collapse,
                (collapsed ? "CQF_AI_Expand" : "CQF_AI_Collapse").Translate())) ToggleCollapsed();
            x -= 32f;
            if (CQFAIIconButton.Draw(new Rect(x, 0f, 28f, 28f), CQFAIIcon.Settings, "CQF_AI_Settings".Translate()))
            {
                if (Find.WindowStack.TryGetWindow<CQFAISettingsWindow>(out CQFAISettingsWindow settings)) Find.WindowStack.Notify_ManuallySetFocus(settings);
                else Find.WindowStack.Add(new CQFAISettingsWindow());
            }
            bool enabled = GUI.enabled;
            x -= 32f;
            bool undoAvailable = transaction?.CanUndo == true;
            bool undoCurrent = undoAvailable && transaction!.IsCurrent;
            GUI.enabled = enabled && task == null && undoCurrent;
            if (CQFAIIconButton.Draw(new Rect(x, 0f, 28f, 28f), CQFAIIcon.Undo, (undoAvailable && !undoCurrent ? "CQF_AI_UndoConflict" : "CQF_DialogGraph_Undo").Translate())) Undo();
            x -= 32f;
            GUI.enabled = enabled && task == null && conversation.Messages.Count > 0;
            if (CQFAIIconButton.Draw(new Rect(x, 0f, 28f, 28f), CQFAIIcon.Clear, "CQF_AI_ClearChat".Translate())) ClearChat();
            GUI.enabled = enabled;
        }

        private void DrawComposer(Rect rect)
        {
            CQFAIIconButton.DrawSurface(rect, new Color(0.19f, 0.23f, 0.21f), 9);
            CQFAIIconButton.DrawSurface(rect.ContractedBy(1f), new Color(0.10f, 0.13f, 0.12f), 8);
            if (composerStyle == null)
            {
                composerStyle = new GUIStyle(Text.CurTextAreaStyle) { padding = new RectOffset(0, 0, 0, 0) };
                composerStyle.normal.background = null;
                composerStyle.hover.background = null;
                composerStyle.focused.background = null;
                composerStyle.active.background = null;
            }
            Rect input = new Rect(rect.x + 12f, rect.y + 10f, rect.width - 24f, rect.height - 48f);
            GUI.SetNextControlName("CQF_AI_Command");
            command = GUI.TextArea(input, command, composerStyle);
            if (command.Length == 0 && GUI.GetNameOfFocusedControl() != "CQF_AI_Command")
            {
                Color previous = GUI.color;
                GUI.color = new Color(0.45f, 0.53f, 0.48f);
                Widgets.Label(input, "CQF_AI_InputHint".Translate());
                GUI.color = previous;
            }
            bool enabled = GUI.enabled;
            GUI.enabled = enabled && task == null;
            if (context?.Read() is CustomMapDataDef && CQFAIBridge.GenerateMap != null
                && CQFAIIconButton.Draw(new Rect(rect.x + 8f, rect.yMax - 36f, 28f, 28f), CQFAIIcon.Options, "CQF_AI_GenerateMap".Translate())) ShowOptions();
            GUI.enabled = enabled && (task != null || !string.IsNullOrWhiteSpace(command));
            if (CQFAIIconButton.Draw(new Rect(rect.xMax - 38f, rect.yMax - 36f, 28f, 28f), task == null ? CQFAIIcon.Send : CQFAIIcon.Stop,
                (task == null ? "CQF_AI_SendHint" : "CQF_AI_Stop").Translate(), true))
            {
                if (task == null) Start(); else Cancel();
            }
            GUI.enabled = enabled;
            if (sessionUsage.Reported + sessionUsage.Unavailable > 0)
            {
                Rect usageRect = new Rect(rect.x + 42f, rect.yMax - 31f, rect.width - 86f, 22f);
                Text.Font = GameFont.Tiny;
                string current = taskUsage.Reported > 0 ? taskUsage.Total.ToString("N0") + (taskUsage.Unavailable > 0 ? "+?" : "") : "CQF_AI_UsageUnknown".Translate().ToString();
                string session = sessionUsage.Reported > 0 ? sessionUsage.Total.ToString("N0") + (sessionUsage.Unavailable > 0 ? "+?" : "") : "CQF_AI_UsageUnknown".Translate().ToString();
                Widgets.Label(usageRect, "CQF_AI_UsageBrief".Translate(current, session).ToString().Truncate(usageRect.width));
                TooltipHandler.TipRegion(usageRect, "CQF_AI_UsageDetails".Translate(taskUsage.Input.ToString("N0"), taskUsage.Output.ToString("N0"), taskUsage.Unavailable,
                    sessionUsage.Input.ToString("N0"), sessionUsage.Output.ToString("N0"), sessionUsage.Unavailable));
                Text.Font = GameFont.Small;
            }
        }

        private void ShowOptions()
        {
            if (context?.Read() is not CustomMapDataDef || CQFAIBridge.GenerateMap == null) return;
            List<FloatMenuOption> options = new List<FloatMenuOption>
            {
                new FloatMenuOption("CQF_AI_GenerateMap".Translate(), () =>
                {
                    try { CQFAIBridge.GenerateMap!((CustomMapDataDef)model.Copy(context!.Read())); }
                    catch (Exception error) { Error(error); }
                })
            };
            FloatMenu menu = new FloatMenu(options) { vanishIfMouseDistant = false };
            Find.WindowStack.Add(menu);
            menu.windowRect.position = new Vector2(
                Mathf.Clamp(windowRect.x + Margin + 8f, 0f, Mathf.Max(0f, UI.screenWidth - menu.windowRect.width)),
                Mathf.Max(0f, windowRect.yMax - Margin - 40f - menu.windowRect.height));
        }

        private void ClearChat()
        {
            conversation.Clear();
            taskUsage.Clear(); sessionUsage.Clear();
            status = string.Empty;
            statusIsError = false;
            chatScroll = Vector2.zero;
        }

        private void SyncContext()
        {
            CQFAIEditorContext? current = Find.WindowStack.Windows.Reverse().OfType<ICQFAIEditorHost>().Select(host => host.AIContext).FirstOrDefault(value => value != null && value.IsValid?.Invoke() != false);
            if (current != null) UseContext(current);
            else
            {
                if (context?.Owner is Window) UseContext(null);
                if (Current.Game != null && Find.CurrentMap is Map map)
                {
                    if (!ReferenceEquals(context?.Identity, map)) UseContext(CQFAILiveMapContext.Create(map));
                }
                else if (context?.Owner is Map) UseContext(null);
            }
            if (context?.IsValid?.Invoke() == false) UseContext(null);
        }

        private void UseContext(CQFAIEditorContext? value)
        {
            object? identity = value?.Identity;
            if (ReferenceEquals(value?.Owner, context?.Owner) && ReferenceEquals(identity, contextIdentity))
            {
                context = value;
                return;
            }
            bool running = task != null;
            if (running) Cancel();
            context = value;
            contextIdentity = identity;
            transaction = null;
            harness = null;
            if (conversation.Messages.Count > 0)
                conversation.Add("system", "Editing target changed. Use only the current target supplied with the next request.");
            status = running ? "CQF_AI_TargetChanged".Translate() : string.Empty;
            statusIsError = running;
            scrollToEnd = true;
        }

        private void ToggleCollapsed()
        {
            if (collapsed) windowRect.size = expandedSize;
            else { expandedSize = windowRect.size; windowRect.height = 66f; }
            collapsed = !collapsed;
            resizeable = !collapsed;
        }

        private void Start()
        {
            try
            {
                SyncContext();
                CustomQuestFramework_ModSetting setting = CustomQuestFramework_ModSetting.setting;
                if (!setting.dialogAIEnabled) throw new InvalidOperationException("CQF_DialogAI_Disabled");
                if (string.IsNullOrWhiteSpace(command)) return;
                nativeTools = setting.dialogAIUseTools;
                harness = new CQFAIHarness(model, catalog, conversation, context, command, setting.dialogAIAllowEditing && CQFEditorBridge.IsLoaded, setting.dialogAIAllowTextGeneration, nativeTools);
                taskUsage.Clear();
                conversation.Add("user", command);
                command = string.Empty;
                client = new CQFDialogAIClient(setting.dialogAIEndpoint, setting.dialogAIModel, setting.dialogAIKey, setting.dialogAITimeout);
                cancellation = new CancellationTokenSource();
                task = client.CompleteConversationAsync(harness.Instructions, conversation.RequestMessages, cancellation.Token, nativeTools ? harness.Registry.Tools : null);
                status = "CQF_DialogAI_Working".Translate();
                statusIsError = false;
                scrollToEnd = true;
            }
            catch (Exception error) { Error(error); }
        }

        private void Poll()
        {
            if (task == null || !task.IsCompleted) return;
            Task<string> completed = task;
            task = null;
            try
            {
                string response;
                try { response = completed.GetAwaiter().GetResult(); }
                finally
                {
                    if (client?.ReceivedResponse == true)
                    {
                        taskUsage.Add(client.LastUsage); sessionUsage.Add(client.LastUsage);
                        if (client.UsageError != null) Log.Warning("CQF AI: " + "CQF_AI_InvalidUsage".Translate());
                    }
                }
                if (!CustomQuestFramework_ModSetting.setting.dialogAIEnabled) throw new InvalidOperationException("CQF_DialogAI_Disabled");
                if (harness?.Transaction != null && !CustomQuestFramework_ModSetting.setting.dialogAIAllowEditing) throw new InvalidDataException("CQF_AI_ReadOnly");
                bool continued;
                try { continued = harness!.Process(response); }
                finally
                {
                    if (harness?.Transaction?.CanUndo == true)
                    {
                        transaction = harness.Transaction;
                        contextIdentity = context?.Identity;
                    }
                }
                XElement[] failures = harness!.LastResults.Where(result => result.Element("error") != null).ToArray();
                if (failures.Length > 0)
                {
                    string details = string.Join("\n", failures.Select(result => result.Element("error")!.Value));
                    string secret = CustomQuestFramework_ModSetting.setting.dialogAIKey;
                    if (!string.IsNullOrEmpty(secret)) details = details.Replace(secret, "***");
                    Log.Warning("CQF AI tool: " + details);
                    status = "CQF_AI_ToolRetrying".Translate(harness.Rounds) + " " + details;
                    statusIsError = true;
                }
                if (continued)
                {
                    task = client!.CompleteConversationAsync(harness.Instructions, conversation.RequestMessages, cancellation!.Token, nativeTools ? harness.Registry.Tools : null);
                    if (failures.Length == 0) { status = "CQF_AI_ToolWorking".Translate(harness.Rounds); statusIsError = false; }
                    scrollToEnd = true;
                    return;
                }
                status = (harness.Transaction?.CanUndo != true ? "CQF_AI_Replied" : harness.Transaction is CQFAILiveMapTransaction ? "CQF_AI_AppliedMap" : "CQF_AI_AppliedLive").Translate();
                statusIsError = false;
                cancellation?.Dispose();
                cancellation = null;
                harness = null;
                scrollToEnd = true;
            }
            catch (OperationCanceledException)
            {
                status = (cancellation?.IsCancellationRequested == true ? "CQF_DialogAI_Cancelled" : "CQF_DialogAI_TimedOut").Translate();
                statusIsError = true;
                cancellation?.Dispose(); cancellation = null; harness = null;
            }
            catch (Exception error) { Error(error); }
        }

        private void Undo()
        {
            try
            {
                transaction!.Undo();
                contextIdentity = context?.Identity;
                conversation.Add("system", "The player undid the last AI edit. Use the latest editor data for subsequent changes.", "CQF_AI_Undone".Translate());
                status = "CQF_AI_Undone".Translate();
                statusIsError = false;
                scrollToEnd = true;
            }
            catch (Exception error) { Error(error); }
        }

        private void Cancel()
        {
            cancellation?.Cancel();
            task?.ContinueWith(completed => { _ = completed.Exception; }, TaskContinuationOptions.OnlyOnFaulted);
            task = null;
            cancellation?.Dispose();
            cancellation = null;
            harness = null;
            status = "CQF_DialogAI_Cancelled".Translate();
            statusIsError = false;
        }

        private void Error(Exception error)
        {
            cancellation?.Cancel();
            task?.ContinueWith(completed => { _ = completed.Exception; }, TaskContinuationOptions.OnlyOnFaulted);
            task = null;
            cancellation?.Dispose();
            cancellation = null;
            harness = null;
            string key = error.Message.Split(new[] { ':', ' ' }, 2)[0];
            string message = key.CanTranslate() ? key.Translate() + error.Message.Substring(key.Length) : error.Message;
            string secret = CustomQuestFramework_ModSetting.setting.dialogAIKey;
            if (!string.IsNullOrEmpty(secret)) message = message.Replace(secret, "***");
            status = "CQF_DialogGraph_Error".Translate(message);
            statusIsError = true;
            if (conversation.Messages.Sum(message => message.Content.Length) < 599900)
                conversation.Add("system", "Request failed. Read the latest editor state before retrying.", status, true);
            scrollToEnd = true;
            string details = string.IsNullOrEmpty(secret) ? error.ToString() : error.ToString().Replace(secret, "***");
            Log.Error("CQF AI: " + details);
        }

        private CQFAIEditorContext? context;
        private object? contextIdentity;
        private readonly CQFAIModel model;
        private readonly CQFAIResourceCatalog catalog;
        private readonly CQFAIConversation conversation;
        private readonly CQFAITokenTotals taskUsage = new CQFAITokenTotals();
        private readonly CQFAITokenTotals sessionUsage = new CQFAITokenTotals();
        private CQFAITransaction? transaction;
        private CQFAIHarness? harness;
        private Task<string>? task;
        private CancellationTokenSource? cancellation;
        private CQFDialogAIClient? client;
        private string command = string.Empty;
        private string status = string.Empty;
        private bool nativeTools;
        private bool collapsed;
        private bool scrollToEnd;
        private bool statusIsError;
        private GUIStyle? composerStyle;
        private Vector2 expandedSize;
        private Vector2 chatScroll;
    }
}
