using RimWorld;
using Verse;

namespace QuestEditor_Library
{
    public static class CQFEntityBookCatalog
    {
        public static CQFEntityRecipeStore Store => store ??= new CQFEntityRecipeStore(Path.Combine(CQFContentPaths.Quests, "Entities"));

        public static void Register()
        {
            CQFEntityBookBridge.Register(OpenReferences, OpenRecipes, Collect, DisplayReference);
        }

        public static void OpenReferences(Action<string>? selected = null, bool allowGroups = false, Quest? quest = null)
        {
            Find.WindowStack.Add(new Window_CQFEntityBook(selected, null, allowGroups, quest));
        }

        public static void OpenRecipes(Action<PawnSpawnData> selected)
        {
            Find.WindowStack.Add(new Window_CQFEntityBook(null, selected, quest: GameTools.GetQuestFromMap(Find.CurrentMap)));
        }

        public static void Collect(PawnSpawnData recipe)
        {
            try
            {
                SaveRecipe(recipe);
                Messages.Message("CQF_EntityBook_Collected".Translate(LabelFor(recipe)), MessageTypeDefOf.PositiveEvent, false);
            }
            catch (Exception exception)
            {
                ReportError(exception);
            }
        }

        public static void SaveRecipe(PawnSpawnData recipe)
        {
            PawnSpawnData saved = Store.Save(recipe);
            recipes[saved.EntityBookSourceId] = saved;
            Track(recipe);
        }

        public static void Track(PawnSpawnData recipe)
        {
            EnsureGame();
            observed[recipe.EntityBookSourceId] = recipe;
        }

        public static IReadOnlyList<CQFEntityBookEntry> GetEntries(Quest? quest = null)
        {
            EnsureGame();
            EnsureLoaded();
            Dictionary<string, CQFEntityBookEntry> entries = new(StringComparer.Ordinal);
            foreach (PawnSpawnData recipe in recipes.Values)
                entries[recipe.EntityBookSourceId] = new CQFEntityBookEntry(recipe.EntityBookSourceId, LabelFor(recipe), "CQF_EntityBook_SourceSaved".Translate(), recipe, saved: true);
            foreach (PawnSpawnData recipe in EnumerateCurrentRecipes())
                foreach (PawnSpawnData nested in EnumerateRecipes(recipe))
                {
                    string source = nested.EntityBookSourceId;
                    if (!entries.ContainsKey(source)) entries[source] = new CQFEntityBookEntry(source, LabelFor(nested), "CQF_EntityBook_SourceCurrent".Translate(), nested);
                    observed[source] = nested;
                }
            foreach (PawnSpawnData recipe in observed.Values)
                if (!entries.ContainsKey(recipe.EntityBookSourceId)) entries[recipe.EntityBookSourceId] = new CQFEntityBookEntry(recipe.EntityBookSourceId, LabelFor(recipe), "CQF_EntityBook_SourceCurrent".Translate(), recipe);
            foreach (ComplexPawnDef template in DefDatabase<ComplexPawnDef>.AllDefsListForReading)
                entries["template:" + template.defName] = new CQFEntityBookEntry("template:" + template.defName, template.LabelCap, "CQF_EntityBook_SourceTemplate".Translate(), template: template);
            GameComponent_EntityBook? runtime = Current.Game?.GetComponent<GameComponent_EntityBook>();
            if (runtime != null)
                foreach (IGrouping<string, CQFEntityRecord> source in runtime.Records.Where(record => record.QuestId == (quest?.id ?? -1)).GroupBy(record => record.SourceId, StringComparer.Ordinal))
                    if (!entries.ContainsKey(source.Key))
                    {
                        string label = source.FirstOrDefault(record => !string.IsNullOrWhiteSpace(record.SourceLabel))?.SourceLabel ?? "CQF_EntityBook_Entity".Translate().ToString();
                        entries[source.Key] = new CQFEntityBookEntry(source.Key, label, "CQF_EntityBook_SourceCurrent".Translate());
                    }
            return entries.Values.OrderBy(entry => entry.Label, StringComparer.CurrentCultureIgnoreCase).ThenBy(entry => entry.SourceId, StringComparer.Ordinal).ToList();
        }

        public static void Reload()
        {
            loaded = false;
            EnsureLoaded();
        }

