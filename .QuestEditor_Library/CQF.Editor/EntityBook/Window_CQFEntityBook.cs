using RimWorld;
using UnityEngine;
using Verse;

namespace QuestEditor_Library
{
    public sealed class Window_CQFEntityBook : Window
    {
        public Window_CQFEntityBook(Action<string>? selected = null, Action<PawnSpawnData>? recipeSelected = null, bool allowGroups = false, Quest? quest = null)
        {
            this.selected = selected;
            this.recipeSelected = recipeSelected;
            this.allowGroups = allowGroups;
            this.quest = quest;
            forcePause = true;
            doCloseX = true;
            draggable = true;
            resizeable = true;
            closeOnClickedOutside = false;
        }

        public override Vector2 InitialSize => new(980f, 700f);

        public bool HasVisibleSelection => current != null && MatchesFilters(current) && entries.Any(entry => entry.SourceId == current.SourceId);

        public void SynchronizeSelection()
        {
            if (!ReferenceEquals(filteredSource, entries) || filteredSearch != search || filteredKind != filter)
            {
                visibleEntries = entries.Where(MatchesFilters).ToList();
                filteredSource = entries;
                filteredSearch = search;
                filteredKind = filter;
            }
            CQFEntityBookEntry? next = current == null ? null : visibleEntries.FirstOrDefault(entry => entry.SourceId == current.SourceId);
            next ??= visibleEntries.FirstOrDefault();
            if (current?.SourceId != next?.SourceId) SelectEntry(next);
            else current = next;
        }

        public override void PreOpen()
        {
            base.PreOpen();
            Refresh();
        }

        public override void PostClose()
        {
            ReleasePreview();
            base.PostClose();
        }

        public override void WindowUpdate()
        {
            base.WindowUpdate();
            windowRect.width = Mathf.Clamp(windowRect.width, Mathf.Min(640f, UI.screenWidth - 20f), UI.screenWidth - 20f);
            windowRect.height = Mathf.Clamp(windowRect.height, Mathf.Min(420f, UI.screenHeight - 20f), UI.screenHeight - 20f);
        }

        public override void DoWindowContents(Rect inRect)
        {
            using CQFUIScope scope = new(inRect.width, inRect.height);
            Text.Font = GameFont.Medium;
            Widgets.Label(new Rect(0f, 0f, inRect.width - 40f, 32f), "CQF_EntityBook_Title".Translate().Colorize(CQFUIStyle.Accent));
            Text.Font = GameFont.Small;
            Rect context = new(0f, 36f, inRect.width - 136f, 26f);
            Widgets.Label(context, (quest == null ? "CQF_EntityBook_ContextUnassigned".Translate() : "CQF_EntityBook_ContextQuest".Translate(quest.name)).Colorize(CQFUIStyle.Muted));
            TooltipHandler.TipRegion(context, "CQF_EntityBook_ContextTip".Translate());
            if (CQFUIStyle.ButtonText(new Rect(inRect.width - 128f, 34f, 128f, 30f), "CQF_EntityBook_Refresh".Translate())) Refresh(true);
            float listWidth = Mathf.Clamp(inRect.width * 0.46f, 260f, 430f);
            string previousSearch = search;
            search = Widgets.TextField(new Rect(0f, 74f, listWidth, 30f), search);
            if (previousSearch != search) listScroll = Vector2.zero;
            TooltipHandler.TipRegion(new Rect(0f, 74f, listWidth, 30f), "CQF_EntityBook_SearchTip".Translate());
            float tabWidth = (listWidth - 12f) / 3f;
            for (int index = 0; index < 3; index++)
            {
                Rect tab = new(index * (tabWidth + 6f), 112f, tabWidth, 30f);
                if (filter == index) CQFUIStyle.DrawHighlightSelected(tab);
                if (CQFUIStyle.ButtonText(tab, new[] { "CQF_EntityBook_FilterAll", "CQF_EntityBook_FilterRecipes", "CQF_EntityBook_FilterTemplates" }[index].Translate(), false))
                {
                    filter = index;
                    listScroll = Vector2.zero;
                }
            }
            Rect list = new(0f, 150f, listWidth, Mathf.Max(40f, inRect.height - 194f));
            DrawEntries(list);
            Rect details = new(listWidth + 12f, 74f, inRect.width - listWidth - 12f, Mathf.Max(60f, inRect.height - 74f));
            DrawDetails(details);
            if (CQFUIStyle.ButtonText(new Rect(0f, inRect.height - 34f, listWidth, 32f), "CQF_EntityBook_NewRecipe".Translate()))
                Find.WindowStack.Add(new Dialog_CQFEntityRecipe(new PawnSpawnData(), () => Refresh()));
        }

