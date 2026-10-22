using RimWorld;
using System.Collections;
using System.Reflection;
using UnityEngine;
using Verse;

namespace QuestEditor_Library
{
    public sealed class Dialog_CQFEntityRecipe : Window
    {
        public Dialog_CQFEntityRecipe(PawnSpawnData recipe, Action? saved = null, Action<PawnSpawnData>? accepted = null, string? acceptLabelKey = null)
        {
            this.recipe = recipe;
            this.saved = saved;
            this.accepted = accepted;
            this.acceptLabelKey = acceptLabelKey;
            forcePause = true;
            doCloseX = true;
            draggable = true;
            resizeable = true;
            closeOnClickedOutside = false;
        }

        public override Vector2 InitialSize => new(680f, 660f);

        public PawnSpawnData Recipe => recipe;

        public void ChangeRecipeType(Type type)
        {
            if (type == recipe.GetType()) return;
            try
            {
                if (!typeof(PawnSpawnData).IsAssignableFrom(type) || type.IsAbstract)
                    throw new InvalidOperationException("CQF_EntityBook_InvalidRecipe".Translate());
                PawnSpawnData replacement = (PawnSpawnData)Activator.CreateInstance(type)!;
                List<string> removed = new();
                foreach (FieldInfo field in recipe.GetType().GetFields(BindingFlags.Instance | BindingFlags.Public))
                {
                    if (field.IsInitOnly || field.IsLiteral) continue;
                    FieldInfo? target = type.GetField(field.Name, BindingFlags.Instance | BindingFlags.Public);
                    if (target != null && !target.IsInitOnly && target.FieldType.IsAssignableFrom(field.FieldType))
                        target.SetValue(replacement, field.GetValue(recipe));
                    else if (!field.Name.StartsWith("buffer", StringComparison.OrdinalIgnoreCase) && HasValue(field.GetValue(recipe)))
                        removed.Add(FieldLabel(field.Name) + ": " + FieldValue(field.GetValue(recipe)));
                }
                void Apply()
                {
                    recipe = replacement;
                    scroll = Vector2.zero;
                    height = 0f;
                }
                if (removed.Count == 0) Apply();
                else Find.WindowStack.Add(Dialog_MessageBox.CreateConfirmation("CQF_EntityBook_ChangeTypeConfirmation".Translate(recipe.GetType().Name.Translate(), type.Name.Translate(), string.Join("\n", removed)), Apply, destructive: true));
            }
            catch (Exception exception) { CQFEntityBookCatalog.ReportError(exception); }
        }

        public override void WindowUpdate()
        {
            base.WindowUpdate();
            windowRect.width = Mathf.Clamp(windowRect.width, Mathf.Min(420f, UI.screenWidth - 20f), UI.screenWidth - 20f);
            windowRect.height = Mathf.Clamp(windowRect.height, Mathf.Min(300f, UI.screenHeight - 20f), UI.screenHeight - 20f);
        }

