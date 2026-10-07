using System.Reflection;
using System.Xml.Linq;
using RimWorld;
using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace QuestEditor_Library
{
    public sealed class CQFAIPawnRuntime
    {
        public CQFAIPawnRuntime(CQFAIModel model, CQFAIRuntimeJournal journal, bool text = true, string command = "") { this.model = model; this.journal = journal; this.text = text; this.command = command; }
        public XElement Help(string group) => group == "pawns" ? new XElement("pawns",
            new XElement("queries", "kinds=pawns (scope=map|all, optional map_id,search), factions (optional search); pawn/pawn_profile/pawn_skills/pawn_health/pawn_traits/pawn_genes/pawn_abilities/pawn_needs/pawn_dialogue (pawn_id); offset/limit supported. Factions returns actual loaded faction IDs, not FactionDef names. Scope all includes current map Pawns, world Pawns, CQF cached NPCs and dialogue-bound Pawns; exact real IDs are required. Profile includes age, gender, appearance and backstories; absent optional trackers return empty lists without creation."),
            new XElement("operation", new XAttribute("kind", "pawn_skill"), "pawn_id, skill=exact SkillDef, level=0..20, passion=None|Minor|Major; omitted passion preserved. Changes base level and resets level XP; undoable."),
            new XElement("operation", new XAttribute("kind", "pawn_spawn"), "map_id,x,z,data_xml=model <value Class='actual PawnSpawnData subtype'> with queried schema, optional quest_id. Executes actual CQF spawning and actions; not undoable."),
            new XElement("operation", new XAttribute("kind", "pawn_apply_entity"), "pawn_id, definition=exact ComplexPawnDef. Applies its available modules to the existing Pawn, not a draft. May replace inventory/health/duty etc.; not fully undoable. Inspect the modules first."),
            new XElement("operation", new XAttribute("kind", "pawn_faction"), "pawn_id, faction_id from pawn query/factions resources. Uses actual loaded Faction.loadID; not fully undoable because it notifies game systems."),
            new XElement("operation", new XAttribute("kind", "pawn_dialogue"), "pawn_id,definition=DialogManagerDef or remove=true. Replaces only the existing Pawn's CQF dialogue binding with undo. Does not start dialogue or apply the manager's forced traits; use entity application explicitly for module side effects."),
            new XElement("more", "CQF actions for health, traits, genes, abilities, inventory, mental state and duties are executable with run_actions; query actual supported types and schema first."))
            : new XElement("duties",
                new XElement("queries", "duty/duty_database require pawn_id. duty_database optional category=strings|ints|floats|bools|targets, default strings. lords requires map_id; lord additionally lord_id; lord_route additionally pawn_id for full paginated route cells. Lists use offset/limit."),
                new XElement("operation", new XAttribute("kind", "duty_assign"), "pawn_id, definition=DutyMapDef, optional quest_id, start=true. Sets actual runtime, applies duty and evaluates transitions/actions; not fully undoable."),
                new XElement("operation", new XAttribute("kind", "duty_node"), "pawn_id,node_id,optional quest_id. Requires existing runtime/node; executes exit/enter actions. Not fully undoable."),
                new XElement("operation", new XAttribute("kind", "duty_remove"), "pawn_id. Removes runtime and signal subscription. Does not remove its Lord or undo already executed actions; not fully undoable."),
                new XElement("operation", new XAttribute("kind", "lord_configure"), "pawn_id,map_id,lord_id,duty=DutyDef, optional route=<cell><x>..</x><z>..</z></cell> list, optional x,z defend cell. Omitted route/defend configuration is preserved; remove_route/remove_defend explicitly clear it. Requires an existing LordJob_Custom owning that Pawn; applies native duties, supports undo."),
                new XElement("more", "duty_value in database help edits per-Pawn private variables/targets without evaluating transitions."));
        public XElement Read(XElement request)
        {
            CQFAIRuntimeRequest.Fields(request, "kind", "scope", "map_id", "pawn_id", "lord_id", "search", "category", "offset", "limit");
            string kind = CQFAIRuntimeRequest.Text(request, "kind");
            if (kind == "factions")
            {
                string search = CQFAIRuntimeRequest.Text(request, "search");
                if (search.Length > 100) throw new InvalidDataException("CQF_AI_InvalidTool: faction search");
                if (Find.FactionManager == null) throw new InvalidDataException("CQF_AI_MissingResource: faction manager");
                return CQFAIRuntimeRequest.Page("factions", Find.FactionManager.AllFactions.Where(faction => (faction.loadID + " " + faction.def.defName + " " + faction.Name).IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0)
                    .Select(faction => new XElement("faction", new XAttribute("id", faction.loadID), new XAttribute("def", faction.def.defName), new XAttribute("name", faction.Name ?? ""), new XAttribute("player", faction.IsPlayer), new XAttribute("defeated", faction.defeated))), request);
            }
            if (kind == "pawns")
            {
                string search = CQFAIRuntimeRequest.Text(request, "search");
                if (search.Length > 100) throw new InvalidDataException("CQF_AI_InvalidTool: search");
                string scope = CQFAIRuntimeRequest.Text(request, "scope", "map");
                IEnumerable<Pawn> candidates = scope == "map" ? CQFAIRuntimeRequest.Map(request).mapPawns.AllPawnsSpawned : scope == "all" ? CQFAIRuntimeRequest.Pawns() : throw new InvalidDataException("CQF_AI_InvalidTool: Pawn scope");
                if (scope == "all" && request.Element("map_id") != null) throw new InvalidDataException("CQF_AI_InvalidTool: map_id is only valid for map scope");
                return CQFAIRuntimeRequest.Page("pawns", candidates
                    .Where(pawn => (pawn.ThingID + " " + pawn.Name + " " + pawn.kindDef?.defName).IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0).Select(Summary), request);
            }
            if (kind == "lords") return CQFAIRuntimeRequest.Page("lords", CQFAIRuntimeRequest.Map(request).lordManager.lords.Select(LordSummary), request);
            if (kind == "lord")
            {
                Lord lord = Lord(request);
                return new XElement("lord", LordSummary(lord), CQFAIRuntimeRequest.Page("pawns", lord.ownedPawns.Select(pawn => new XElement("pawn", new XAttribute("id", pawn.ThingID),
                    new XAttribute("duty", lord.LordJob is LordJob_Custom job && job.pawnDutyDatas.TryGetValue(pawn, out DutyDef duty) ? duty.defName : ""),
                    new XAttribute("routeCount", lord.LordJob is LordJob_Custom custom && custom.pawnRouteDatas.TryGetValue(pawn, out RouteData route) ? route.routue?.Count ?? 0 : 0),
                    lord.LordJob is LordJob_Custom defend && defend.defendDatas.TryGetValue(pawn, out IntVec3 position) ? new XElement("defend", position.ToString()) : null)), request));
            }
            Pawn target = CQFAIRuntimeRequest.Pawn(request);
            if (kind == "lord_route")
            {
                Lord lord = Lord(request);
                if (lord.LordJob is not LordJob_Custom job || !lord.ownedPawns.Contains(target)) throw new InvalidDataException("CQF_AI_InvalidValue: Custom Lord membership required");
                return CQFAIRuntimeRequest.Page("route", job.pawnRouteDatas.TryGetValue(target, out RouteData route) ? (route.routue ?? new List<IntVec3>()).Select(cell => new XElement("cell", cell.ToString())) : Array.Empty<XElement>(), request);
            }
            if (kind == "pawn") return Summary(target);
            if (kind == "pawn_profile") return new XElement("profile", Summary(target), new XAttribute("gender", target.gender), new XAttribute("ageYears", target.ageTracker?.AgeBiologicalYearsFloat ?? 0),
                new XAttribute("bodyType", target.story?.bodyType?.defName ?? ""), new XAttribute("headType", target.story?.headType?.defName ?? ""), new XAttribute("hair", target.story?.hairDef?.defName ?? ""),
                new XAttribute("childhood", target.story?.Childhood?.defName ?? ""), new XAttribute("adulthood", target.story?.Adulthood?.defName ?? ""));
            if (kind == "pawn_skills") return CQFAIRuntimeRequest.Page("skills", (target.skills?.skills ?? new List<SkillRecord>()).Select(Skill), request);
            if (kind == "pawn_health") return CQFAIRuntimeRequest.Page("hediffs", (target.health?.hediffSet?.hediffs ?? new List<Hediff>()).Select(hediff => new XElement("hediff", new XAttribute("def", hediff.def.defName),
                new XAttribute("id", hediff.loadID), new XAttribute("severity", hediff.Severity), new XAttribute("part", hediff.Part?.def?.defName ?? ""))), request);
            if (kind == "pawn_traits") return CQFAIRuntimeRequest.Page("traits", (target.story?.traits?.allTraits ?? new List<Trait>()).Select(trait => new XElement("trait", new XAttribute("def", trait.def.defName), new XAttribute("degree", trait.Degree))), request);
            if (kind == "pawn_genes") return CQFAIRuntimeRequest.Page("genes", (target.genes?.Endogenes ?? new List<Gene>()).Select(gene => GeneState(gene, false)).Concat((target.genes?.Xenogenes ?? new List<Gene>()).Select(gene => GeneState(gene, true))), request);
            if (kind == "pawn_abilities") return CQFAIRuntimeRequest.Page("abilities", (target.abilities?.AllAbilitiesForReading ?? new List<Ability>()).Select(ability => new XElement("ability", new XAttribute("def", ability.def.defName))), request);
            if (kind == "pawn_needs") return CQFAIRuntimeRequest.Page("needs", (target.needs?.AllNeeds ?? new List<Need>()).Select(need => new XElement("need", new XAttribute("def", need.def.defName), new XAttribute("level", need.CurLevel), new XAttribute("max", need.MaxLevel))), request);
            if (kind == "pawn_dialogue") return Dialogue(target);
            CustomDutyMap? runtime = Runtime(target);
            if (kind == "duty") return Duty(target, runtime);
            if (kind != "duty_database") throw new InvalidDataException("CQF_AI_InvalidTool: pawn query");
            string category = CQFAIRuntimeRequest.Text(request, "category", "strings");
            if (category is not ("strings" or "ints" or "floats" or "bools" or "targets")) throw new InvalidDataException("CQF_AI_InvalidTool: duty database category");
            XElement snapshot = CQFAIDatabaseRuntime.Snapshot(runtime == null ? null : typeof(CustomDutyMap).GetField(category, BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(runtime), category);
            return new XElement("dutyDatabase", new XAttribute("initialized", runtime != null), CQFAIRuntimeRequest.Page(category, snapshot.Elements(), request));
        }
        public void AddInstruction(string text) { command += "\n" + text; }
        public XElement Operate(XElement request)
        {
            string kind = CQFAIRuntimeRequest.Text(request, "kind");
            if (kind == "pawn_spawn")
            {
                CQFAIRuntimeRequest.Fields(request, "kind", "map_id", "x", "z", "data_xml", "quest_id");
                Map map = CQFAIRuntimeRequest.Map(request); IntVec3 cell = Cell(request, map);
                PawnSpawnData data = (PawnSpawnData)(model.Read(CQFAIChanges.Parse(CQFAIRuntimeRequest.Text(request, "data_xml")), typeof(PawnSpawnData)) ?? throw new InvalidDataException("CQF_AI_InvalidValue: pawn data"));
                CQFAIChanges.Validate(data);
                new CQFAIChanges(model).Build(Activator.CreateInstance(data.GetType())!, new XElement("changes", model.Write(data, root: true).Elements().Select(field =>
                    new XElement("set", new XAttribute("path", "/" + field.Name.LocalName), new XElement("value", field.Attributes(), field.Nodes())))), command, text);
                CQFAILiveFeatureValidation.Validate(new CQFAILiveThingConfiguration { spawner = new CQFAISpawnerConfiguration { pawns = new List<PawnSpawnData> { data } } });
                if (!data.CanSaveToMap() || data.count.min < 1 || data.count.max > 40 || data.count.min > data.count.max) throw new InvalidDataException("CQF_AI_InvalidValue: spawn data bounds");
                ValidateSpawnData(data);
                Quest? quest = CQFAIRuntimeRequest.Quest(request);
                journal.MarkIrreversible();
                MainSite? site = map.Parent as MainSite ?? (map.Parent as MapParent_Custom)?.rootSite as MainSite;
                MainSite previousSite = MainMapGenerationContext.CurrentSite; MainMapDef previousDefinition = MainMapGenerationContext.CurrentDef;
                Dictionary<string, TargetInfo> result;
                try { MainMapGenerationContext.CurrentSite = site!; MainMapGenerationContext.CurrentDef = site?.mainMapDef!; result = data.Spawn(cell, map, (quest?.GetUniqueLoadID())!, quest!); }
                finally { MainMapGenerationContext.CurrentSite = previousSite; MainMapGenerationContext.CurrentDef = previousDefinition; }
                return new XElement("spawned", new XAttribute("undoSupported", false), new XAttribute("siteId", site?.ID ?? -1), (result ?? new Dictionary<string, TargetInfo>()).Select(pair => new XElement("target", new XAttribute("key", pair.Key), CQFAIRuntimeCatalog.Target(pair.Value))));
            }
            if (kind == "lord_configure") return ConfigureLord(request);
            if (kind == "pawn_dialogue")
            {
                CQFAIRuntimeRequest.Fields(request, "kind", "pawn_id", "definition", "remove");
                Pawn target = CQFAIRuntimeRequest.Pawn(request);
                GameComponent_Editor editor = Current.Game?.components?.OfType<GameComponent_Editor>().FirstOrDefault() ?? throw new InvalidDataException("CQF_AI_MissingResource: dialogue component");
                DialogManagerDef? definition = CQFAIRuntimeRequest.Bool(request, "remove") ? null : CQFAIRuntimeRequest.Def<DialogManagerDef>(CQFAIRuntimeRequest.Text(request, "definition"));
                var old = editor.dialogsWithTargets;
                var updated = old == null ? new Dictionary<Thing, DialogManagerDef>() : new Dictionary<Thing, DialogManagerDef>(old);
                if (definition == null) updated.Remove(target); else updated[target] = definition;
                return journal.Edit("pawnDialogue:" + target.ThingID, () => editor.dialogsWithTargets = updated, () => editor.dialogsWithTargets = old!, () => Dialogue(target));
            }
            CQFAIRuntimeRequest.Fields(request, kind switch
            {
                "pawn_skill" => new[] { "kind", "pawn_id", "skill", "level", "passion" },
                "pawn_apply_entity" => new[] { "kind", "pawn_id", "definition" },
                "pawn_faction" => new[] { "kind", "pawn_id", "faction_id" },
                "duty_assign" => new[] { "kind", "pawn_id", "definition", "quest_id", "start" },
                "duty_node" => new[] { "kind", "pawn_id", "node_id", "quest_id" },
                "duty_remove" => new[] { "kind", "pawn_id" },
                _ => throw new InvalidDataException("CQF_AI_InvalidTool: Pawn operation")
            });
            Pawn pawn = CQFAIRuntimeRequest.Pawn(request);
            if (kind == "pawn_skill")
            {
                SkillDef def = CQFAIRuntimeRequest.Def<SkillDef>(CQFAIRuntimeRequest.Text(request, "skill"));
                SkillRecord skill = pawn.skills?.skills.FirstOrDefault(value => value.def == def) ?? throw new InvalidDataException("CQF_AI_MissingResource: Pawn skill");
                int level = CQFAIRuntimeRequest.Int(request, "level");
                if (level < 0 || level > 20) throw new InvalidDataException("CQF_AI_InvalidValue: skill level");
                Passion passion = skill.passion;
                if (request.Element("passion") != null && (!Enum.TryParse(CQFAIRuntimeRequest.Text(request, "passion"), out passion) || !Enum.IsDefined(typeof(Passion), passion))) throw new InvalidDataException("CQF_AI_InvalidValue: passion");
                int oldLevel = skill.levelInt; float oldXp = skill.xpSinceLastLevel; Passion oldPassion = skill.passion;
                return journal.Edit("pawn:" + pawn.ThingID + ":skill:" + def.defName, () => { skill.Level = level; skill.xpSinceLastLevel = 0; skill.passion = passion; },
                    () => { skill.Level = oldLevel; skill.xpSinceLastLevel = oldXp; skill.passion = oldPassion; }, () => Skill(skill));
            }
            if (kind == "pawn_apply_entity")
            {
                ComplexPawnDef definition = CQFAIRuntimeRequest.Def<ComplexPawnDef>(CQFAIRuntimeRequest.Text(request, "definition"));
                CQFAIChanges.Validate(definition);
                journal.MarkIrreversible(); definition.ApplyModsToPawn(pawn, false);
                return new XElement("entityApplied", new XAttribute("undoSupported", false), new XAttribute("definition", definition.defName), Summary(pawn));
            }
            if (kind == "pawn_faction")
            {
                Faction faction = Find.FactionManager.AllFactions.FirstOrDefault(value => value.loadID == CQFAIRuntimeRequest.Int(request, "faction_id")) ?? throw new InvalidDataException("CQF_AI_MissingResource: faction");
                journal.MarkIrreversible(); pawn.SetFaction(faction);
                if (pawn.Faction != faction) throw new InvalidDataException("CQF_AI_ApplyMismatch: Pawn faction");
                return new XElement("factionChanged", new XAttribute("undoSupported", false), Summary(pawn));
            }
            GameComponent_ComplexDuty component = Current.Game?.components?.OfType<GameComponent_ComplexDuty>().FirstOrDefault() ?? throw new InvalidDataException("CQF_AI_MissingResource: duty component");
            Quest? bound = CQFAIRuntimeRequest.Quest(request);
            if (kind == "duty_assign")
            {
                DutyMapDef definition = CQFAIRuntimeRequest.Def<DutyMapDef>(CQFAIRuntimeRequest.Text(request, "definition"));
                CQFAIChanges.Validate(definition);
                if (definition.StartNode == null) throw new InvalidDataException("CQF_AI_InvalidValue: duty start node");
                bool start = CQFAIRuntimeRequest.Bool(request, "start", true);
                journal.MarkIrreversible(); component.SetDutyMap(pawn, definition, bound!, start);
                if (Runtime(pawn)?.dutyMap != definition) throw new InvalidDataException("CQF_AI_ApplyMismatch: duty assignment");
            }
            else if (kind == "duty_node")
            {
                string node = CQFAIRuntimeRequest.Text(request, "node_id");
                CustomDutyMap runtime = Runtime(pawn) ?? throw new InvalidDataException("CQF_AI_MissingResource: duty runtime");
                if (runtime.dutyMap?.GetNode(node) == null) throw new InvalidDataException("CQF_AI_MissingResource: duty node");
                journal.MarkIrreversible(); component.SetNode(pawn, node, bound!);
                if (runtime.currentNodeId != node) throw new InvalidDataException("CQF_AI_ApplyMismatch: duty node");
            }
            else if (kind == "duty_remove") { journal.MarkIrreversible(); component.Remove(pawn); }
            else throw new InvalidDataException("CQF_AI_InvalidTool: Pawn operation");
            return new XElement("dutyChanged", new XAttribute("undoSupported", false), Duty(pawn, Runtime(pawn)));
        }
        public static CustomDutyMap? Runtime(Pawn pawn) => Current.Game?.components?.OfType<GameComponent_ComplexDuty>().FirstOrDefault() is GameComponent_ComplexDuty component
            && typeof(GameComponent_ComplexDuty).GetField("runtimes", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(component) is Dictionary<Pawn, CustomDutyMap> values
            && values.TryGetValue(pawn, out CustomDutyMap runtime) ? runtime : null;
        public static Lord Lord(XElement request) => CQFAIRuntimeRequest.Map(request).lordManager?.lords.FirstOrDefault(lord => lord.loadID == CQFAIRuntimeRequest.Int(request, "lord_id"))
            ?? throw new InvalidDataException("CQF_AI_MissingResource: Lord");
        public static void ValidateSpawnData(PawnSpawnData data, int depth = 0)
        {
            if (depth > 32 || data == null || data.count.min < 0 || data.count.max < data.count.min || data.count.max > 40) throw new InvalidDataException("CQF_AI_InvalidValue: nested spawn recipe bounds");
            if (data is PawnSpawnData_Faction faction && (faction.point.min < 0 || faction.point.max < faction.point.min || faction.point.max > 100000)) throw new InvalidDataException("CQF_AI_InvalidValue: faction generation points");
            if (data is PawnSpawnData_Random random)
            {
                if (random.datas == null || random.datas.Count < 1 || random.datas.Count > 64) throw new InvalidDataException("CQF_AI_InvalidValue: random spawn recipes");
                foreach (PawnSpawnData child in random.datas) ValidateSpawnData(child, depth + 1);
            }
            if (data is MainPawnSpawnData main)
            {
                if (main.spawnData == null) throw new InvalidDataException("CQF_AI_InvalidValue: main NPC spawn recipe");
                ValidateSpawnData(main.spawnData, depth + 1);
            }
        }
        public static XElement Summary(Pawn pawn) => new XElement("pawn", new XAttribute("id", pawn.ThingID), new XAttribute("kind", pawn.kindDef?.defName ?? ""),
            new XAttribute("name", pawn.Name?.ToString() ?? ""), new XAttribute("mapId", pawn.Map?.uniqueID ?? -1), new XAttribute("x", pawn.Position.x), new XAttribute("z", pawn.Position.z),
            new XAttribute("factionId", pawn.Faction?.loadID ?? -1), new XAttribute("faction", pawn.Faction?.def?.defName ?? ""), new XAttribute("dead", pawn.Dead),
            new XAttribute("downed", pawn.Downed), new XAttribute("job", pawn.CurJob?.def?.defName ?? ""), new XAttribute("nativeDuty", pawn.mindState?.duty?.def?.defName ?? ""),
            new XAttribute("dutyMap", Runtime(pawn)?.dutyMap?.defName ?? ""), new XAttribute("node", Runtime(pawn)?.currentNodeId ?? ""));
        private XElement ConfigureLord(XElement request)
        {
            CQFAIRuntimeRequest.Fields(request, "kind", "pawn_id", "map_id", "lord_id", "duty", "route", "x", "z", "remove_route", "remove_defend");
            Pawn pawn = CQFAIRuntimeRequest.Pawn(request); Lord lord = Lord(request);
            if (lord.LordJob is not LordJob_Custom job || !lord.ownedPawns.Contains(pawn) || pawn.mindState == null || pawn.Map != lord.Map) throw new InvalidDataException("CQF_AI_InvalidValue: existing Custom Lord membership on the same map required");
            DutyDef duty = CQFAIRuntimeRequest.Def<DutyDef>(CQFAIRuntimeRequest.Text(request, "duty"));
            RouteData? route = null;
            if (request.Element("route") is XElement routeXml)
            {
                if (routeXml.HasAttributes || routeXml.Elements().Count() < 1 || routeXml.Elements().Count() > 256 || routeXml.Elements().Any(element => element.Name != "cell")) throw new InvalidDataException("CQF_AI_InvalidValue: patrol route");
                List<IntVec3> cells = routeXml.Elements().Select(element => { CQFAIRuntimeRequest.Fields(element, "x", "z"); return Cell(element, pawn.Map); }).ToList();
                route = new RouteData(cells);
            }
            IntVec3? defend = request.Element("x") != null || request.Element("z") != null ? Cell(request, pawn.Map) : null;
            bool removeRoute = CQFAIRuntimeRequest.Bool(request, "remove_route"), removeDefend = CQFAIRuntimeRequest.Bool(request, "remove_defend");
            if (removeRoute && route != null || removeDefend && defend.HasValue) throw new InvalidDataException("CQF_AI_InvalidValue: contradictory Lord configuration");
            bool changeRoute = request.Element("route") != null || removeRoute, changeDefend = defend.HasValue || removeDefend;
            bool hadDuty = job.pawnDutyDatas.TryGetValue(pawn, out DutyDef oldDuty), hadRoute = job.pawnRouteDatas.TryGetValue(pawn, out RouteData oldRoute), hadDefend = job.defendDatas.TryGetValue(pawn, out IntVec3 oldDefend);
            PawnDuty? oldApplied = pawn.mindState.duty;
            return journal.Edit("lord:" + lord.loadID + ":pawn:" + pawn.ThingID,
                () => { job.pawnDutyDatas[pawn] = duty; if (changeRoute) { if (route == null) job.pawnRouteDatas.Remove(pawn); else job.pawnRouteDatas[pawn] = route; } if (changeDefend) { if (defend.HasValue) job.defendDatas[pawn] = defend.Value; else job.defendDatas.Remove(pawn); } lord.CurLordToil.UpdateAllDuties(); },
                () => { if (hadDuty) job.pawnDutyDatas[pawn] = oldDuty; else job.pawnDutyDatas.Remove(pawn); if (hadRoute) job.pawnRouteDatas[pawn] = oldRoute; else job.pawnRouteDatas.Remove(pawn); if (hadDefend) job.defendDatas[pawn] = oldDefend; else job.defendDatas.Remove(pawn); lord.CurLordToil.UpdateAllDuties(); pawn.mindState.duty = oldApplied; },
                () => new XElement("lordPawn", new XAttribute("id", pawn.ThingID), new XAttribute("duty", job.pawnDutyDatas.TryGetValue(pawn, out DutyDef currentDuty) ? currentDuty.defName : ""),
                    new XAttribute("appliedDuty", pawn.mindState.duty?.def?.defName ?? ""), new XAttribute("defend", job.defendDatas.TryGetValue(pawn, out IntVec3 currentDefend) ? currentDefend.ToString() : ""),
                    job.pawnRouteDatas.TryGetValue(pawn, out RouteData currentRoute) ? new XElement("route", currentRoute.routue.Select(cell => new XElement("cell", cell.ToString()))) : null));
        }
        private static IntVec3 Cell(XElement request, Map map)
        { IntVec3 cell = new IntVec3(CQFAIRuntimeRequest.Int(request, "x"), 0, CQFAIRuntimeRequest.Int(request, "z")); return cell.InBounds(map) ? cell : throw new InvalidDataException("CQF_AI_MapBounds"); }
        private static XElement Skill(SkillRecord skill) => new XElement("skill", new XAttribute("def", skill.def.defName), new XAttribute("level", skill.levelInt), new XAttribute("passion", skill.passion), new XAttribute("xp", skill.xpSinceLastLevel));
        private static XElement GeneState(Gene gene, bool xenogene) => new XElement("gene", new XAttribute("def", gene.def.defName), new XAttribute("xenogene", xenogene), new XAttribute("active", gene.Active));
        private static XElement Dialogue(Pawn pawn)
        {
            GameComponent_Editor? editor = Current.Game?.components?.OfType<GameComponent_Editor>().FirstOrDefault();
            DialogManagerDef? definition = editor?.dialogsWithTargets != null && editor.dialogsWithTargets.TryGetValue(pawn, out DialogManagerDef value) ? value : null;
            return new XElement("dialogue", new XAttribute("pawnId", pawn.ThingID), new XAttribute("bound", definition != null), new XAttribute("definition", definition?.defName ?? ""));
        }
        private static XElement Duty(Pawn pawn, CustomDutyMap? runtime) => new XElement("duty", new XAttribute("pawnId", pawn.ThingID), new XAttribute("initialized", runtime != null),
            new XAttribute("definition", runtime?.dutyMap?.defName ?? ""), new XAttribute("node", runtime?.currentNodeId ?? ""), new XAttribute("lastTransitionTick", runtime?.lastTransitionTick ?? -1),
            new XAttribute("nextTransitionTick", runtime?.nextTickTransitionTick ?? -1), new XAttribute("lastDamageTick", runtime?.lastDamageTick ?? -1), new XAttribute("lastSignal", runtime?.lastSignal ?? ""), new XAttribute("lastSignalTick", runtime?.lastSignalTick ?? -1));
        private static XElement LordSummary(Lord lord) => new XElement("lord", new XAttribute("id", lord.loadID), new XAttribute("jobType", lord.LordJob?.GetType().FullName ?? ""), new XAttribute("pawnCount", lord.ownedPawns?.Count ?? 0), new XAttribute("factionId", lord.faction?.loadID ?? -1));
        private readonly CQFAIModel model;
        private readonly CQFAIRuntimeJournal journal;
        private readonly bool text;
        private string command;
    }
}