        private void Refresh(bool reload = false)
        {
            try
            {
                ReleasePreview();
                if (reload) CQFEntityBookCatalog.Reload();
                entries = CQFEntityBookCatalog.GetEntries(quest);
                if (current != null) current = entries.FirstOrDefault(entry => entry.SourceId == current.SourceId);
                SynchronizeSelection();
                error = null;
            }
            catch (Exception exception)
            {
                error = exception.Message;
                CQFEntityBookCatalog.ReportError(exception);
            }
        }

        private void DrawEntries(Rect rect)
        {
            CQFUIStyle.DrawMenuSection(rect);
            SynchronizeSelection();
            Rect viewport = rect.ContractedBy(6f);
            Rect content = new(0f, 0f, viewport.width - 18f, Mathf.Max(viewport.height, visibleEntries.Count * 74f));
            Widgets.BeginScrollView(viewport, ref listScroll, content);
            int firstVisible = Mathf.Max(0, Mathf.FloorToInt(listScroll.y / 74f));
            int lastVisible = Mathf.Min(visibleEntries.Count, Mathf.CeilToInt((listScroll.y + viewport.height) / 74f));
            for (int index = firstVisible; index < lastVisible; index++)
            {
                CQFEntityBookEntry entry = visibleEntries[index];
                Rect row = new(0f, index * 74f, content.width, 68f);
                if (current?.SourceId == entry.SourceId) CQFUIStyle.DrawHighlightSelected(row);
                else Widgets.DrawHighlightIfMouseover(row);
                IReadOnlyList<CQFEntityRecord> records = Records(entry);
                Pawn? pawn = records.FirstOrDefault(CanUseRecord)?.Pawn;
                string status = Status(records);
                Rect icon = new(row.x + 6f, row.y + 8f, 48f, 48f);
                if (pawn != null) Widgets.ThingIcon(icon, pawn);
                else if (entry.Kind?.race != null) Widgets.DefIcon(icon, entry.Kind.race);
                using (new CQFUIScope())
                {
                    Text.WordWrap = false;
                    Widgets.Label(new Rect(62f, row.y + 6f, row.width - 68f, 25f), entry.Label.Truncate(row.width - 68f));
                    Text.Font = GameFont.Tiny;
                    Widgets.Label(new Rect(62f, row.y + 34f, row.width - 68f, 25f), (entry.Origin + " · " + status).Truncate(row.width - 68f).Colorize(CQFUIStyle.Muted));
                }
                TooltipHandler.TipRegion(row, entry.Label + "\n" + entry.Origin + "\n" + status);
                if (Widgets.ButtonInvisible(row))
                {
                    SelectEntry(entry);
                }
            }
            if (visibleEntries.Count == 0) Widgets.Label(new Rect(10f, 10f, content.width - 20f, 60f), "CQF_EntityBook_Empty".Translate().Colorize(CQFUIStyle.Muted));
            Widgets.EndScrollView();
        }