        public static string DisplayReference(string token)
        {
            EnsureGame();
            if (!CQFEntityReference.TryParse(token, out CQFEntityReference? reference) || reference == null) return token;
            string label = "CQF_EntityBook_SourceMissing".Translate();
            if (reference.SourceId.StartsWith("template:", StringComparison.Ordinal))
            {
                ComplexPawnDef? template = DefDatabase<ComplexPawnDef>.GetNamedSilentFail(reference.SourceId.Substring("template:".Length));
                if (template != null) label = template.LabelCap;
            }
            else
            {
                string? runtimeLabel = Current.Game?.GetComponent<GameComponent_EntityBook>()?.Records
                    .FirstOrDefault(record => record.SourceId == reference.SourceId && !string.IsNullOrWhiteSpace(record.SourceLabel))?.SourceLabel;
                if (!string.IsNullOrWhiteSpace(runtimeLabel)) label = runtimeLabel;
                try
                {
                    EnsureLoaded();
                    if (observed.TryGetValue(reference.SourceId, out PawnSpawnData? current)) label = LabelFor(current);
                    else if (recipes.TryGetValue(reference.SourceId, out PawnSpawnData? saved)) label = LabelFor(saved);
                }
                catch (Exception exception)
                {
                    if (!displayLoadErrorReported)
                    {
                        displayLoadErrorReported = true;
                        ReportError(exception);
                    }
                }
            }
            if (reference.Group) return "CQF_EntityBook_GroupDisplay".Translate(label);
            return reference.MemberIndex.HasValue ? "CQF_EntityBook_MemberDisplay".Translate(label, reference.MemberIndex.Value + 1) : label;
        }

        public static string LabelFor(PawnSpawnData recipe)
        {
            if (!recipe.dataName.NullOrEmpty() && recipe.dataName != "undefined") return recipe.dataName;
            if (recipe is PawnSpawnData_ComplexPawn complex && complex.pawnDef != null) return complex.pawnDef.LabelCap;
            return recipe.kind?.LabelCap ?? recipe.GetType().Name.Translate().ToString();
        }

        public static void ReportError(Exception exception)
        {
            Log.Error("[CQF] Entity book: " + exception);
            string detail = exception.Message.CanTranslate() ? exception.Message.Translate().ToString() : exception.Message;
            Messages.Message("CQF_EntityBook_Error".Translate(detail), MessageTypeDefOf.RejectInput, false);
        }

        private static void EnsureLoaded()
        {
            if (loaded) return;
            IReadOnlyList<PawnSpawnData> values = Store.Load();
            recipes.Clear();
            foreach (PawnSpawnData recipe in values) recipes[recipe.EntityBookSourceId] = recipe;
            loaded = true;
            displayLoadErrorReported = false;
        }

        private static void EnsureGame()
        {
            if (ReferenceEquals(observedGame, Current.Game)) return;
            observed.Clear();
            observedGame = Current.Game;
        }

        private static IEnumerable<PawnSpawnData> EnumerateCurrentRecipes()
        {
            if (Find.CurrentMap is Map map)
            {
                MapComponent_CustomMapData? component = map.GetComponent<MapComponent_CustomMapData>();
                if (component != null)
                {
                    foreach (List<PawnSpawnData> list in component.PawnSpawnDatas_Building.Values)
                        foreach (PawnSpawnData recipe in list) yield return recipe;
                    foreach (PawnDataWithPosAndTime entry in component.pawnSpawnDatas_Tick)
                        if (entry.data != null) yield return entry.data;
                }
                foreach (Spawner spawner in map.listerThings.AllThings.OfType<Spawner>())
                    foreach (PawnSpawnData recipe in spawner.pawns) yield return recipe;
                foreach (LootBox box in map.listerThings.AllThings.OfType<LootBox>())
                    foreach (LootData loot in box.loots.Concat(box.lootDef?.loots ?? Enumerable.Empty<LootData>()))
                        foreach (PawnSpawnData recipe in loot.pawnDatas) yield return recipe;
            }
            CustomMapDataDef? editing = CQFEditorBridge.Module?.CurrentEditingMap;
            if (editing != null)
            {
                foreach (List<PawnSpawnData> list in editing.pawns.Values.Concat(editing.specialSpawnPawns.Values))
                    foreach (PawnSpawnData recipe in list) yield return recipe;
            }
            if (CQFEditorContext.FixedPawnHeader is PawnSpawnData current) yield return current;
        }

        private static IEnumerable<PawnSpawnData> EnumerateRecipes(PawnSpawnData recipe)
        {
            if (recipe == null) yield break;
            yield return recipe;
            if (recipe is MainPawnSpawnData main && main.spawnData != null)
                foreach (PawnSpawnData nested in EnumerateRecipes(main.spawnData)) yield return nested;
            if (recipe is PawnSpawnData_Random random)
                foreach (PawnSpawnData child in random.datas)
                    foreach (PawnSpawnData nested in EnumerateRecipes(child)) yield return nested;
        }

        private static CQFEntityRecipeStore? store;
        private static bool loaded;
        private static bool displayLoadErrorReported;
        private static Game? observedGame;
        private static readonly Dictionary<string, PawnSpawnData> recipes = new(StringComparer.Ordinal);
        private static readonly Dictionary<string, PawnSpawnData> observed = new(StringComparer.Ordinal);
    }
}
