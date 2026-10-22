using System;
using RimWorld;
using Verse;

namespace QuestEditor_Library
{
    public static class CQFEntityBookBridge
    {
        public static bool IsAvailable => openReferences != null;

        public static void Register(Action<Action<string>?, bool, Quest?> references, Action<Action<PawnSpawnData>> recipes, Action<PawnSpawnData> collect, Func<string, string> display)
        {
            openReferences = references ?? throw new ArgumentNullException(nameof(references));
            openRecipes = recipes ?? throw new ArgumentNullException(nameof(recipes));
            collectRecipe = collect ?? throw new ArgumentNullException(nameof(collect));
            displayReference = display ?? throw new ArgumentNullException(nameof(display));
        }

        public static void OpenReferences(Action<string>? selected = null, bool allowGroups = false, Quest? quest = null)
        {
            if (openReferences == null) throw new InvalidOperationException("CQF_Editor_NotLoaded".Translate());
            openReferences(selected, allowGroups, quest);
        }

        public static void OpenRecipes(Action<PawnSpawnData> selected)
        {
            if (openRecipes == null) throw new InvalidOperationException("CQF_Editor_NotLoaded".Translate());
            openRecipes(selected ?? throw new ArgumentNullException(nameof(selected)));
        }

        public static void Collect(PawnSpawnData recipe)
        {
            if (collectRecipe == null) throw new InvalidOperationException("CQF_Editor_NotLoaded".Translate());
            collectRecipe(recipe ?? throw new ArgumentNullException(nameof(recipe)));
        }

        public static string DisplayReference(string token) => displayReference?.Invoke(token) ?? token;

        private static Action<Action<string>?, bool, Quest?>? openReferences;
        private static Action<Action<PawnSpawnData>>? openRecipes;
        private static Action<PawnSpawnData>? collectRecipe;
        private static Func<string, string>? displayReference;
    }
}