        private void DrawDetails(Rect rect)
        {
            CQFUIStyle.DrawMenuSection(rect);
            useRecipeRect = default;
            selectEntityRect = default;
            selectGroupRect = default;
            placeOnMapRect = default;
            if (!HasVisibleSelection || current == null)
            {
                Widgets.Label(rect.ContractedBy(12f), (error ?? "CQF_EntityBook_SelectEntry".Translate().ToString()).Colorize(CQFUIStyle.Muted));
                return;
            }
            int selectionRows = (recipeSelected != null && (current!.Recipe != null || current.Template != null) ? 1 : 0) + (selected != null ? allowGroups ? 2 : 1 : 0)
                + (selected == null && recipeSelected == null && (current!.Recipe != null || current.Template != null) ? 1 : 0);
            Rect inner = rect.ContractedBy(12f);
            Rect viewport = new(inner.x, inner.y, inner.width, Mathf.Max(30f, inner.height - selectionRows * 42f));
            Rect content = new(0f, 0f, viewport.width - 18f, Mathf.Max(viewport.height, detailsHeight));
            Widgets.BeginScrollView(viewport, ref detailScroll, content);
            using CQFUIScope scope = new(content.width);
            float y = 0f;
            Text.Font = GameFont.Medium;
            float titleHeight = Text.CalcHeight(current.Label, content.width);
            Widgets.Label(new Rect(0f, y, content.width, titleHeight), current.Label.Colorize(CQFUIStyle.Accent));
            y += titleHeight + 8f;
            Text.Font = GameFont.Small;
            Widgets.Label(new Rect(0f, y, content.width, 26f), current.Origin.Colorize(CQFUIStyle.Muted));
            y += 32f;
            DrawPreview(new Rect(Mathf.Max(0f, (content.width - 180f) / 2f), y, Mathf.Min(180f, content.width), 168f));
            y += 176f;
            Rect statusRect = new(0f, y, content.width, 30f);
            Widgets.Label(statusRect, Status(current).Colorize(CQFUIStyle.Accent));
            TooltipHandler.TipRegion(statusRect, "CQF_EntityBook_StatusTip".Translate());
            y += 36f;
            DrawMembers(ref y, content.width);
            if (current.Recipe != null)
            {
                Widgets.Label(new Rect(0f, y, content.width, 28f), "CQF_EntityBook_Count".Translate(current.Recipe.count.ToString()));
                y += 34f;
                if (CQFUIStyle.ButtonText(new Rect(0f, y, content.width, 32f), "CQF_EntityBook_EditRecipe".Translate()))
                    Find.WindowStack.Add(new Dialog_CQFEntityRecipe(CQFEntityRecipeStore.Copy(current.Recipe), () => Refresh()));
                y += 40f;
                if (CQFUIStyle.ButtonText(new Rect(0f, y, content.width, 32f), "CQF_EntityBook_SaveRecipe".Translate()))
                {
                    CQFEntityBookCatalog.Collect(current.Recipe);
                    Refresh();
                }
                y += 40f;
                if (CQFUIStyle.ButtonText(new Rect(0f, y, content.width, 32f), "CQF_EntityBook_SaveAsNew".Translate()))
                    Find.WindowStack.Add(new Dialog_CQFEntityRecipe(CQFEntityRecipeStore.CopyAsNew(current.Recipe), () => Refresh()));
                TooltipHandler.TipRegion(new Rect(0f, y, content.width, 32f), "CQF_EntityBook_SaveAsNewTip".Translate());
                y += 40f;
            }
            ComplexPawnDef? template = current.Template ?? (current.Recipe as PawnSpawnData_ComplexPawn)?.pawnDef;
            if (template != null)
            {
                if (current.Recipe == null)
                {
                    if (CQFUIStyle.ButtonText(new Rect(0f, y, content.width, 32f), "CQF_EntityBook_CreateRecipe".Translate()))
                        Find.WindowStack.Add(new Dialog_CQFEntityRecipe(CreateCurrentRecipe(), () => Refresh()));
                    y += 40f;
                }
                if (CQFUIStyle.ButtonText(new Rect(0f, y, content.width, 32f), "CQF_EntityBook_EditTemplate".Translate()))
                    Find.WindowStack.Add(new QuestEditor_PawnDataEditor(template));
                y += 40f;
            }
            if (!previewError.NullOrEmpty())
            {
                float height = Text.CalcHeight(previewError, content.width);
                Widgets.Label(new Rect(0f, y, content.width, height), previewError.Colorize(Color.red));
                y += height + 8f;
            }
            detailsHeight = y + 8f;
            Widgets.EndScrollView();
            DrawSelectionButtons(new Rect(inner.x, viewport.yMax + 8f, inner.width, selectionRows * 42f));
        }

        private void DrawSelectionButtons(Rect rect)
        {
            if (!HasVisibleSelection || current == null) return;
            float y = rect.y;
            if (recipeSelected != null && (current.Recipe != null || current.Template != null))
            {
                useRecipeRect = new Rect(rect.x, y, rect.width, 34f);
                if (CQFUIStyle.ButtonText(useRecipeRect, "CQF_EntityBook_UseRecipe".Translate())) SelectRecipe();
                TooltipHandler.TipRegion(useRecipeRect, "CQF_EntityBook_UseRecipeTip".Translate());
                y += 42f;
            }
            if (selected != null)
            {
                selectEntityRect = new Rect(rect.x, y, rect.width, 34f);
                if (CQFUIStyle.ButtonText(selectEntityRect, "CQF_EntityBook_SelectEntity".Translate(), active: CanSelectReference(false, out _))) SelectReference(false);
                if (!CanSelectReference(false, out string? entityReason) && entityReason != null) TooltipHandler.TipRegion(selectEntityRect, entityReason);
                y += 42f;
                if (allowGroups)
                {
                    selectGroupRect = new Rect(rect.x, y, rect.width, 34f);
                    if (CQFUIStyle.ButtonText(selectGroupRect, "CQF_EntityBook_SelectGroup".Translate(), active: CanSelectReference(true, out _))) SelectReference(true);
                    if (!CanSelectReference(true, out string? groupReason) && groupReason != null) TooltipHandler.TipRegion(selectGroupRect, groupReason);
                }
            }
            if (selected == null && recipeSelected == null && (current!.Recipe != null || current.Template != null))
            {
                placeOnMapRect = new Rect(rect.x, y, rect.width, 34f);
                if (CQFUIStyle.ButtonText(placeOnMapRect, "CQF_EntityBook_PlaceOnMap".Translate(), active: Find.CurrentMap != null && DebugSettings.godMode)) BeginMapPlacement();
                TooltipHandler.TipRegion(placeOnMapRect, "CQF_EntityBook_PlaceOnMapTip".Translate());
            }
        }

