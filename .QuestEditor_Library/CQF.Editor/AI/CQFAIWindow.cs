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
            store = new CQFAISessionStore(Path.Combine(GenFilePaths.ConfigFolderPath, "CQF_AIConversations"));
            try { RestoreSession(store.Recent() ?? new CQFAISession()); }
            catch (Exception error) { HistoryError(error); }
            UseContext(context);
        }

        public override Vector2 InitialSize => new Vector2(Mathf.Min(UI.screenWidth - 40f, 420f), Mathf.Min(UI.screenHeight - 40f, 510f));
        public string UsageSummary => "CQF_AI_UsageBrief".Translate(UsageValue(taskUsage), UsageValue(sessionUsage));
        public string UsageDetails => "CQF_AI_UsageDetails".Translate(taskUsage.Input.ToString("N0"), taskUsage.Output.ToString("N0"), taskUsage.Unavailable,
            sessionUsage.Input.ToString("N0"), sessionUsage.Output.ToString("N0"), sessionUsage.Unavailable);
        public string BudgetSummary => requestBudget == null ? string.Empty : "CQF_AI_BudgetUsage".Translate(requestBudget.Used.ToString("N0"), requestBudget.TokenLimit.ToString("N0"), requestBudget.Requests, requestBudget.RequestLimit, requestBudget.Reserved.ToString("N0")).ToString()
            + (requestBudget.HasEstimates ? "\n" + "CQF_AI_BudgetEstimated".Translate().ToString() : string.Empty);
        public string SessionId => session.Id;
        public bool IsWorking => harness != null;

        public IReadOnlyList<CQFAISessionEntry> History()
        {
            SaveChat();
            try { return store.List(HistoryError); }
            catch (Exception error) { HistoryError(error); return Array.Empty<CQFAISessionEntry>(); }
        }
        public void NewChat()
        {
            Cancel(); SaveChat(); RestoreSession(new CQFAISession()); SaveChat();
        }
        public void SwitchChat(string id)
        {
            if (id == session.Id) return;
            Cancel(); SaveChat();
            try { RestoreSession(store.Load(id)); SaveChat(); }
            catch (Exception error) { HistoryError(error); }
        }
        public void RenameChat(string id, string name)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(name) || name.Length > 100) throw new InvalidDataException("CQF_AI_InvalidHistory");
                if (id == session.Id) { session.Title = name; SaveChat(); }
                else { CQFAISession renamed = store.Load(id); renamed.Title = name; store.Save(renamed, false); }
            }
            catch (Exception error) { HistoryError(error); }
        }
        public void DeleteChat(string id)
        {
            try
            {
                if (id == session.Id) Cancel();
                store.Delete(id);
                if (id == session.Id) { RestoreSession(new CQFAISession()); SaveChat(); }
            }
            catch (Exception error) { HistoryError(error); }
        }

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
            try { SyncContext(); Poll(); mapFeedback.Draw(); }
            catch (Exception error) { Error(error); }
            if ((command != savedDraft || chatDirty) && DateTime.UtcNow >= nextSave) SaveChat();
            windowRect.width = Mathf.Clamp(windowRect.width, Mathf.Min(340f, UI.screenWidth - 20f), UI.screenWidth - 20f);
            if (!collapsed) windowRect.height = Mathf.Clamp(windowRect.height, Mathf.Min(360f, UI.screenHeight - 20f), UI.screenHeight - 20f);
            windowRect.x = Mathf.Clamp(windowRect.x, 0f, Mathf.Max(0f, UI.screenWidth - windowRect.width));
            windowRect.y = Mathf.Clamp(windowRect.y, 0f, Mathf.Max(0f, UI.screenHeight - windowRect.height));
        }

        public override void PostClose()
        {
            Cancel();
            SaveChat();
            Find.WindowStack.TryRemove(typeof(CQFAIHistoryWindow));
            Find.WindowStack.TryRemove(typeof(CQFAIRenameSessionWindow));
            base.PostClose();
        }

        public override void DoWindowContents(Rect inRect)
        {
            using CQFUIScope cqfUIScope = new CQFUIScope(inRect.width, inRect.height);
            GameFont previousFont = Text.Font;
            TextAnchor previousAnchor = Text.Anchor;
            bool previousWrap = Text.WordWrap;
            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.UpperLeft;
            Text.WordWrap = true;
            try
            {
                CQFAIActivity? latestActivity = currentActivity ?? activities.LastOrDefault();
                bool showActivity = latestActivity != null && !latestActivity.Finished;
                CQFAIWindowLayout layout = new CQFAIWindowLayout(inRect, Text.CalcHeight(command, inRect.width - 24f), (statusIsError ? 1 : 0) + (showActivity ? 1 : 0));
                DrawHeader(layout);
                if (collapsed) return;
                DrawHistory(layout.History);
                if (layout.Status.height > 0f)
                {
                    Rect statusRect = new Rect(layout.Status.x, layout.Status.y, layout.Status.width, 22f);
                    Color previous = GUI.color;
                    Text.Font = GameFont.Tiny;
                    if (showActivity)
                    {
                        GUI.color = CQFEditorPalette.Muted;
                        Widgets.Label(statusRect, latestActivity!.Header.Truncate(statusRect.width));
                        TooltipHandler.TipRegion(statusRect, latestActivity.Header + "\n" + "CQF_AI_ElapsedHint".Translate());
                        statusRect.y += 24f;
                    }
                    if (statusIsError)
                    {
                        GUI.color = ColorLibrary.RedReadable;
                        Widgets.Label(statusRect, "CQF_AI_RequestFailed".Translate().ToString().Truncate(statusRect.width));
                        TooltipHandler.TipRegion(statusRect, status);
                    }
                    Text.Font = GameFont.Small;
                    GUI.color = previous;
                }
                DrawComposer(layout);
            }
            finally { Text.Font = previousFont; Text.Anchor = previousAnchor; Text.WordWrap = previousWrap; }
        }

        protected override void SetInitialSizeAndPosition()
        {
            Vector2 size = InitialSize;
            windowRect = new Rect(UI.screenWidth - size.x - 12f, Mathf.Min(90f, Mathf.Max(0f, UI.screenHeight - size.y - 12f)), size.x, size.y);
        }

        private void DrawHistory(Rect rect)
        {
            float width = rect.width - 20f;
            bool refresh = historyWidth != width || historyMessageCount != conversation.Messages.Count || historyActivityCount != activities.Count || scrollToEnd
                || currentActivity?.Finished == false && DateTime.UtcNow >= nextHistoryLayout || nextHistoryLayout == DateTime.MinValue;
            if (refresh)
            {
                historyMessages = conversation.VisibleMessages.ToArray();
                historyRows.Clear();
                for (int index = 0; index <= historyMessages.Length; index++)
                {
                    foreach (CQFAIActivity activity in activities.Where(activity => activity.MessageIndex == index))
                        historyRows.Add((null, activity, ActivityHeight(activity, width), width));
                    if (index < historyMessages.Length)
                    {
                        CQFAIMessage message = historyMessages[index];
                        float cardWidth = message.Role == "system" ? width : ChatWidth(message.DisplayContent, width);
                        historyRows.Add((message, null, Text.CalcHeight(message.DisplayContent, cardWidth - 20f) + 32f, cardWidth));
                    }
                }
                historyWidth = width; historyMessageCount = conversation.Messages.Count; historyActivityCount = activities.Count;
                nextHistoryLayout = DateTime.UtcNow.AddMilliseconds(150);
            }
            CQFAIMessage[] messages = historyMessages;
            if (messages.Length == 0)
            {
                Color previous = GUI.color;
                GUI.color = CQFEditorPalette.Muted;
                Text.Anchor = TextAnchor.MiddleCenter;
                Widgets.Label(rect, "CQF_AI_EmptyChat".Translate());
                Text.Anchor = TextAnchor.UpperLeft;
                GUI.color = previous;
                return;
            }
            var rows = historyRows;
            float height = rows.Sum(row => row.height);
            bool follow = currentActivity?.Finished == false && chatScroll.y >= Mathf.Max(0f, historyHeight - rect.height) - 24f;
            if (scrollToEnd || follow) { chatScroll.y = Mathf.Max(0f, height - rect.height); scrollToEnd = false; }
            historyHeight = height;
            Widgets.BeginScrollView(rect, ref chatScroll, new Rect(0f, 0f, width, Mathf.Max(rect.height, height)));
            float y = 0f;
            foreach (var row in rows)
            {
                if (y + row.height < chatScroll.y || y > chatScroll.y + rect.height) { y += row.height; continue; }
                if (row.activity != null)
                {
                    DrawActivity(row.activity, new Rect(0f, y, width, row.height - 14f), chatScroll.y, chatScroll.y + rect.height);
                    y += row.height;
                    continue;
                }
                CQFAIMessage message = row.message!;
                bool player = message.Role == "user";
                float cardWidth = row.cardWidth;
                float textHeight = row.height - 32f;
                Rect card = new Rect(player ? width - cardWidth : 0f, y, cardWidth, textHeight + 18f);
                if (message.Role == "user" || message.Role == "assistant") CQFAIChatBubble.Draw(card, player ? CQFEditorPalette.Header : CQFEditorPalette.Card);
                Color previous = GUI.color;
                if (message.Role == "system") GUI.color = ColorLibrary.RedReadable;
                Widgets.Label(new Rect(card.x + 10f, card.y + 8f, card.width - 20f, textHeight), message.DisplayContent);
                GUI.color = previous;
                y += row.height;
            }
            Widgets.EndScrollView();
        }

        private static float ChatWidth(string content, float width)
        {
            return Mathf.Clamp(Text.CalcSize(content).x + 24f, 50f, width * 0.87f);
        }

        private float ActivityHeight(CQFAIActivity activity, float width)
        {
            Text.Font = GameFont.Tiny;
            float height = 52f;
            if (activity.Expanded)
            {
                if (activity.TaskState != null) height += CQFAITaskPanel.Height(activity.TaskState, width - 20f);
                if (activity.Notice.Length > 0) height += Text.CalcHeight(activity.Notice, width - 20f) + 8f;
                height += 24f + Text.CalcHeight(activity.Reasoning.Length > 0 ? activity.Reasoning : "CQF_AI_ReasoningUnavailable".Translate().ToString(), width - 20f) + 8f;
                height += activity.Operations.Sum(operation => OperationLayout(operation, width - 20f).height + 8f);
            }
            Text.Font = GameFont.Small;
            if (activity.Preview.Length > 0) height += Text.CalcHeight(activity.Preview, ChatWidth(activity.Preview, width) - 20f) + 28f;
            return height + 14f;
        }

        private void DrawActivity(CQFAIActivity activity, Rect rect, float visibleTop, float visibleBottom)
        {
            Color previous = GUI.color;
            GUI.color = CQFEditorPalette.Muted;
            Text.Font = GameFont.Tiny;
            Rect header = new Rect(rect.x + 10f, rect.y, rect.width - 20f, 24f);
            Widgets.Label(header, ((activity.Expanded ? "▾ " : "▸ ") + activity.Header).Truncate(header.width));
            TooltipHandler.TipRegion(header, "CQF_AI_ActivityHint".Translate());
            if (Widgets.ButtonInvisible(header)) { activity.Expanded = !activity.Expanded; nextHistoryLayout = DateTime.MinValue; }
            Rect step = new Rect(header.x, header.yMax, header.width, 24f);
            Widgets.Label(step, activity.CurrentStep.Truncate(step.width));
            TooltipHandler.TipRegion(step, activity.CurrentStep);
            float y = step.yMax + 4f;
            if (activity.Expanded)
            {
                if (activity.TaskState != null) y += CQFAITaskPanel.Draw(activity.TaskState, new Rect(header.x, y, header.width, 0f));
                if (activity.Notice.Length > 0)
                {
                    float noticeHeight = Text.CalcHeight(activity.Notice, header.width);
                    Widgets.Label(new Rect(header.x, y, header.width, noticeHeight), activity.Notice);
                    y += noticeHeight + 8f;
                }
                Widgets.Label(new Rect(header.x, y, header.width, 24f), "CQF_AI_Reasoning".Translate());
                y += 24f;
                string reasoning = activity.Reasoning.Length > 0 ? activity.Reasoning : "CQF_AI_ReasoningUnavailable".Translate().ToString();
                float height = Text.CalcHeight(reasoning, header.width);
                Widgets.Label(new Rect(header.x, y, header.width, height), reasoning);
                y += height + 8f;
                foreach (CQFAIOperation operation in activity.Operations)
                {
                    GUI.color = operation.Succeeded == false ? ColorLibrary.RedReadable : CQFEditorPalette.Muted;
                    var operationLayout = OperationLayout(operation, header.width);
                    float lineHeight = operationLayout.height;
                    Rect line = new Rect(header.x, y, header.width, lineHeight);
                    if (line.yMax >= visibleTop && line.y <= visibleBottom)
                    {
                        Widgets.Label(line, operationLayout.label);
                        if (operation.Details.Length > 0) TooltipHandler.TipRegion(line, operation.Details);
                    }
                    y += lineHeight + 8f;
                }
            }
            Text.Font = GameFont.Small;
            GUI.color = previous;
            if (activity.Preview.Length > 0)
            {
                float cardWidth = ChatWidth(activity.Preview, rect.width);
                float textHeight = Text.CalcHeight(activity.Preview, cardWidth - 20f);
                Rect card = new Rect(rect.x, y, cardWidth, textHeight + 18f);
                CQFAIChatBubble.Draw(card, CQFEditorPalette.Card);
                Widgets.Label(new Rect(card.x + 10f, card.y + 8f, card.width - 20f, textHeight), activity.Preview);
            }
        }

        private (string label, float height) OperationLayout(CQFAIOperation operation, float width)
        {
            if (operation.Succeeded != null && operationLayouts.TryGetValue(operation, out var finished) && finished.width == width) return (finished.label, finished.height);
            string label = operation.Label;
            if (!operationLayouts.TryGetValue(operation, out var cached) || cached.width != width || cached.label != label)
            {
                cached = (label, width, Text.CalcHeight(label, width));
                operationLayouts[operation] = cached;
            }
            return (cached.label, cached.height);
        }
        private void TrackTool(CQFAIToolCall call, XElement? result)
        {
            if (currentActivity == null) return;
            if (result == null) currentActivity.Add(new CQFAIOperation(call));
            else currentActivity.Operations.Last(operation => operation.Id == call.Id).Complete(result, CustomQuestFramework_ModSetting.setting.dialogAIKey);
            if (result != null) chatDirty = true;
        }
        private void DrawHeader(CQFAIWindowLayout layout)
        {
            string title = session.Title.Length > 0 ? session.Title : "CQF_AI_Open".Translate().ToString();
            Text.Anchor = TextAnchor.MiddleLeft;
            Widgets.Label(layout.Title, title.Truncate(layout.Title.width));
            Text.Anchor = TextAnchor.UpperLeft;
            TooltipHandler.TipRegion(layout.Title, title);
            if (CQFAIIconButton.Draw(layout.HeaderButton(0), CQFAIIcon.Close, "CloseButton".Translate())) Close();
            if (CQFAIIconButton.Draw(layout.HeaderButton(1), collapsed ? CQFAIIcon.Expand : CQFAIIcon.Collapse,
                (collapsed ? "CQF_AI_Expand" : "CQF_AI_Collapse").Translate())) ToggleCollapsed();
            if (CQFAIIconButton.Draw(layout.HeaderButton(2), CQFAIIcon.Settings, "CQF_AI_Settings".Translate()))
            {
                if (Find.WindowStack.TryGetWindow<CQFAISettingsWindow>(out CQFAISettingsWindow settings)) Find.WindowStack.Notify_ManuallySetFocus(settings);
                else Find.WindowStack.Add(new CQFAISettingsWindow());
            }
            bool enabled = GUI.enabled;
            bool undoAvailable = transaction?.CanUndo == true;
            bool undoCurrent = undoAvailable;
            string undoTip = transaction?.UndoSupported == false ? "CQF_AI_UndoUnsupported" : undoAvailable && !undoCurrent ? "CQF_AI_UndoConflict" : "CQF_DialogGraph_Undo";
            int historyIndex = layout.Compact ? 3 : 4;
            if (CQFAIIconButton.Draw(layout.HeaderButton(historyIndex), CQFAIIcon.History, "CQF_AI_History".Translate()))
            {
                if (Find.WindowStack.TryGetWindow<CQFAIHistoryWindow>(out CQFAIHistoryWindow history)) Find.WindowStack.Notify_ManuallySetFocus(history);
                else Find.WindowStack.Add(new CQFAIHistoryWindow(this));
            }
            if (layout.Compact)
            {
                if (CQFAIIconButton.Draw(layout.HeaderButton(4), CQFAIIcon.More, "CQF_AI_More".Translate()))
                    Find.WindowStack.Add(new FloatMenu(new List<FloatMenuOption>
                    {
                        new FloatMenuOption("CQF_AI_NewChat".Translate(), NewChat),
                        new FloatMenuOption(undoTip.Translate(), !IsWorking && undoCurrent ? Undo : (Action?)null)
                    }));
                return;
            }
            GUI.enabled = enabled && !IsWorking && undoCurrent;
            if (CQFAIIconButton.Draw(layout.HeaderButton(3), CQFAIIcon.Undo, undoTip.Translate())) Undo();
            GUI.enabled = enabled;
            if (CQFAIIconButton.Draw(layout.HeaderButton(5), CQFAIIcon.NewChat, "CQF_AI_NewChat".Translate())) NewChat();
            GUI.enabled = enabled;
        }

        private void DrawComposer(CQFAIWindowLayout layout)
        {
            Rect rect = layout.Composer;
            CQFAIIconButton.DrawSurface(rect, CQFEditorPalette.Border);
            CQFAIIconButton.DrawSurface(rect.ContractedBy(1f), CQFEditorPalette.Canvas);
            if (composerStyle == null)
            {
                composerStyle = new GUIStyle(Text.CurTextAreaStyle) { padding = new RectOffset(0, 0, 0, 0) };
                composerStyle.normal.background = null;
                composerStyle.hover.background = null;
                composerStyle.focused.background = null;
                composerStyle.active.background = null;
            }
            Rect input = layout.Input;
            UnityEngine.Event inputEvent = UnityEngine.Event.current;
            bool sendShortcut = inputEvent.type == EventType.KeyDown && inputEvent.control
                && (inputEvent.keyCode == KeyCode.Return || inputEvent.keyCode == KeyCode.KeypadEnter)
                && GUI.enabled && GUI.GetNameOfFocusedControl() == "CQF_AI_Command";
            if (sendShortcut) inputEvent.Use();
            GUI.SetNextControlName("CQF_AI_Command");
            command = GUI.TextArea(input, command, composerStyle);
            if (sendShortcut && !string.IsNullOrWhiteSpace(command)) Submit();
            if (command.Length == 0 && GUI.GetNameOfFocusedControl() != "CQF_AI_Command")
            {
                Color previous = GUI.color;
                GUI.color = CQFEditorPalette.Muted;
                Widgets.Label(input, "CQF_AI_InputHint".Translate());
                GUI.color = previous;
            }
            bool enabled = GUI.enabled;
            GUI.enabled = enabled && !IsWorking;
            if (context?.Read() is CustomMapDataDef && CQFAIBridge.GenerateMap != null
                && CQFAIIconButton.Draw(new Rect(rect.x + 12f, layout.Send.y, 32f, 32f), CQFAIIcon.Options, "CQF_AI_GenerateMap".Translate())) ShowOptions();
            GUI.enabled = enabled && (IsWorking || !string.IsNullOrWhiteSpace(command));
            bool steering = IsWorking && !string.IsNullOrWhiteSpace(command);
            if (CQFAIIconButton.Draw(layout.Send, !IsWorking || steering ? CQFAIIcon.Send : CQFAIIcon.Stop,
                (!IsWorking ? "CQF_AI_SendHint" : steering ? "CQF_AI_SteerHint" : "CQF_AI_Stop").Translate(), true))
            {
                if (!IsWorking || steering) Submit(); else Cancel();
            }
            GUI.enabled = enabled;
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
        private static string UsageValue(CQFAITokenTotals totals) => totals.Reported > 0 ? totals.Total.ToString("N0") + (totals.Unavailable > 0 ? "+?" : "") : "CQF_AI_UsageUnknown".Translate().ToString();

        private void RestoreSession(CQFAISession value)
        {
            session = value;
            conversation.Clear();
            activities.Clear(); currentActivity = null; historyHeight = 0f;
            operationLayouts.Clear(); historyRows.Clear(); historyMessages = Array.Empty<CQFAIMessage>(); nextHistoryLayout = DateTime.MinValue;
            requestBudget = null;
            chatDirty = false;
            foreach (CQFAIMessage message in value.Messages) conversation.Add(message.Role, message.DisplayContent, visible: true);
            activities.AddRange(value.Activities);
            taskUsage.Restore(value.TaskUsage.Save("usage")); sessionUsage.Restore(value.SessionUsage.Save("usage"));
            command = value.Draft; savedDraft = command;
            transaction = null; harness = null; pendingSteering = false;
            status = string.Empty;
            statusIsError = false;
            chatScroll = Vector2.zero;
            scrollToEnd = true;
        }
        private void SaveChat()
        {
            nextSave = DateTime.UtcNow.AddSeconds(2);
            try
            {
                session.Updated = DateTime.UtcNow; session.Draft = command;
                session.Messages.Clear(); session.Messages.AddRange(conversation.VisibleMessages);
                session.Activities.Clear(); session.Activities.AddRange(activities);
                session.TaskUsage.Restore(taskUsage.Save("usage")); session.SessionUsage.Restore(sessionUsage.Save("usage"));
                if (session.Title.Length == 0)
                {
                    string title = session.Messages.FirstOrDefault(message => message.Role == "user")?.DisplayContent.Replace('\n', ' ').Trim() ?? "";
                    session.Title = title.Length > 40 ? title.Substring(0, 40) : title;
                }
                store.Save(session); savedDraft = command; chatDirty = false;
            }
            catch (Exception error) { nextSave = DateTime.UtcNow.AddSeconds(30); HistoryError(error); }
        }
        private void HistoryError(Exception error)
        {
            status = "CQF_AI_HistoryError".Translate(error.Message); statusIsError = true;
            Log.Error("CQF AI history: " + error);
            Messages.Message(status, MessageTypeDefOf.RejectInput);
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
            context = value;
        }

        private void TrackMapProgress(CQFAIToolCall call, CQFAILiveMapTransaction execution)
        {
            if (currentActivity == null) return;
            string kind = execution.CurrentOperation?.Name.LocalName ?? "edit";
            string key = "CQF_AI_MapStep_" + kind;
            string label = key.CanTranslate() ? key.Translate().ToString() : "CQF_AI_ToolApply".Translate().ToString();
            if (execution.ExecutionPhase != "CQF_AI_Executing") label = execution.ExecutionPhase.Translate();
            string line = "CQF_AI_MapProgress".Translate(label, execution.CompletedOperations, execution.TotalOperations);
            if (execution.CurrentOperation is XElement receipt && receipt.Attribute("x") != null && receipt.Attribute("z") != null)
                line += " · (" + receipt.Attribute("x")!.Value + ", " + receipt.Attribute("z")!.Value + ")";
            currentActivity.ExecutionProgress = line;
            CQFAIOperation? operation = currentActivity.Operations.LastOrDefault(value => value.Id == call.Id);
            if (operation != null) operation.Progress = line;
        }
        private void StopExecution()
        {
            try { harness?.CancelResponse(); }
            catch (Exception error)
            {
                string secret = CustomQuestFramework_ModSetting.setting.dialogAIKey;
                string details = string.IsNullOrEmpty(secret) ? error.ToString() : error.ToString().Replace(secret, "***");
                if (currentActivity != null) currentActivity.Notice = "CQF_DialogGraph_Error".Translate(details);
                Log.Error("CQF AI execution cleanup: " + details);
            }
            finally { if (harness?.Transaction?.CanUndo == true) transaction = harness.Transaction; }
        }
        private void ToggleCollapsed()
        {
            if (collapsed) windowRect.size = expandedSize;
            else { expandedSize = windowRect.size; windowRect.height = 72f; }
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
                harness = new CQFAIHarness(model, catalog, conversation, null, command, setting.dialogAIAllowEditing && CQFEditorBridge.IsLoaded, setting.dialogAIAllowTextGeneration, nativeTools,
                    new CQFAITargetCatalog(() => CQFAITargetCatalog.Discover(model)), setting.dialogAIAdditionalPromptEnabled ? setting.dialogAIAdditionalPrompt : string.Empty);
                taskUsage.Clear();
                conversation.Add("user", command);
                currentActivity = new CQFAIActivity(conversation.VisibleMessages.Count());
                activities.Add(currentActivity);
                harness.ToolProgress = TrackTool;
                harness.MapProgress = TrackMapProgress;
                harness.MapStepApplied = mapFeedback.Add;
                command = string.Empty;
                requestBudget = new CQFAIRequestBudget(Math.Max(1000, setting.dialogAITokenBudget), Math.Max(1, setting.dialogAIRequestBudget));
                client = new CQFDialogAIClient(setting.dialogAIEndpoint, setting.dialogAIModel, setting.dialogAIKey, setting.dialogAITimeout) { RequestBudget = requestBudget };
                cancellation = new CancellationTokenSource();
                if (setting.dialogAIPlanning)
                {
                    CQFAITaskState? previous = activities.Take(activities.Count - 1).LastOrDefault()?.TaskState;
                    CQFAITaskState state = previous?.Status == "paused" ? CQFAITaskState.Restore(previous.Save()) : new CQFAITaskState();
                    state.Resume(); currentActivity.TaskState = state;
                    string endpoint = setting.dialogAIEndpoint, agentModel = string.IsNullOrWhiteSpace(setting.dialogAIAgentModel) ? setting.dialogAIModel : setting.dialogAIAgentModel;
                    string key = setting.dialogAIKey, prompt = setting.dialogAIAgentPrompt;
                    int timeout = setting.dialogAITimeout;
                    CancellationToken token = cancellation.Token;
                    CQFAIOrchestration? orchestration = setting.dialogAIAgents ? new CQFAIOrchestration(state, setting.dialogAIParallelAgents,
                        record => CreateAgent(record, endpoint, agentModel, key, timeout, prompt, token), error => AgentError(error, key)) : null;
                    harness.AttachTask(state, orchestration);
                }
                RequestMain();
                status = "CQF_DialogAI_Working".Translate();
                statusIsError = false;
                scrollToEnd = true;
                SaveChat();
            }
            catch (Exception error) { Error(error); }
        }

        private void Submit()
        {
            if (!IsWorking) { Start(); return; }
            try
            {
                if (string.IsNullOrWhiteSpace(command)) return;
                harness!.AddInstruction(command);
                harness.Orchestration?.ContinueMain();
                command = string.Empty; pendingSteering = true;
                status = "CQF_AI_Steering".Translate(); statusIsError = false; scrollToEnd = true;
                SaveChat();
            }
            catch (Exception error) { Error(error); }
        }

        private CQFAIAgentRunner CreateAgent(CQFAIAgentRecord record, string endpoint, string agentModel, string key, int timeout, string prompt, CancellationToken token)
        {
            CQFAIConversation workerConversation = new CQFAIConversation(model, catalog);
            CQFAIHarness workerHarness = new CQFAIHarness(model, catalog, workerConversation, null, record.Assignment, false, false, nativeTools,
                new CQFAITargetCatalog(() => CQFAITargetCatalog.Discover(model)), prompt, inspectionOnly: true);
            workerConversation.Add("user", record.Assignment);
            CQFDialogAIClient workerClient = new CQFDialogAIClient(endpoint, agentModel, key, timeout) { RequestBudget = requestBudget };
            bool accounted = false;
            return new CQFAIAgentRunner(record, workerHarness, (worker, cancellationToken) =>
            {
                accounted = false;
                return workerClient.CompleteConversationAsync(worker.Instructions, worker.RequestMessages, cancellationToken, nativeTools ? worker.Registry.Tools : null, stream: true);
            }, token, () => workerClient.LastProgress, () =>
            {
                if (accounted || !workerClient.ReceivedResponse) return;
                accounted = true; taskUsage.Add(workerClient.LastUsage); sessionUsage.Add(workerClient.LastUsage);
                if (workerClient.UsageError != null) Log.Warning("CQF AI agent: " + "CQF_AI_InvalidUsage".Translate());
            });
        }

        private static string AgentError(Exception error, string secret)
        {
            string key = error.Message.Split(new[] { ':', ' ' }, 2)[0];
            string message = error is OperationCanceledException ? "CQF_DialogAI_TimedOut".Translate().ToString() : key.CanTranslate() ? key.Translate() + error.Message.Substring(key.Length) : error.Message;
            string details = error.ToString();
            if (!string.IsNullOrEmpty(secret)) { message = message.Replace(secret, "***"); details = details.Replace(secret, "***"); }
            Log.Warning("CQF AI agent: " + details);
            return message.Length > 32768 ? message.Substring(0, 32768) : message;
        }

        private void RequestMain()
        {
            harness!.CollectAgentReports();
            pendingSteering = false;
            task = client!.CompleteConversationAsync(harness.Instructions, harness.RequestMessages, cancellation!.Token, nativeTools ? harness.Registry.Tools : null, stream: true);
        }

        private void Poll()
        {
            if (harness != null)
            {
                if (!CustomQuestFramework_ModSetting.setting.dialogAIEnabled) { Cancel(); return; }
                harness.PollAgents();
                if (requestBudget?.IsBlocked == true) { PauseBudget(); return; }
                if (harness.HasPendingResponse)
                {
                    if (harness.ActiveMapExecution != null && !CustomQuestFramework_ModSetting.setting.dialogAIAllowEditing) { Cancel(); return; }
                    if (pendingSteering) { StopExecution(); currentActivity?.NextRound(); RequestMain(); return; }
                    if (DateTime.UtcNow < nextExecution) return;
                    int speed = Mathf.Clamp(CustomQuestFramework_ModSetting.setting.dialogAIExecutionSpeed, 0, 3);
                    nextExecution = DateTime.UtcNow.AddMilliseconds(speed == 0 ? 100 : speed == 1 ? 50 : 0);
                    bool finished;
                    try { finished = harness.AdvanceResponse(speed == 0 ? 1 : speed == 1 ? 8 : speed == 2 ? 48 : int.MaxValue, speed == 3 ? 6 : 3); }
                    finally { if (harness.Transaction?.CanUndo == true) transaction = harness.Transaction; }
                    if (finished) { FinishResponse(harness.ResponseContinued); chatDirty = true; }
                    return;
                }
                if (task == null)
                {
                    if (harness.Orchestration?.Waiting == true && !pendingSteering) { currentActivity?.WaitForAgents(); return; }
                    currentActivity?.NextRound(); RequestMain();
                }
            }
            if (task != null)
            {
                currentActivity?.Update(client?.LastProgress);
                if (currentActivity != null && client?.TransportNotice != null) currentActivity.Notice = client.TransportNotice.Translate();
            }
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
                currentActivity?.Update(client?.LastProgress);
                if (pendingSteering)
                {
                    currentActivity?.NextRound(); RequestMain();
                    return;
                }
                if (!CustomQuestFramework_ModSetting.setting.dialogAIEnabled) throw new InvalidOperationException("CQF_DialogAI_Disabled");
                if (harness?.Transaction != null && !CustomQuestFramework_ModSetting.setting.dialogAIAllowEditing) throw new InvalidDataException("CQF_AI_ReadOnly");
                harness!.BeginResponse(response);
                nextExecution = DateTime.MinValue;
            }
            catch (OperationCanceledException)
            {
                currentActivity?.Complete("CQF_AI_ActivityStopped", true);
                bool userCancelled = cancellation?.IsCancellationRequested == true;
                cancellation?.Cancel(); harness?.Orchestration?.Stop(); currentActivity?.TaskState?.Pause();
                status = (userCancelled ? "CQF_DialogAI_Cancelled" : "CQF_DialogAI_TimedOut").Translate();
                statusIsError = true;
                cancellation?.Dispose(); cancellation = null; harness = null; pendingSteering = false;
            }
            catch (Exception error) { Error(error); }
            finally { SaveChat(); }
        }

        private void FinishResponse(bool continued)
        {
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
                currentActivity?.NextRound();
                if (harness.Orchestration?.Waiting != true) RequestMain();
                else currentActivity?.WaitForAgents();
                if (failures.Length == 0) { status = "CQF_AI_ToolWorking".Translate(harness.Rounds); statusIsError = false; }
                scrollToEnd = true;
                return;
            }
            currentActivity?.Complete(harness.TaskState?.Status == "blocked" ? "CQF_AI_ActivityBlocked" : "CQF_AI_ActivityDone");
            status = (harness.Transaction?.CanUndo != true ? "CQF_AI_Replied" : harness.Transaction is CQFAILiveMapTransaction ? "CQF_AI_AppliedMap" : "CQF_AI_AppliedLive").Translate();
            statusIsError = false;
            cancellation?.Cancel(); harness.Orchestration?.Stop(); cancellation?.Dispose();
            cancellation = null;
            harness = null;
            scrollToEnd = true;
        }

        private void Undo()
        {
            try
            {
                transaction!.Undo();
                conversation.Add("system", "The player undid the last AI edit. Use the latest editor data for subsequent changes.", "CQF_AI_Undone".Translate());
                status = "CQF_AI_Undone".Translate();
                statusIsError = false;
                scrollToEnd = true;
                SaveChat();
            }
            catch (Exception error) { Error(error); }
        }

        private void Cancel()
        {
            StopExecution();
            currentActivity?.Update(client?.LastProgress);
            currentActivity?.Complete("CQF_AI_ActivityStopped", true);
            if (task != null && client?.ReceivedResponse == true) { taskUsage.Add(client.LastUsage); sessionUsage.Add(client.LastUsage); }
            cancellation?.Cancel();
            harness?.Orchestration?.Stop(); currentActivity?.TaskState?.Pause(); pendingSteering = false;
            task?.ContinueWith(completed => { _ = completed.Exception; }, TaskContinuationOptions.OnlyOnFaulted);
            task = null;
            cancellation?.Dispose();
            cancellation = null;
            harness = null;
            status = "CQF_DialogAI_Cancelled".Translate();
            statusIsError = false;
            SaveChat();
        }

        private void Error(Exception error)
        {
            if (error.Message == "CQF_AI_BudgetPaused") { PauseBudget(); return; }
            StopExecution();
            currentActivity?.Complete("CQF_AI_ActivityFailed", true);
            cancellation?.Cancel();
            harness?.Orchestration?.Stop(); currentActivity?.TaskState?.Pause(); pendingSteering = false;
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
            SaveChat();
        }

        private void PauseBudget()
        {
            Cancel();
            status = "CQF_AI_BudgetPaused".Translate(); statusIsError = false;
            conversation.Add("system", "Task paused at its shared request/token budget. Completed changes remain applied. Continue only after a new player instruction, with fresh state reads and a new budget.", status, true);
            Log.Warning("CQF AI: " + status);
            scrollToEnd = true; SaveChat();
        }
        private CQFAIEditorContext? context;
        private readonly CQFAISessionStore store;
        private CQFAISession session = new CQFAISession();
        private string savedDraft = string.Empty;
        private DateTime nextSave;
        private readonly CQFAIModel model;
        private readonly CQFAIResourceCatalog catalog;
        private readonly CQFAIConversation conversation;
        private readonly List<CQFAIActivity> activities = new List<CQFAIActivity>();
        private CQFAIActivity? currentActivity;
        private readonly CQFAIMapFeedback mapFeedback = new CQFAIMapFeedback();
        private DateTime nextExecution;
        private float historyHeight;
        private readonly List<(CQFAIMessage? message, CQFAIActivity? activity, float height, float cardWidth)> historyRows = new List<(CQFAIMessage?, CQFAIActivity?, float, float)>();
        private readonly Dictionary<CQFAIOperation, (string label, float width, float height)> operationLayouts = new Dictionary<CQFAIOperation, (string, float, float)>();
        private CQFAIMessage[] historyMessages = Array.Empty<CQFAIMessage>();
        private int historyMessageCount;
        private int historyActivityCount;
        private float historyWidth;
        private DateTime nextHistoryLayout;
        private readonly CQFAITokenTotals taskUsage = new CQFAITokenTotals();
        private readonly CQFAITokenTotals sessionUsage = new CQFAITokenTotals();
        private CQFAIRequestBudget? requestBudget;
        private CQFAITransaction? transaction;
        private CQFAIHarness? harness;
        private Task<string>? task;
        private CancellationTokenSource? cancellation;
        private CQFDialogAIClient? client;
        private string command = string.Empty;
        private string status = string.Empty;
        private bool nativeTools;
        private bool pendingSteering;
        private bool collapsed;
        private bool scrollToEnd;
        private bool statusIsError;
        private bool chatDirty;
        private GUIStyle? composerStyle;
        private Vector2 expandedSize;
        private Vector2 chatScroll;
    }
}
