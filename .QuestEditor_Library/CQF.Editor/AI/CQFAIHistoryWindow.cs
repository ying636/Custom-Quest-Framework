using UnityEngine;
using Verse;

namespace QuestEditor_Library
{
    public sealed class CQFAIHistoryWindow : Window
    {
        public CQFAIHistoryWindow(CQFAIWindow owner)
        {
            this.owner = owner; draggable = true; doCloseX = true; closeOnAccept = false; absorbInputAroundWindow = false; preventCameraMotion = false;
            sessions = owner.History();
        }
        public override Vector2 InitialSize => new Vector2(Mathf.Min(UI.screenWidth - 40f, 420f), Mathf.Min(UI.screenHeight - 40f, 500f));
        public override void DoWindowContents(Rect rect)
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
                Widgets.Label(new Rect(0f, 0f, rect.width - 70f, 28f), "CQF_AI_History".Translate());
                if (CQFAIIconButton.Draw(new Rect(rect.width - 65f, 0f, 28f, 28f), CQFAIIcon.NewChat, "CQF_AI_NewChat".Translate())) { owner.NewChat(); Close(); }
                Rect body = new Rect(0f, 38f, rect.width, rect.height - 38f);
                if (sessions.Count == 0) { Widgets.Label(body, "CQF_AI_HistoryEmpty".Translate()); return; }
                float width = body.width - 20f;
                Widgets.BeginScrollView(body, ref scroll, new Rect(0f, 0f, width, Mathf.Max(body.height, sessions.Count * 80f)));
                for (int index = 0; index < sessions.Count; index++)
                {
                    CQFAISessionEntry session = sessions[index];
                    Rect row = new Rect(0f, index * 80f, width, 74f);
                    Rect content = new Rect(row.x, row.y, row.width - 40f, row.height);
                    if (session.Id == owner.SessionId || Mouse.IsOver(row)) Widgets.DrawBoxSolid(row, CQFEditorPalette.Header);
                    string title = session.Title.Length > 0 ? session.Title : "CQF_AI_NewChat".Translate().ToString();
                    Action delete = () => Find.WindowStack.Add(Dialog_MessageBox.CreateConfirmation("CQF_AI_DeleteChatConfirm".Translate(title),
                        () => { owner.DeleteChat(session.Id); sessions = owner.History(); }, destructive: true));
                    if (CQFAIIconButton.Draw(new Rect(row.xMax - 32f, row.y + 5f, 26f, 26f), CQFAIIcon.Clear, "CQF_AI_DeleteChat".Translate())) delete();
                    Widgets.Label(new Rect(8f, row.y + 3f, content.width - 16f, 24f), title.Truncate(content.width - 16f));
                    Text.Font = GameFont.Tiny;
                    Widgets.Label(new Rect(8f, row.y + 27f, content.width - 16f, 20f), session.Preview.Truncate(content.width - 16f));
                    Widgets.Label(new Rect(8f, row.y + 49f, content.width - 16f, 20f), session.Updated.ToLocalTime().ToString("yyyy-MM-dd HH:mm"));
                    Text.Font = GameFont.Small;
                    TooltipHandler.TipRegion(content, "CQF_AI_HistoryHint".Translate(title));
                    if (UnityEngine.Event.current.type == EventType.MouseDown && UnityEngine.Event.current.button == 1 && Mouse.IsOver(content))
                    {
                        UnityEngine.Event.current.Use();
                        Find.WindowStack.Add(new FloatMenu(new List<FloatMenuOption>
                        {
                            new FloatMenuOption("CQF_AI_RenameChat".Translate(), () => Find.WindowStack.Add(new CQFAIRenameSessionWindow(title, name => { owner.RenameChat(session.Id, name); sessions = owner.History(); }))),
                            new FloatMenuOption("CQF_AI_DeleteChat".Translate(), delete)
                        }));
                    }
                    if (Widgets.ButtonInvisible(content)) { owner.SwitchChat(session.Id); Close(); }
                }
                Widgets.EndScrollView();
            }
            finally { Text.Font = previousFont; Text.Anchor = previousAnchor; Text.WordWrap = previousWrap; }
        }
        private readonly CQFAIWindow owner;
        private IReadOnlyList<CQFAISessionEntry> sessions;
        private Vector2 scroll;
    }
}