        private void DrawMembers(ref float y, float width)
        {
            if (current == null) return;
            IReadOnlyList<CQFEntityRecord> records = Records(current);
            bool multiple = records.Count > 1 || MayHaveMultiple(current.Recipe);
            if (selected != null && multiple)
            {
                Rect label = new(0f, y, Mathf.Min(120f, width * 0.5f), 30f);
                Widgets.Label(label, "CQF_EntityBook_MemberNumber".Translate());
                Rect field = new(label.xMax + 6f, y, Mathf.Max(40f, width - label.width - 6f), 30f);
                int previous = memberNumber;
                Widgets.TextFieldNumeric(field, ref memberNumber, ref memberBuffer, 1, int.MaxValue);
                if (previous != memberNumber) memberIndex = memberNumber - 1;
                TooltipHandler.TipRegion(field, "CQF_EntityBook_MemberNumberTip".Translate());
                y += 38f;
                if (records.Count == 0 && !memberIndex.HasValue)
                {
                    if (CQFUIStyle.ButtonText(new Rect(0f, y, width, 30f), "CQF_EntityBook_ChooseMember".Translate(memberNumber))) memberIndex = memberNumber - 1;
                    y += 38f;
                }
            }
            foreach (CQFEntityRecord record in records)
            {
                string name = record.Pawn?.LabelShortCap ?? "CQF_EntityBook_MissingPawn".Translate().ToString();
                string text = "CQF_EntityBook_RecordMember".Translate(record.MemberIndex + 1, name);
                text += " · " + RecordStatus(record);
                Rect row = new(0f, y, Mathf.Max(40f, width - 66f), 32f);
                if (memberIndex == record.MemberIndex) CQFUIStyle.DrawHighlightSelected(row);
                if (CQFUIStyle.ButtonText(row, text, false, active: CanUseRecord(record)))
                {
                    memberIndex = record.MemberIndex;
                    memberNumber = record.MemberIndex + 1;
                    memberBuffer = memberNumber.ToString();
                }
                TooltipHandler.TipRegion(row, text + "\n" + "CQF_EntityBook_MemberStatusTip".Translate());
                if (record.Pawn?.Spawned == true && CQFUIStyle.ButtonText(new Rect(width - 60f, y, 60f, 32f), "CQF_EntityBook_Locate".Translate()))
                {
                    CameraJumper.TryJumpAndSelect(record.Pawn);
                    Close();
                }
                y += 38f;
            }
        }

        private void DrawPreview(Rect rect)
        {
            if (current == null) return;
            Pawn? actual = Records(current).FirstOrDefault(record => CanUseRecord(record) && (!memberIndex.HasValue || record.MemberIndex == memberIndex))?.Pawn;
            Pawn? pawn = actual;
            ComplexPawnDef? template = current.Template ?? (current.Recipe as PawnSpawnData_ComplexPawn)?.pawnDef;
            PawnKindDef? kind = template?.KindDef ?? current.Kind;
            if (pawn == null && Current.Game != null && kind != null)
            {
                if (previewSource != current.SourceId)
                {
                    ReleasePreview();
                    previewSource = current.SourceId;
                    try
                    {
                        previewPawn = template != null ? template.CreatePreviewPawn() : PawnGenerator.GeneratePawn(new PawnGenerationRequest(kind, forceGenerateNewPawn: true, canGeneratePawnRelations: false));
                    }
                    catch (Exception exception)
                    {
                        previewError = "CQF_EntityBook_PreviewFailed".Translate(exception.Message);
                        CQFEntityBookCatalog.ReportError(exception);
                    }
                }
                pawn = previewPawn;
            }
            if (pawn != null) GUI.DrawTexture(rect, PortraitsCache.Get(pawn, new Vector2(rect.width, rect.height), Rot4.South, default, 1.1f), ScaleMode.ScaleToFit);
            else if (current.Kind?.race != null) Widgets.DefIcon(rect.ContractedBy(24f), current.Kind.race);
            TooltipHandler.TipRegion(rect, actual != null ? "CQF_EntityBook_PreviewActual".Translate() : "CQF_EntityBook_PreviewTemplate".Translate());
        }