        public override void DoWindowContents(Rect inRect)
        {
            using CQFUIScope scope = new(inRect.width, inRect.height);
            Text.Font = GameFont.Medium;
            Widgets.Label(new Rect(0f, 0f, inRect.width - 35f, 32f), "CQF_EntityBook_RecipeTitle".Translate().Colorize(CQFUIStyle.Accent));
            Text.Font = GameFont.Small;
            if (CQFUIStyle.ButtonText(new Rect(0f, 42f, inRect.width, 32f), recipe.GetType().Name.Translate()))
            {
                List<Type> types = new() { typeof(PawnSpawnData) };
                types.AddRange(typeof(PawnSpawnData).AllSubclassesNonAbstract());
                CQFEditorTools.DrawFloatMenu(types, ChangeRecipeType, type => type.Name.Translate());
            }
            Rect viewport = new(0f, 84f, inRect.width, Mathf.Max(40f, inRect.height - 132f));
            Rect content = new(0f, 0f, viewport.width - 20f, Mathf.Max(viewport.height, height));
            Widgets.BeginScrollView(viewport, ref scroll, content);
            float y = 0f;
            using (new CQFUIScope(content.width))
            {
                recipe.DrawName(ref y, 0f, new Rect(0f, y, content.width, 36f));
                y += 6f;
                DrawCommonSettings(ref y, content);
                using (new CQFEditorContext(CQFEditorContext.SourceThing, recipe))
                {
                    recipe.Draw(ref y, content, 0f);
                }
            }
            height = y + 12f;
            Widgets.EndScrollView();
            if (CQFUIStyle.ButtonText(new Rect(0f, inRect.height - 36f, inRect.width, 34f), (acceptLabelKey ?? "CQF_EntityBook_SaveRecipe").Translate()))
            {
                try
                {
                    ValidateRecipe();
                    if (accepted != null) accepted(CQFEntityRecipeStore.Copy(recipe));
                    else
                    {
                        CQFEntityBookCatalog.SaveRecipe(recipe);
                        Messages.Message("CQF_EntityBook_Collected".Translate(CQFEntityBookCatalog.LabelFor(recipe)), MessageTypeDefOf.PositiveEvent, false);
                        saved?.Invoke();
                    }
                    Close();
                }
                catch (Exception exception) { CQFEntityBookCatalog.ReportError(exception); }
            }
        }

        private void DrawCommonSettings(ref float y, Rect content)
        {
            bool custom = recipe is PawnSpawnData_ComplexPawn;
            bool standard = recipe.GetType().GetMethod(nameof(PawnSpawnData.Draw))?.DeclaringType == typeof(PawnSpawnData);
            if (!custom && !standard) return;
            Rect header = new(8f, y, content.width - 16f, 32f);
            Widgets.DrawBoxSolid(header, CQFUIStyle.Header);
            Widgets.Label(header.ContractedBy(8f, 3f), "CQF_EntityBook_CommonSettings".Translate().Colorize(CQFUIStyle.Accent));
            y += 42f;
            if (custom)
            {
                Rect count = PawnSpawnData.DrawField(ref y, 0f, "QE_Count".Translate(), 240f);
                float part = (count.width - 18f) / 2f;
                int min = recipe.count.min;
                int max = recipe.count.max;
                Widgets.TextFieldNumeric(new Rect(count.x, count.y, part, count.height), ref min, ref recipe.buffer, 1, int.MaxValue);
                Widgets.Label(new Rect(count.x + part + 3f, count.y + 4f, 12f, 25f), "~");
                Widgets.TextFieldNumeric(new Rect(count.x + part + 18f, count.y, part, count.height), ref max, ref recipe.bufferMax, 1, int.MaxValue);
                recipe.count = new IntRange(min, max);
                Rect spawnType = PawnSpawnData.DrawField(ref y, 0f, "SpawnType".Translate(string.Empty));
                if (CQFUIStyle.ButtonText(spawnType, recipe.spawnType.ToString().Translate()))
                    CQFEditorTools.DrawFloatMenu(new List<SpawnType> { SpawnType.BuildingDamaged, SpawnType.BuildingTick, SpawnType.MapGeneration, SpawnType.BuildingDestroyed }, value => recipe.spawnType = value, value => value.ToString().Translate());
                if (recipe.spawnType == SpawnType.BuildingTick)
                    Widgets.TextFieldNumeric(PawnSpawnData.DrawField(ref y, 0f, "TimeToSpawn".Translate()), ref recipe.timeToSpawn, ref recipe.buffer_time, 0, int.MaxValue);
            }
            Rect chance = PawnSpawnData.DrawField(ref y, 0f, "GenerationChance".Translate());
            Widgets.TextFieldNumeric(chance, ref recipe.generationChance, ref recipe.buffer_chance, 0f, 1f);
            TooltipHandler.TipRegion(chance, "CQF_EntityBook_GenerationChanceTip".Translate());
            recipe.way ??= new ArrivingWay();
            Rect arriving = PawnSpawnData.DrawField(ref y, 0f, "CQF_EntityBook_ArrivingWay".Translate());
            if (CQFUIStyle.ButtonText(arriving, recipe.way.GetType().Name.Translate()))
            {
                List<Type> types = new() { typeof(ArrivingWay) };
                types.AddRange(typeof(ArrivingWay).AllSubclassesNonAbstract());
                CQFEditorTools.DrawFloatMenu(types, type => recipe.way = (ArrivingWay)Activator.CreateInstance(type)!, type => type.Name.Translate());
            }
            recipe.way.Draw(ref y, content, 0f);
            y += 8f;
        }

