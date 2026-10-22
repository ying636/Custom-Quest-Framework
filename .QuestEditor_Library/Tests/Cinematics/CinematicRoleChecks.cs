using System.Reflection;
using System.Runtime.CompilerServices;
using System.Xml;
using QuestEditor_Library;
using RimWorld;
using RimWorld.Planet;
using Verse;

internal static class CinematicRoleChecks
{
    public static int Run()
    {
        int checks = 0;
        Game previousGame = Current.Game;
        LoadedLanguage previousActive = LanguageDatabase.activeLanguage;
        LoadedLanguage previousDefault = LanguageDatabase.defaultLanguage;
        LoadedLanguage language = (LoadedLanguage)RuntimeHelpers.GetUninitializedObject(typeof(LoadedLanguage));
        language.folderName = "English";
        language.info = new LanguageInfo { languageWorkerClass = typeof(LanguageWorker_English) };
        language.keyedReplacements = new();
        typeof(LoadedLanguage).GetField("dataIsLoaded", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(language, true);
        LanguageDatabase.activeLanguage = LanguageDatabase.defaultLanguage = language;
        try
        {
            Game game = NewGame();
            Current.Game = game;
            Map map = NewMap(41);
            Map otherMap = NewMap(42);
            game.Maps.Add(map);
            game.Maps.Add(otherMap);
            Pawn actor = NewPawn(map, 310);
            Pawn other = NewPawn(otherMap, 311);
            GameComponent_Editor editor = (GameComponent_Editor)RuntimeHelpers.GetUninitializedObject(typeof(GameComponent_Editor));
            game.components.Add(editor);
            Quest quest = (Quest)RuntimeHelpers.GetUninitializedObject(typeof(Quest));
            quest.id = 901;
            CQFCinematicRole role = new() { key = "CQF_Role_Lead", label = "CQF_RoleLabel", source = CQFCinematicRoleSource.Context, sourceKey = "CQF_EventPawn" };
            CQFCinematicDef definition = Definition(role);
            Dictionary<string, TargetInfo> context = new() { [role.sourceKey] = new TargetInfo(actor) };
            Check(definition.ResolveBindings(map, context)[role.key].Thing == actor, "context roles use the exact event target key");
            definition.bindings.Add(new CQFCinematicBinding(role.key, new TargetInfo(other)) { worldID = -1 });
            Check(definition.ResolveBindings(map, context)[role.key].Thing == actor, "dynamic roles never resolve their obsolete recorded actor binding");
            Throws(() => definition.ResolveBindings(map), "RoleMissing", "missing context cannot fall back to the recorded rehearsal actor");
            Check(definition.ResolveBindings(map, new Dictionary<string, TargetInfo> { [role.key] = new TargetInfo(other) })[role.key].Thing == other,
                "same-slot explicit rehearsal targets override the configured dynamic source");
            Throws(() => definition.ResolveBindings(map, new Dictionary<string, TargetInfo> { [role.key] = new TargetInfo(map.Center, map) }),
                "RoleNotPawn", "explicit role overrides must still resolve to a pawn");
            role.source = CQFCinematicRoleSource.Recorded;
            definition.bindings.Clear();
            definition.bindings.Add(new CQFCinematicBinding(role.key, new TargetInfo(actor)));
            Check(definition.ResolveBindings(map)[role.key].Thing == actor, "recorded roles resolve their own recorded pawn binding");
            definition.bindings.Clear();
            Throws(() => definition.ResolveBindings(map), "RoleMissing", "a missing recorded role binding fails explicitly");
            Check(definition.ResolveBindings(map, new Dictionary<string, TargetInfo> { [role.key] = new TargetInfo(other) })[role.key].Thing == other,
                "explicit role targets can replace a missing recorded actor");
            role.source = CQFCinematicRoleSource.QuestDatabase;
            role.sourceKey = "CQF_DbActor";
            editor.GetQuestData(quest).RecordTarget(role.sourceKey, new TargetInfo(actor));
            editor.TemporaryDatabase.RecordTarget(role.sourceKey, new TargetInfo(other));
            editor.GlobalDatabase.RecordTarget(role.sourceKey, new TargetInfo(other));
            Check(definition.ResolveBindings(map, quest: quest)[role.key].Thing == actor, "quest database roles use only the owning quest database");
            Throws(() => definition.ResolveBindings(map), "RoleNoQuest", "quest database roles require an explicit quest context");
            role.source = CQFCinematicRoleSource.TemporaryDatabase;
            Check(definition.ResolveBindings(map)[role.key].Thing == other, "temporary database roles use the current game's temporary database");
            role.source = CQFCinematicRoleSource.GlobalDatabase;
            Check(definition.ResolveBindings(map)[role.key].Thing == other, "global database roles do not require or borrow a quest database");
            role.sourceKey = "CQF_GlobalOnly";
            editor.GlobalDatabase.RecordTarget(role.sourceKey, new TargetInfo(actor));
            role.source = CQFCinematicRoleSource.TemporaryDatabase;
            Throws(() => definition.ResolveBindings(map, quest: quest), "RoleMissing", "temporary roles cannot fall back to a global key");
            role.source = CQFCinematicRoleSource.QuestDatabase;
            Throws(() => definition.ResolveBindings(map, quest: quest), "RoleMissing", "quest roles cannot fall back to a global key");
            role.source = CQFCinematicRoleSource.Context;
            Throws(() => definition.ResolveBindings(map, quest: quest), "RoleMissing", "context roles cannot fall back to any database");
            role.source = CQFCinematicRoleSource.QuestDatabase;
            QuestData data = editor.GetQuestData(quest);
            role.sourceKey = "CQF_Group";
            data.AddGroup(role.sourceKey, new List<Pawn> { actor, other });
            Throws(() => definition.ResolveBindings(map, quest: quest), "RoleAmbiguous", "a multi-pawn group cannot silently select its first member");
            role.sourceKey = "CQF_SingleGroup";
            data.AddGroup(role.sourceKey, new List<Pawn> { actor });
            Check(definition.ResolveBindings(map, quest: quest)[role.key].Thing == actor, "a single-pawn group is an unambiguous role source");
            role.sourceKey = "CQF_Duplicate";
            data.TargetDatas.Add(new TargetWithKey(role.sourceKey, new TargetInfo(actor)));
            data.TargetDatas.Add(new TargetWithKey(role.sourceKey, new TargetInfo(other)));
            Throws(() => definition.ResolveBindings(map, quest: quest), "RoleAmbiguous", "duplicate exact database keys fail instead of selecting one target");
            role.sourceKey = "CQF_Conflict";
            data.RecordTarget(role.sourceKey, new TargetInfo(actor));
            data.AddGroup(role.sourceKey, new List<Pawn> { actor });
            Throws(() => definition.ResolveBindings(map, quest: quest), "RoleAmbiguous", "a database target and group with the same key are ambiguous");
            role.sourceKey = "CQF_Cell";
            data.RecordTarget(role.sourceKey, new TargetInfo(map.Center, map));
            Throws(() => definition.ResolveBindings(map, quest: quest), "RoleNotPawn", "database cell targets cannot fill a pawn role");
            role.source = CQFCinematicRoleSource.Context;
            role.sourceKey = "CQF_EventPawn";
            typeof(Thing).GetField("mapIndexOrState", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(other, (sbyte)-2);
            Throws(() => definition.ResolveBindings(map, new Dictionary<string, TargetInfo> { [role.sourceKey] = new TargetInfo(other) }),
                "RoleNotPawn", "destroyed pawn references cannot fill a role");
            typeof(Thing).GetField("mapIndexOrState", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(other, (sbyte)1);
            CQFCinematicRole unused = new() { key = "CQF_UnusedRole", source = CQFCinematicRoleSource.QuestDatabase, sourceKey = "CQF_Missing" };
            definition.roles.Add(unused);
            Check(definition.GetReferencedTargetKeys().SetEquals(new[] { role.key }), "role metadata and source keys do not count as step references");
            Check(definition.ResolveBindings(map, context)[role.key].Thing == actor, "unused roles do not require a quest or their missing database target");
            definition.steps.Clear();
            definition.steps.Add(new CQFCinematicStep_Wait());
            Check(definition.ResolveBindings(map).Count == 0, "deleting the last role step prunes all role resolution");
            definition.steps = new() { new CQFCinematicStep_Sequence { steps = new() { new CQFCinematicStep_PawnFace { pawnKey = role.key } } } };
            XmlDocument xml = new();
            xml.LoadXml(definition.SaveToXElement(typeof(CQFCinematicDef).FullName!).ToString());
            CQFCinematicDef loaded = DirectXmlToObject.ObjectFromXml<CQFCinematicDef>(xml.DocumentElement, false);
            Check(loaded.roles.Count == 2 && loaded.roles[0].key == role.key && loaded.roles[0].label == role.label
                && loaded.roles[0].source == role.source && loaded.roles[0].sourceKey == role.sourceKey,
                "native definition XML roundtrip preserves role slots, labels and source configuration");
            Check(loaded.ResolveBindings(map, context)[role.key].Thing == actor, "nested role references resolve after native definition XML roundtrip");
            Dictionary<string, TargetInfo> crossMap = new(context) { ["Position"] = new TargetInfo(otherMap.Center, otherMap) };
            CQFCinematicInstance instance = new(loaded, crossMap, quest);
            Check(instance.Map == map && instance.Quest == quest && instance.Targets[role.key].Thing == actor,
                "the actual referenced role chooses the scene map before an unrelated context Position");
            CQFCinematicRole empty = new() { label = "CQF_RoleLabel" };
            Check(empty.ConfigErrors().Any(error => error.Contains("RoleKeyRequired", StringComparison.Ordinal)), "role configuration requires its internal stable slot key");
            empty.key = "CQF_EmptyRole";
            empty.source = CQFCinematicRoleSource.Context;
            Check(empty.ConfigErrors().Any(error => error.Contains("RoleSourceKeyRequired", StringComparison.Ordinal)), "dynamic role configuration requires a source key");
            empty.source = (CQFCinematicRoleSource)999;
            Check(empty.ConfigErrors().Any(error => error.Contains("RoleSourceInvalid", StringComparison.Ordinal)), "undefined role source values fail configuration validation");
            loaded.roles.Add(new CQFCinematicRole { key = role.key });
            Check(loaded.ConfigErrors().Any(error => error.Contains("RoleDuplicate", StringComparison.Ordinal)), "duplicate internal role slot keys fail definition validation");
            Check(new CQFCinematicRole { key = "CQF_PrivateSlot" }.DisplayName != "CQF_PrivateSlot", "an unnamed role does not expose its internal slot identifier");
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
            if (!passed) throw new InvalidOperationException("Cinematic role check failed: " + name);
            checks++;
        }

        void Throws(Action action, string expected, string name)
        {
            try { action(); }
            catch (InvalidOperationException error)
            {
                if (!error.Message.Contains(expected, StringComparison.Ordinal)) throw new InvalidOperationException(name + ": " + error.Message, error);
                checks++;
                return;
            }
            throw new InvalidOperationException("Cinematic role check failed: " + name);
        }
    }

    private static CQFCinematicDef Definition(CQFCinematicRole role)
        => new() { defName = "CQF_RoleCheck", roles = new() { role }, steps = new() { new CQFCinematicStep_PawnFace { pawnKey = role.key } } };

    private static Game NewGame()
    {
        Game game = (Game)RuntimeHelpers.GetUninitializedObject(typeof(Game));
        World world = (World)RuntimeHelpers.GetUninitializedObject(typeof(World));
        WorldInfo info = (WorldInfo)RuntimeHelpers.GetUninitializedObject(typeof(WorldInfo));
        info.seedString = "CQF_RoleWorld";
        info.persistentRandomValue = 743;
        world.info = info;
        game.components = new();
        typeof(Game).GetField("worldInt", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(game, world);
        typeof(Game).GetField("maps", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(game, new List<Map>());
        return game;
    }

    private static Map NewMap(int id)
    {
        Map map = (Map)RuntimeHelpers.GetUninitializedObject(typeof(Map));
        map.uniqueID = id;
        map.info = new MapInfo { Size = new IntVec3(40, 1, 40) };
        map.spawnedThings = new ThingOwner<Thing>(map);
        return map;
    }

    private static Pawn NewPawn(Map map, int id)
    {
        Pawn pawn = (Pawn)RuntimeHelpers.GetUninitializedObject(typeof(Pawn));
        pawn.def = (ThingDef)RuntimeHelpers.GetUninitializedObject(typeof(ThingDef));
        pawn.def.defName = "CQF_RolePawn";
        pawn.thingIDNumber = id;
        pawn.holdingOwner = map.spawnedThings;
        typeof(Thing).GetField("mapIndexOrState", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(pawn, (sbyte)Current.Game.Maps.IndexOf(map));
        ((ThingOwner<Thing>)map.spawnedThings).InnerListForReading.Add(pawn);
        return pawn;
    }
}