        private void ReleasePreview()
        {
            Pawn? owned = previewPawn;
            previewPawn = null;
            previewSource = null;
            previewError = null;
            if (owned == null || owned.Spawned || owned.Discarded) return;
            try
            {
                if (Current.Game?.GetComponent<GameComponent_EntityBook>()?.Records.Any(record => ReferenceEquals(record.Pawn, owned)) == true)
                    throw new InvalidOperationException("CQF_EntityBook_InvalidReference".Translate());
                RimWorld.Planet.WorldPawns worldPawns = Find.WorldPawns ?? throw new InvalidOperationException("CQF_EntityBook_Unavailable".Translate());
                if (worldPawns.Contains(owned)) worldPawns.RemoveAndDiscardPawnViaGC(owned);
                else worldPawns.PassToWorld(owned, RimWorld.Planet.PawnDiscardDecideMode.Discard);
            }
            catch (Exception exception)
            {
                CQFEntityBookCatalog.ReportError(exception);
            }
        }

        private void SelectRecipe()
        {
            if (!HasVisibleSelection || current == null || recipeSelected == null) return;
            try
            {
                PawnSpawnData recipe = CreateCurrentRecipe();
                if (current!.Recipe == null)
                {
                    Find.WindowStack.Add(new Dialog_CQFEntityRecipe(recipe, accepted: value =>
                    {
                        recipeSelected(value);
                        Close();
                    }, acceptLabelKey: "CQF_EntityBook_UseRecipe"));
                    return;
                }
                recipeSelected(recipe);
                Close();
            }
            catch (Exception exception) { CQFEntityBookCatalog.ReportError(exception); }
        }

        private void SelectReference(bool group)
        {
            if (!HasVisibleSelection || current == null || selected == null) return;
            if (!CanSelectReference(group, out string? reason))
            {
                if (reason != null) Messages.Message(reason, MessageTypeDefOf.RejectInput, false);
                return;
            }
            try
            {
                selected(CQFEntityReference.Encode(current!.SourceId, group ? null : memberIndex, group));
                Close();
            }
            catch (Exception exception) { CQFEntityBookCatalog.ReportError(exception); }
        }

        private bool MatchesFilters(CQFEntityBookEntry entry) => (filter == 0 || filter == 1 && entry.Template == null || filter == 2 && entry.Template != null)
            && (search.NullOrEmpty() || entry.Label.IndexOf(search, StringComparison.CurrentCultureIgnoreCase) >= 0 || entry.Origin.IndexOf(search, StringComparison.CurrentCultureIgnoreCase) >= 0 || entry.SourceId.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0);

        private void SelectEntry(CQFEntityBookEntry? entry)
        {
            ReleasePreview();
            current = entry;
            memberIndex = null;
            memberNumber = 1;
            memberBuffer = "1";
            detailScroll = Vector2.zero;
            detailsHeight = 0f;
        }

        private PawnSpawnData CreateCurrentRecipe()
        {
            if (!HasVisibleSelection || current == null) throw new InvalidOperationException("CQF_EntityBook_SelectEntry".Translate());
            PawnSpawnData recipe = current!.Recipe != null ? CQFEntityRecipeStore.Copy(current.Recipe)
                : current.Template != null ? new PawnSpawnData_ComplexPawn { pawnDef = current.Template, dataName = current.Template.defName }
                : throw new InvalidOperationException("CQF_EntityBook_InvalidRecipe".Translate());
            _ = recipe.EntityBookSourceId;
            return recipe;
        }

        private void BeginMapPlacement()
        {
            if (!HasVisibleSelection || Find.CurrentMap == null || !DebugSettings.godMode) return;
            try
            {
                Find.WindowStack.Add(new Dialog_CQFEntityRecipe(CreateCurrentRecipe(), accepted: recipe =>
                {
                    CQFEntityMapPlacement.Begin(recipe, quest);
                    Close();
                }, acceptLabelKey: "CQF_EntityBook_PlaceOnMap"));
            }
            catch (Exception exception) { CQFEntityBookCatalog.ReportError(exception); }
        }

