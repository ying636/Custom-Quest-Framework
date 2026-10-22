using RimWorld;
using UnityEngine;
using Verse;

namespace QuestEditor_Library
{
    public sealed class Window_CQFEntityPlacement : Window
    {
        public Window_CQFEntityPlacement(Spawner marker, Quest? quest = null)
        {
            this.marker = marker;
            this.quest = quest;
            doCloseX = true;
            forcePause = true;
            draggable = true;
            absorbInputAroundWindow = false;
            preventCameraMotion = false;
            closeOnAccept = false;
        }

        public override Vector2 InitialSize => new(380f, 310f);

        public override void DoWindowContents(Rect inRect)
        {
            using CQFUIScope scope = new(inRect.width, inRect.height);
            Text.Font = GameFont.Medium;
            Widgets.Label(new Rect(0f, 0f, inRect.width - 30f, 32f), "CQF_EntityBook_PlacementReady".Translate().Colorize(CQFUIStyle.Accent));
            Text.Font = GameFont.Small;
            if (!marker.Spawned || marker.Destroyed || marker.pawns.Count == 0)
            {
                Widgets.Label(new Rect(0f, 42f, inRect.width, 60f), "CQF_EntityBook_PlacementMissing".Translate());
                return;
            }
            PawnSpawnData recipe = marker.pawns[0];
            Rect name = new(0f, 40f, inRect.width, 28f);
            Widgets.Label(name, CQFEntityBookCatalog.LabelFor(recipe));
            TooltipHandler.TipRegion(name, "CQF_EntityBook_PlacementDescription".Translate());
            Widgets.Label(new Rect(0f, 78f, inRect.width * 0.52f, 28f), "CQF_EntityBook_PlacementMember".Translate());
            Rect memberRect = new(inRect.width * 0.55f, 74f, inRect.width * 0.45f, 30f);
            Widgets.TextFieldNumeric(memberRect, ref memberNumber, ref memberBuffer, 1, int.MaxValue);
            TooltipHandler.TipRegion(memberRect, "CQF_EntityBook_PlacementMemberTip".Translate());
            if (CQFUIStyle.ButtonText(new Rect(0f, 116f, inRect.width, 32f), "CQF_EntityBook_PlacementRecordMove".Translate()))
                Run(() => OpenStudio().RecordEntityMove(CQFEntityReference.Encode(recipe.EntityBookSourceId, memberNumber - 1)));
            if (CQFUIStyle.ButtonText(new Rect(0f, 154f, inRect.width, 32f), "CQF_EntityBook_PlacementAddActor".Translate()))
                Run(() => OpenStudio().AddEntityRole(CQFEntityReference.Encode(recipe.EntityBookSourceId, memberNumber - 1)));
            float half = (inRect.width - 6f) / 2f;
            if (CQFUIStyle.ButtonText(new Rect(0f, 192f, half, 32f), "CQF_EntityBook_PlacementEdit".Translate()))
            {
                Find.WindowStack.Add(new Dialog_CQFEntityRecipe(CQFEntityRecipeStore.Copy(recipe), accepted: changed =>
                {
                    if (!marker.Spawned || marker.Destroyed || marker.pawns.Count == 0)
                        throw new InvalidOperationException("CQF_EntityBook_PlacementMissing".Translate());
                    AcceptanceReport report = CQFEntityMapPlacement.CanPlace(marker.Position, marker.Map, changed, marker);
                    if (!report.Accepted) throw new InvalidOperationException(report.Reason);
                    marker.pawns[0] = CQFEntityRecipeStore.Copy(changed);
                    CQFEntityBookCatalog.Track(marker.pawns[0]);
                }, acceptLabelKey: "CQF_EntityBook_PlacementApply"));
            }
            if (CQFUIStyle.ButtonText(new Rect(half + 6f, 192f, half, 32f), "CQF_EntityBook_PlacementContinue".Translate()))
                Run(() => CQFEntityMapPlacement.Begin(recipe, quest));
            if (CQFUIStyle.ButtonText(new Rect(0f, 230f, inRect.width, 32f), "Close".Translate())) Close();
        }

        private QuestEditor_Cinematics OpenStudio()
        {
            QuestEditor_Cinematics? studio = Find.WindowStack.WindowOfType<QuestEditor_Cinematics>();
            bool create = studio == null;
            studio ??= new QuestEditor_Cinematics();
            string expectedQuest = quest?.GetUniqueLoadID() ?? "";
            if (studio.Project.QuestID != expectedQuest)
            {
                if (studio.Project.Definition.steps.Count > 0 || studio.Project.Definition.roles.Count > 0)
                    throw new InvalidOperationException("CQF_EntityBook_PlacementQuestMismatch".Translate());
                studio.Project.Quest = quest;
            }
            if (create) Find.WindowStack.Add(studio);
            return studio;
        }

        private void Run(Action action)
        {
            try
            {
                action();
                Close();
            }
            catch (Exception error) { CQFEntityBookCatalog.ReportError(error); }
        }

        private readonly Spawner marker;
        private readonly Quest? quest;
        private int memberNumber = 1;
        private string memberBuffer = "1";
    }
}