        private void ValidateRecipe()
        {
            if (!recipe.CanSaveToMap()) throw new InvalidOperationException("CQF_EntityBook_InvalidRecipe".Translate());
            if (recipe is PawnSpawnData_ComplexPawn || recipe.GetType().GetMethod(nameof(PawnSpawnData.Draw))?.DeclaringType == typeof(PawnSpawnData))
            {
                if (float.IsNaN(recipe.generationChance) || float.IsInfinity(recipe.generationChance) || recipe.generationChance < 0f || recipe.generationChance > 1f)
                    throw new InvalidOperationException("CQF_EntityBook_InvalidGenerationChance".Translate());
                if (recipe is not PawnSpawnData_Faction && (recipe.count.min < 1 || recipe.count.max < recipe.count.min))
                    throw new InvalidOperationException("CQF_EntityBook_InvalidCountRange".Translate());
            }
        }

        private static bool HasValue(object? value)
        {
            if (value == null) return false;
            if (value is string text) return !text.NullOrEmpty();
            if (value is ICollection collection) return collection.Count > 0;
            return !value.GetType().IsValueType || !Equals(value, Activator.CreateInstance(value.GetType()));
        }

        private static string FieldLabel(string name) => name switch
        {
            "pawnDef" => "CQF_EntityBook_FieldTemplate".Translate(),
            "group" => "CQF_EntityBook_FieldGroup".Translate(),
            "datas" => "CQF_EntityBook_FieldCandidates".Translate(),
            "point" => "SpawmPoint".Translate(),
            "kindDef" => "CQF_PawnGroupMaker".Translate(string.Empty),
            "spawnData" => "MainPawnSpawnDataSpawnData".Translate(),
            "allowGetFromQuestDatabase" => "MainPawnAllowGetFromQuestDatabase".Translate(),
            "allowGetFromGlobalDatabase" => "MainPawnAllowGetFromGlobalDatabase".Translate(),
            "questDatabaseKey" => "MainPawnQuestDatabaseKey".Translate(),
            "globalDatabaseKey" => "MainPawnGlobalDatabaseKey".Translate(),
            "regenerateIfDead" => "MainPawnRegenerateIfDead".Translate(),
            "generateConditions" => "MainPawnGenerateConditions".Translate(),
            "regenerateConditions" => "MainPawnRegenerateConditions".Translate(),
            _ => name.CanTranslate() ? name.Translate() : name
        };

        private static string FieldValue(object? value) => value switch
        {
            Def definition => definition.label.NullOrEmpty() ? definition.defName : definition.label,
            PawnSpawnData data => CQFEntityBookCatalog.LabelFor(data),
            ICollection collection => "CQF_EntityBook_ConfiguredItems".Translate(collection.Count),
            bool boolean => (boolean ? "Yes" : "No").Translate(),
            _ => value?.ToString() ?? string.Empty
        };

        private PawnSpawnData recipe;
        private readonly Action? saved;
        private readonly Action<PawnSpawnData>? accepted;
        private readonly string? acceptLabelKey;
        private Vector2 scroll;
        private float height;
    }
}