        private bool CanSelectReference(bool group, out string? reason)
        {
            reason = null;
            if (!HasVisibleSelection || group && !allowGroups) return false;
            IReadOnlyList<CQFEntityRecord> records = Records(current!);
            if (!group && !memberIndex.HasValue && (records.Count > 1 || MayHaveMultiple(current!.Recipe)))
            {
                reason = "CQF_EntityBook_ChooseMemberFirst".Translate();
                return false;
            }
            CQFEntityRecord? record = !group ? memberIndex.HasValue ? records.FirstOrDefault(value => value.MemberIndex == memberIndex.Value) : records.FirstOrDefault() : null;
            if (record != null && !CanUseRecord(record) || group && records.Count > 0 && records.All(value => !CanUseRecord(value)))
            {
                reason = "CQF_EntityBook_CannotSelectUnavailable".Translate(record != null ? RecordStatus(record) : "CQF_EntityBook_StatusUnavailable".Translate().ToString());
                return false;
            }
            return true;
        }

        private static bool CanUseRecord(CQFEntityRecord record) => record.IsAvailable && record.Pawn?.Discarded == false;

        private static string RecordStatus(CQFEntityRecord record)
        {
            Pawn? pawn = record.Pawn;
            if (pawn == null) return "CQF_EntityBook_StatusMissing".Translate();
            if (pawn.Dead) return "CQF_EntityBook_StatusDead".Translate();
            if (pawn.Destroyed || pawn.Discarded) return "CQF_EntityBook_StatusMissing".Translate();
            string location = !pawn.Spawned ? "CQF_EntityBook_StatusUnspawned".Translate()
                : pawn.Map == Find.CurrentMap ? "CQF_EntityBook_StatusCurrentMap".Translate() : "CQF_EntityBook_StatusOtherMap".Translate();
            return pawn.Downed ? location + " · " + "CQF_EntityBook_StatusDowned".Translate() : location;
        }

        private IReadOnlyList<CQFEntityRecord> Records(CQFEntityBookEntry entry) => Current.Game?.GetComponent<GameComponent_EntityBook>()?.GetRecords(entry.SourceId, quest) ?? Array.Empty<CQFEntityRecord>();

        private static bool MayHaveMultiple(PawnSpawnData? recipe)
        {
            if (recipe == null) return false;
            if (recipe.count.max > 1 || recipe.extraKinds.Count > 0 || recipe is PawnSpawnData_Faction || recipe is PawnSpawnData_Group) return true;
            if (recipe is MainPawnSpawnData main) return MayHaveMultiple(main.spawnData);
            return recipe is PawnSpawnData_Random random && random.datas.Any(MayHaveMultiple);
        }

        private string Status(CQFEntityBookEntry entry) => Status(Records(entry));

        private static string Status(IReadOnlyList<CQFEntityRecord> records)
        {
            if (records.Count == 0) return "CQF_EntityBook_StatusWaiting".Translate();
            int available = records.Count(CanUseRecord);
            if (records.Count == 1) return RecordStatus(records[0]);
            return available == 0 ? "CQF_EntityBook_StatusUnavailable".Translate() : "CQF_EntityBook_StatusMultiple".Translate(available, records.Count);
        }

        private readonly Action<string>? selected;
        private readonly Action<PawnSpawnData>? recipeSelected;
        private readonly bool allowGroups;
        private readonly Quest? quest;
        private IReadOnlyList<CQFEntityBookEntry> entries = Array.Empty<CQFEntityBookEntry>();
        private IReadOnlyList<CQFEntityBookEntry>? filteredSource;
        private List<CQFEntityBookEntry> visibleEntries = new();
        private string? filteredSearch;
        private int filteredKind;
        private CQFEntityBookEntry? current;
        private string search = "";
        private string? error;
        private int filter;
        private Vector2 listScroll;
        private Vector2 detailScroll;
        private float detailsHeight;
        private int? memberIndex;
        private int memberNumber = 1;
        private string memberBuffer = "1";
        private Pawn? previewPawn;
        private string? previewSource;
        private string? previewError;
        private Rect useRecipeRect;
        private Rect selectEntityRect;
        private Rect selectGroupRect;
        private Rect placeOnMapRect;
    }
}
