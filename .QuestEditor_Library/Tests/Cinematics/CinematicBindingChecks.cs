using System.Reflection;
using System.Runtime.CompilerServices;
using System.Xml;
using System.Xml.Linq;
using QuestEditor_Library;
using RimWorld.Planet;
using Verse;

internal static class CinematicBindingChecks
{
    public static int Run()
    {
        int checks = 0;
        Game previousGame = Current.Game;
        LoadedLanguage previousActive = LanguageDatabase.activeLanguage;
        LoadedLanguage previousDefault = LanguageDatabase.defaultLanguage;
        LoadedLanguage language = (LoadedLanguage)RuntimeHelpers.GetUninitializedObject(typeof(LoadedLanguage));
        language.folderName = "English";
        language.keyedReplacements = new();
        typeof(LoadedLanguage).GetField("dataIsLoaded", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(language, true);
        LanguageDatabase.activeLanguage = LanguageDatabase.defaultLanguage = language;
        try
        {
            Game game = NewGame("CQF_BindingWorld", 412);
            Map map = (Map)RuntimeHelpers.GetUninitializedObject(typeof(Map));
            map.uniqueID = 9;
            map.info = new MapInfo { Size = new IntVec3(40, 1, 40) };
            game.Maps.Add(map);
            Current.Game = game;
            TargetInfo original = new(new IntVec3(7, 0, 13), map);
            CQFCinematicBinding binding = new("CQF_Binding", original);
            Check(binding.worldSeed == "CQF_BindingWorld" && binding.worldID == 412 && binding.mapID == 9, "capture persists world and map identities");
            Check(binding.Matches(original), "same recorded cell reuses its binding");
            Check(!binding.Matches(new TargetInfo(new IntVec3(8, 0, 13), map)), "a different cell requires a different binding");
            CQFCinematicDef definition = new() { defName = "CQF_BindingScene", steps = new() { new CQFCinematicStep_Camera { targetKey = binding.key } }, bindings = new() { binding } };
            XmlDocument document = new();
            document.LoadXml(definition.SaveToXElement(typeof(CQFCinematicDef).FullName!).ToString());
            CQFCinematicDef loaded = DirectXmlToObject.ObjectFromXml<CQFCinematicDef>(document.DocumentElement, false);
            Check(loaded.bindings.Count == 1 && loaded.bindings[0].key == binding.key && loaded.bindings[0].cell == original.Cell, "definition XML restores the recorded anchor");
            Check(loaded.ResolveBindings(map)[binding.key] == original, "reloaded definition resolves its recorded cell");
            Map replacement = (Map)RuntimeHelpers.GetUninitializedObject(typeof(Map));
            replacement.uniqueID = 9;
            replacement.info = new MapInfo { Size = new IntVec3(40, 1, 40) };
            Game reloadedGame = NewGame("CQF_BindingWorld", 412);
            reloadedGame.Maps.Add(replacement);
            Current.Game = reloadedGame;
            TargetInfo rebound = loaded.ResolveBindings(replacement)[binding.key];
            Check(ReferenceEquals(rebound.Map, replacement) && !ReferenceEquals(rebound.Map, map), "reload binds the new game map rather than retaining the old object");
            Current.Game = NewGame("CQF_BindingWorld", 413);
            Throws(() => loaded.ResolveBindings(replacement), "world identity rejects an unrelated game with matching map IDs");
            Dictionary<string, TargetInfo> external = new() { [binding.key] = new TargetInfo(new IntVec3(4, 0, 5), replacement) };
            Check(loaded.ResolveBindings(replacement, external)[binding.key] == external[binding.key], "explicit targets override unresolvable recorded defaults");
            Current.Game = reloadedGame;
            reloadedGame.Maps.Clear();
            Throws(() => loaded.ResolveBindings(replacement), "missing recorded maps fail explicitly");
            reloadedGame.Maps.Add(replacement);
            loaded.bindings[0].cell = new IntVec3(99, 0, 99);
            Throws(() => loaded.ResolveBindings(replacement), "out-of-bounds recorded cells fail explicitly");
            loaded.bindings[0].cell = original.Cell;
            loaded.bindings.Add(loaded.bindings[0]);
            Check(loaded.ConfigErrors().Any(error => error.Contains("duplicate recorded binding")), "duplicate saved binding keys are rejected by validation");
            replacement.spawnedThings = new ThingOwner<Thing>(replacement);
            TargetInfo cameraTarget = new(new IntVec3(11, 0, 12), replacement);
            CQFCinematicBinding cameraBinding = new("CQF_ActiveCamera", cameraTarget);
            CQFCinematicBinding retiredBinding = new("CQF_RemovedPawn", new TargetInfo(new IntVec3(10, 0, 10), replacement)) { thingID = "Thing_CQF_MissingPawn" };
            CQFCinematicStep_PawnFace retiredStep = new() { pawnKey = retiredBinding.key, targetKey = cameraBinding.key };
            CQFCinematicDef edited = new()
            {
                defName = "CQF_EditedBindingScene",
                steps = new() { new CQFCinematicStep_Camera { targetKey = cameraBinding.key }, retiredStep },
                bindings = new() { retiredBinding, cameraBinding }
            };
            Check(edited.GetReferencedTargetKeys().SetEquals(new[] { retiredBinding.key, cameraBinding.key }), "camera and pawn steps expose their actual recorded target references");
            ThrowsWithMessage(() => edited.ResolveBindings(replacement), "CQF_Cinematic_BindingThingMissing", "a referenced recorded pawn fails explicitly when its thing no longer exists on its recorded map");
            edited.steps.Remove(retiredStep);
            Check(edited.GetReferencedTargetKeys().SetEquals(new[] { cameraBinding.key }), "deleting the pawn step removes its recorded target reference");
            Dictionary<string, TargetInfo> editedTargets = edited.ResolveBindings(replacement);
            Check(editedTargets.Count == 1 && !editedTargets.ContainsKey(retiredBinding.key) && editedTargets[cameraBinding.key] == cameraTarget, "an unused missing pawn binding no longer blocks the remaining real camera binding");
            CQFCinematicDef skipReferences = new()
            {
                defName = "CQF_SkipBindingScene",
                steps = new() { new CQFCinematicStep_Wait() },
                skipActions = new() { new BindingReferenceProbeAction(cameraBinding.key) },
                bindings = new() { retiredBinding, cameraBinding }
            };
            Check(skipReferences.GetReferencedTargetKeys().SetEquals(new[] { cameraBinding.key }), "skip action saveable XML leaf values expose their recorded target references");
            Dictionary<string, TargetInfo> skipTargets = skipReferences.ResolveBindings(replacement);
            Check(skipTargets.Count == 1 && skipTargets[cameraBinding.key] == cameraTarget, "targets referenced only by skip actions resolve while unused missing bindings are ignored");
            CQFCinematicDef candidates = new()
            {
                defName = "CQF_CandidateBindingScene",
                steps = new() { new CQFCinematicStep_Camera { targetKey = "CQF_ExternalTarget" } },
                bindings = new() { cameraBinding }
            };
            Check(candidates.GetReferencedTargetKeys().Count == 0, "default reference candidates include only recorded binding keys");
            Check(candidates.GetReferencedTargetKeys(new[] { "CQF_ExternalTarget" }).SetEquals(new[] { "CQF_ExternalTarget" }), "explicit reference candidates may identify a target without a recorded binding");
            Check(candidates.GetReferencedTargetKeys(new[] { "cqf_ExternalTarget" }).Count == 0, "explicit reference candidate matching is ordinal and case sensitive");
            CQFCinematicBinding upperCase = new("CQF_CaseTarget", new TargetInfo(new IntVec3(14, 0, 14), replacement));
            CQFCinematicBinding lowerCase = new("CQF_casetarget", new TargetInfo(new IntVec3(15, 0, 15), replacement));
            CQFCinematicDef caseSensitive = new()
            {
                defName = "CQF_CaseBindingScene",
                steps = new() { new CQFCinematicStep_Camera { targetKey = lowerCase.key } },
                bindings = new() { upperCase, lowerCase }
            };
            Dictionary<string, TargetInfo> caseTargets = caseSensitive.ResolveBindings(replacement);
            Check(caseSensitive.GetReferencedTargetKeys().SetEquals(new[] { lowerCase.key }) && caseTargets.Count == 1
                && !caseTargets.ContainsKey(upperCase.key) && caseTargets[lowerCase.key].Cell == lowerCase.cell,
                "recorded reference collection and resolution keep differently cased keys distinct");
            CQFCinematicDef metadata = new()
            {
                defName = retiredBinding.key,
                label = cameraBinding.key,
                completedSignal = retiredBinding.key,
                skippedSignal = cameraBinding.key,
                cancelledSignal = retiredBinding.key,
                failedSignal = cameraBinding.key,
                steps = new() { new CQFCinematicStep_Wait() },
                bindings = new() { retiredBinding, cameraBinding }
            };
            Check(metadata.GetReferencedTargetKeys().Count == 0 && metadata.ResolveBindings(replacement).Count == 0,
                "definition metadata and recorded binding metadata do not count as step or skip action target references");
            return checks;
        }
        finally
        {
            Current.Game = previousGame;
            LanguageDatabase.activeLanguage = previousActive;
            LanguageDatabase.defaultLanguage = previousDefault;
        }

        void Check(bool passed, string name)
        {
            if (!passed) throw new InvalidOperationException("Cinematic binding check failed: " + name);
            checks++;
        }

        void Throws(Action action, string name)
        {
            try { action(); }
            catch (InvalidOperationException) { checks++; return; }
            throw new InvalidOperationException("Cinematic binding check failed: " + name);
        }

        void ThrowsWithMessage(Action action, string expectedMessage, string name)
        {
            try { action(); }
            catch (InvalidOperationException error)
            {
                if (!error.Message.Contains(expectedMessage, StringComparison.Ordinal))
                    throw new InvalidOperationException("Cinematic binding check failed: " + name + "; unexpected failure: " + error.Message, error);
                checks++;
                return;
            }
            throw new InvalidOperationException("Cinematic binding check failed: " + name);
        }
    }

    private static Game NewGame(string seed, int identity)
    {
        Game game = (Game)RuntimeHelpers.GetUninitializedObject(typeof(Game));
        World world = (World)RuntimeHelpers.GetUninitializedObject(typeof(World));
        WorldInfo info = (WorldInfo)RuntimeHelpers.GetUninitializedObject(typeof(WorldInfo));
        info.seedString = seed;
        info.persistentRandomValue = identity;
        world.info = info;
        game.components = new();
        typeof(Game).GetField("worldInt", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(game, world);
        typeof(Game).GetField("maps", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(game, new List<Map>());
        return game;
    }

    private sealed class BindingReferenceProbeAction : CQFAction
    {
        public BindingReferenceProbeAction(string targetKey)
        {
            this.targetKey = targetKey;
        }

        public override XElement SaveToXElement(string nodeName)
        {
            XElement result = base.SaveToXElement(nodeName);
            result.Add(new XElement("probe", new XElement("target", targetKey)));
            return result;
        }

        public override void ExposeData() { }

        public override void Work(Dictionary<string, TargetInfo> targets, RimWorld.Quest quest) { }

        private readonly string targetKey;
    }
}
