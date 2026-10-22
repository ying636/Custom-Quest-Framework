using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using RimWorld;
using Verse;

namespace QuestEditor_Library;

public sealed class CQFCinematicProject
{
    public CQFCinematicProject()
    {
        Definition = new CQFCinematicDef { defName = "CQF_Cinematic_" + Guid.NewGuid().ToString("N"), label = "CQF_Cinematic_New".Translate() };
    }

    public CQFCinematicProject(CQFCinematicDef definition)
    {
        Definition = ReadDefinition(new XElement("Defs", definition.SaveToXElement(typeof(CQFCinematicDef).FullName!)).ToString());
    }

    public CQFCinematicDef Definition { get; }
    public Dictionary<string, TargetInfo> Targets { get; } = new(StringComparer.Ordinal);
    public List<CQFCinematicSubtitleText> Subtitles { get; } = new();
    public List<CQFCinematicBinding> RehearsalBindings { get; } = new();
    public Dictionary<string, string> TargetNames { get; } = new(StringComparer.Ordinal);
    public List<CQFCinematicBinding> Locations { get; } = new();
    public string DisplayName { get => Definition.label ?? Definition.defName; set => Definition.label = value; }
    public string QuestID { get; set; } = "";
    public Quest? Quest
    {
        get
        {
            if (string.IsNullOrEmpty(QuestID)) return null;
            if (Current.Game == null) throw new InvalidOperationException("CQF_Cinematic_MapRequired".Translate());
            return Find.QuestManager.QuestsListForReading.FirstOrDefault(candidate => candidate.GetUniqueLoadID() == QuestID)
                ?? throw new InvalidOperationException("CQF_Cinematic_BindingQuestMissing".Translate());
        }
        set => QuestID = value?.GetUniqueLoadID() ?? "";
    }

    private static string RootDirectory => Path.GetDirectoryName(CQFContentPaths.Quests)!;

    public string BindTarget(TargetInfo target)
    {
        CQFCinematicBinding? existing = Definition.bindings.Concat(RehearsalBindings).Concat(Locations).FirstOrDefault(binding => binding.Matches(target));
        string key = existing?.key ?? "CQF_Target_" + Guid.NewGuid().ToString("N");
        CQFCinematicBinding recorded = new(key, target);
        List<CQFCinematicBinding> destination = Definition.roles.Any(role => role.key == key && role.source != CQFCinematicRoleSource.Recorded)
            ? RehearsalBindings : Definition.bindings;
        int index = destination.FindIndex(binding => binding.key == key);
        if (index < 0) destination.Add(recorded);
        else destination[index] = recorded;
        Targets[key] = target;
        if (!target.HasThing && !TargetNames.ContainsKey(key))
            TargetNames[key] = "CQF_Cinematic_LocationNumber".Translate(TargetNames.Count + 1);
        if (!target.HasThing)
        {
            Locations.RemoveAll(binding => binding.key == key);
            Locations.Add(recorded);
        }
        return key;
    }

    public void EnsureRoles()
    {
        var fields = EnumerateSteps(Definition.steps).SelectMany(step => step.GetType()
            .GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance)
            .Where(field => field.FieldType == typeof(string) && field.Name is "pawnKey" or "interviewerKey" or "intervieweeKey")
            .Select(field => (Step: step, Field: field))).ToList();
        foreach (var entry in fields)
            {
                string key = (string?)entry.Field.GetValue(entry.Step) ?? "";
                if (string.IsNullOrWhiteSpace(key) || Definition.roles.Any(role => role.key == key)) continue;
                bool recorded = Definition.bindings.Any(binding => binding.key == key);
                string sourceKey = key;
                if (!recorded)
                {
                    key = "CQF_Role_" + Guid.NewGuid().ToString("N");
                    foreach (var related in fields.Where(item => (string?)item.Field.GetValue(item.Step) == sourceKey))
                        related.Field.SetValue(related.Step, key);
                }
                Definition.roles.Add(new CQFCinematicRole
                {
                    key = key,
                    label = "CQF_Cinematic_RoleNumber".Translate(Definition.roles.Count + 1),
                    source = recorded ? CQFCinematicRoleSource.Recorded : CQFCinematicRoleSource.Context,
                    sourceKey = recorded ? "" : sourceKey
                });
            }
    }

    public string BindPawnRole(Pawn pawn)
    {
        string key = "CQF_Role_" + Guid.NewGuid().ToString("N");
        TargetInfo target = new(pawn);
        Definition.bindings.Add(new CQFCinematicBinding(key, target));
        Definition.roles.Add(new CQFCinematicRole
        {
            key = key, label = "CQF_Cinematic_RoleNumber".Translate(Definition.roles.Count + 1), source = CQFCinematicRoleSource.Recorded
        });
        Targets[key] = target;
        return key;
    }

    public string BindEntityRole(string token, string? label = null)
    {
        if (!CQFEntityReference.TryParse(token, out CQFEntityReference? reference) || reference == null || reference.Group)
            throw new InvalidOperationException("CQF_EntityBook_CinematicReferenceInvalid".Translate());
        CQFCinematicRole? existing = Definition.roles.FirstOrDefault(role => role.source == CQFCinematicRoleSource.EntityBook && role.sourceKey == token);
        if (existing != null) return existing.key;
        string key = "CQF_Role_" + Guid.NewGuid().ToString("N");
        Definition.roles.Add(new CQFCinematicRole
        {
            key = key, label = label ?? CQFEntityBookBridge.DisplayReference(token), source = CQFCinematicRoleSource.EntityBook, sourceKey = token
        });
        BindEntityRehearsal(key, reference);
        return key;
    }

    public void SetEntityRole(string key, string token, string? label = null)
    {
        if (!CQFEntityReference.TryParse(token, out CQFEntityReference? reference) || reference == null || reference.Group)
            throw new InvalidOperationException("CQF_EntityBook_CinematicReferenceInvalid".Translate());
        SetRoleSource(key, CQFCinematicRoleSource.EntityBook, token);
        CQFCinematicRole role = Definition.roles.Single(role => role.key == key);
        if (label != null) role.label = label;
        if (!Targets.ContainsKey(key)) BindEntityRehearsal(key, reference);
    }

    public void SetRoleSource(string key, CQFCinematicRoleSource source, string sourceKey)
    {
        EnsureRoles();
        CQFCinematicRole role = Definition.roles.Single(role => role.key == key);
        role.source = source;
        role.sourceKey = sourceKey;
        CQFCinematicBinding? binding = Definition.bindings.Concat(RehearsalBindings).FirstOrDefault(binding => binding.key == key);
        Definition.bindings.RemoveAll(binding => binding.key == key);
        RehearsalBindings.RemoveAll(binding => binding.key == key);
        if (binding != null)
            (source == CQFCinematicRoleSource.Recorded ? Definition.bindings : RehearsalBindings).Add(binding);
    }

    public void SetRehearsalPawn(string key, Pawn pawn)
    {
        EnsureRoles();
        CQFCinematicRole role = Definition.roles.Single(role => role.key == key);
        TargetInfo target = new(pawn);
        CQFCinematicBinding binding = new(key, target);
        List<CQFCinematicBinding> destination = role.source == CQFCinematicRoleSource.Recorded ? Definition.bindings : RehearsalBindings;
        destination.RemoveAll(entry => entry.key == key);
        destination.Add(binding);
        Targets[key] = target;
    }

    public string RoleLabel(string key)
    {
        EnsureRoles();
        CQFCinematicRole? role = Definition.roles.FirstOrDefault(role => role.key == key);
        return string.IsNullOrWhiteSpace(role?.label) ? "CQF_Cinematic_UnassignedRole".Translate().ToString() : role.label;
    }

    public string RehearsalPawnLabel(string key)
        => Targets.TryGetValue(key, out TargetInfo target) && target.Thing is Pawn pawn
            ? pawn.LabelShortCap.ToString() : "CQF_Cinematic_UnassignedRole".Translate().ToString();

    public void CaptureBindings()
    {
        EnsureRoles();
        HashSet<string> referenced = Definition.GetReferencedTargetKeys(Targets.Keys.Concat(Definition.bindings.Select(binding => binding.key))
            .Concat(Definition.roles.Select(role => role.key)).Concat(RehearsalBindings.Select(binding => binding.key)));
        foreach (string key in Targets.Keys.Where(key => !referenced.Contains(key)).ToArray()) Targets.Remove(key);
        Definition.roles.RemoveAll(role => !referenced.Contains(role.key));
        Definition.bindings.RemoveAll(binding => !referenced.Contains(binding.key));
        RehearsalBindings.RemoveAll(binding => !referenced.Contains(binding.key));
        foreach (string key in TargetNames.Keys.Where(key => !referenced.Contains(key) && !Locations.Any(location => location.key == key)).ToArray()) TargetNames.Remove(key);
        foreach (KeyValuePair<string, TargetInfo> entry in Targets)
        {
            CQFCinematicBinding recorded = new(entry.Key, entry.Value);
            bool dynamic = Definition.roles.Any(role => role.key == entry.Key && role.source != CQFCinematicRoleSource.Recorded);
            List<CQFCinematicBinding> destination = dynamic ? RehearsalBindings : Definition.bindings;
            List<CQFCinematicBinding> other = dynamic ? Definition.bindings : RehearsalBindings;
            other.RemoveAll(binding => binding.key == entry.Key);
            int index = destination.FindIndex(binding => binding.key == entry.Key);
            if (index < 0) destination.Add(recorded);
            else destination[index] = recorded;
        }
    }

    public Dictionary<string, TargetInfo> ResolveBindings()
    {
        Map? map = Find.CurrentMap;
        EnsureRoles();
        HashSet<string> referenced = Definition.GetReferencedTargetKeys(Definition.bindings.Select(binding => binding.key).Concat(Definition.roles.Select(role => role.key)));
        if (map == null && referenced.Count > 0) throw new InvalidOperationException("CQF_Cinematic_MapRequired".Translate());
        Dictionary<string, TargetInfo> resolved = new(StringComparer.Ordinal);
        foreach (CQFCinematicBinding binding in Definition.bindings.Concat(RehearsalBindings))
        {
            if (!referenced.Contains(binding.key)) continue;
            CQFCinematicRole? role = Definition.roles.FirstOrDefault(role => role.key == binding.key);
            if (role?.source != CQFCinematicRoleSource.Recorded && RehearsalBindings.Contains(binding))
            {
                Game? game = Current.Game;
                if (game == null || game.World.info.seedString != binding.worldSeed || game.World.info.persistentRandomValue != binding.worldID)
                    continue;
                try { resolved[binding.key] = binding.Resolve(map!); }
                catch (Exception error)
                {
                    string message = "CQF_Cinematic_RoleTestPawnMissing".Translate(RoleLabel(binding.key));
                    Log.Warning("[CQF] " + message + "\n" + error);
                    Messages.Message(message, MessageTypeDefOf.CautionInput, false);
                }
                continue;
            }
            resolved[binding.key] = binding.Resolve(map!);
        }
        return resolved;
    }

    public void RebindTargets()
    {
        Targets.Clear();
        Dictionary<string, TargetInfo> resolved = ResolveBindings();
        foreach (KeyValuePair<string, TargetInfo> entry in resolved) Targets.Add(entry.Key, entry.Value);
    }

    public CQFCinematicSubtitleText GetSubtitle(CQFCinematicStep_Subtitle step)
    {
        CQFCinematicSubtitleText? existing = Subtitles.FirstOrDefault(text => text.Key == step.textKey);
        if (existing != null) return existing;
        CQFCinematicSubtitleText result = new(step.textKey, ReadTranslation(step.textKey, ChineseFolder), ReadTranslation(step.textKey, "English"));
        Subtitles.Add(result);
        return result;
    }

    public void SetSubtitle(CQFCinematicStep_Subtitle step, string chinese, string english)
    {
        if (!IsOwnedSubtitleKey(step.textKey) || EnumerateSteps(Definition.steps).OfType<CQFCinematicStep_Subtitle>().Count(candidate => candidate.textKey == step.textKey) > 1)
            step.textKey = SubtitlePrefix + Guid.NewGuid().ToString("N");
        CQFCinematicSubtitleText? text = Subtitles.FirstOrDefault(entry => entry.Key == step.textKey);
        if (text == null)
        {
            text = new CQFCinematicSubtitleText(step.textKey, chinese, english);
            Subtitles.Add(text);
        }
        else
        {
            text.Chinese = chinese;
            text.English = english;
        }
        PublishSubtitles(new[] { text });
    }

    public string Save()
    {
        CaptureBindings();
        Validate(Definition);
        List<CQFCinematicStep_Subtitle> subtitleSteps = EnumerateSteps(Definition.steps).OfType<CQFCinematicStep_Subtitle>().ToList();
        foreach (CQFCinematicStep_Subtitle step in subtitleSteps) GetSubtitle(step);
        HashSet<string> referencedSubtitles = new(subtitleSteps.Select(step => step.textKey), StringComparer.Ordinal);
        List<CQFCinematicSubtitleText> authored = Subtitles.Where(text => IsOwnedSubtitleKey(text.Key) && referencedSubtitles.Contains(text.Key)).ToList();
        if (authored.Select(text => text.Key).Distinct(StringComparer.Ordinal).Count() != authored.Count)
            throw new InvalidDataException("CQF_Cinematic_DuplicateSubtitle".Translate());
        if (authored.Any(text => string.IsNullOrWhiteSpace(text.Chinese) || string.IsNullOrWhiteSpace(text.English)))
            throw new InvalidDataException("CQF_Cinematic_SubtitleBothRequired".Translate());
        string destination = Path.Combine(CQFContentPaths.Quests, "Cinematics", Definition.defName + ".xml");
        Dictionary<string, string> files = new(StringComparer.OrdinalIgnoreCase)
        {
            [destination] = new XDocument(new XElement("Defs", Definition.SaveToXElement(typeof(CQFCinematicDef).FullName!))).ToString(),
            [Path.ChangeExtension(destination, ".cqfscene")] = new XDocument(new XElement("CQFCinematicScene",
                new XElement("definition", Definition.defName), new XElement("displayName", DisplayName), new XElement("questID", QuestID),
                new XElement("subtitles", authored.Select(text => text.SaveToXElement())),
                new XElement("targetNames", TargetNames.Select(entry => new XElement("li", new XAttribute("key", entry.Key), entry.Value))),
                new XElement("locations", Locations.Select(binding => binding.SaveToXElement("li"))),
                new XElement("rehearsalBindings", RehearsalBindings.Select(binding => binding.SaveToXElement("li"))))).ToString()
        };
        foreach (string folder in new[] { "English", ChineseFolder })
        {
            string languagePath = Path.Combine(RootDirectory, "Languages", folder, "Keyed", SubtitleFileName);
            XElement language = File.Exists(languagePath) ? ReadXml(File.ReadAllText(languagePath, Encoding.UTF8)) : new XElement("LanguageData");
            if (language.Name != "LanguageData" || language.Elements().GroupBy(entry => entry.Name).Any(group => group.Count() > 1))
                throw new InvalidDataException("CQF_Cinematic_InvalidSubtitleFile".Translate());
            foreach (CQFCinematicSubtitleText text in authored)
            {
                XElement? entry = language.Element(text.Key);
                string value = folder == "English" ? text.English : text.Chinese;
                if (entry == null) language.Add(new XElement(text.Key, value));
                else entry.Value = value;
            }
            files[languagePath] = new XDocument(language).ToString();
        }
        if (authored.Count > 0)
        {
            string reviewPath = Path.Combine(RootDirectory, ".QuestEditor_Library", "TextReview", "Cinematics.csv");
            files[reviewPath] = CQFCinematicTextReview.Merge(reviewPath, authored, ReadReviewText);
        }
        WriteFiles(files);
        PublishSubtitles(authored);
        CQFQuestDefBootstrap.HotLoadDefinition(Definition);
        return destination;
    }

    public static CQFCinematicProject Load(string path)
    {
        if (!File.Exists(path) || new FileInfo(path).Length > 2097152) throw new InvalidDataException("CQF_Cinematic_InvalidFile".Translate());
        CQFCinematicProject project = FromXml(File.ReadAllText(path, Encoding.UTF8));
        project.ReadScene(Path.ChangeExtension(path, ".cqfscene"));
        if (Current.Game != null) project.RebindTargets();
        PublishSubtitles(project.Subtitles.Where(text => IsOwnedSubtitleKey(text.Key)));
        return project;
    }

    public static CQFCinematicProject Load(CQFCinematicDef definition)
    {
        CQFCinematicProject project = new(definition);
        project.ReadScene(Path.Combine(CQFContentPaths.Quests, "Cinematics", definition.defName + ".cqfscene"));
        if (Current.Game != null) project.RebindTargets();
        PublishSubtitles(project.Subtitles.Where(text => IsOwnedSubtitleKey(text.Key)));
        return project;
    }

    public static CQFCinematicProject FromXml(string xml) => new(ReadDefinition(xml));

    public CQFCinematicProject CloneForTest()
    {
        CQFCinematicProject clone = new(Definition) { QuestID = QuestID };
        clone.Subtitles.AddRange(Subtitles.Select(text => text.Copy()));
        clone.RehearsalBindings.AddRange(RehearsalBindings.Select(binding => new CQFCinematicBinding
        {
            key = binding.key, worldSeed = binding.worldSeed, worldID = binding.worldID,
            mapID = binding.mapID, thingID = binding.thingID, cell = binding.cell
        }));
        foreach (KeyValuePair<string, string> entry in TargetNames) clone.TargetNames.Add(entry.Key, entry.Value);
        clone.Locations.AddRange(Locations.Select(binding => new CQFCinematicBinding
        {
            key = binding.key, worldSeed = binding.worldSeed, worldID = binding.worldID,
            mapID = binding.mapID, thingID = binding.thingID, cell = binding.cell
        }));
        return clone;
    }

    public string CaptureSnapshot()
    {
        return new XElement("CQFCinematicDraft", new XAttribute("version", 1),
            new XElement("definition", Definition.SaveToXElement(typeof(CQFCinematicDef).FullName!)),
            new XElement("questID", QuestID),
            new XElement("subtitles", Subtitles.Select(text => text.SaveToXElement())),
            new XElement("targetNames", TargetNames.Select(entry => new XElement("li", new XAttribute("key", entry.Key), entry.Value))),
            new XElement("locations", Locations.Select(binding => binding.SaveToXElement("li"))),
            new XElement("rehearsalBindings", RehearsalBindings.Select(binding => binding.SaveToXElement("li"))))
            .ToString(SaveOptions.DisableFormatting);
    }

    public void RefreshPreviewTexts()
    {
        PublishSubtitles(Subtitles.Where(text => IsOwnedSubtitleKey(text.Key)));
    }

    public static CQFCinematicProject RestoreSnapshot(string snapshot)
    {
        XElement root = ReadXml(snapshot);
        if (root.Name != "CQFCinematicDraft" || (string?)root.Attribute("version") != "1"
            || root.Element("definition")?.Elements().Count() != 1)
            throw new InvalidDataException("CQF_Cinematic_InvalidScene".Translate());
        CQFCinematicProject restored = FromXml(root.Element("definition")!.Elements().Single().ToString());
        restored.QuestID = root.Element("questID")?.Value ?? "";
        restored.ReadLocations(root.Element("locations"));
        foreach (XElement name in root.Element("targetNames")?.Elements("li") ?? Enumerable.Empty<XElement>())
        {
            string key = (string?)name.Attribute("key") ?? "";
            if (string.IsNullOrEmpty(key) || restored.TargetNames.ContainsKey(key)) throw new InvalidDataException("CQF_Cinematic_InvalidScene".Translate());
            restored.TargetNames.Add(key, name.Value);
        }
        foreach (XElement text in root.Element("subtitles")?.Elements("li") ?? Enumerable.Empty<XElement>())
        {
            string key = text.Element("key")?.Value ?? "";
            if (restored.Subtitles.Any(value => value.Key == key)) throw new InvalidDataException("CQF_Cinematic_DuplicateSubtitle".Translate());
            restored.Subtitles.Add(new CQFCinematicSubtitleText(key, text.Element("chinese")?.Value ?? "", text.Element("english")?.Value ?? ""));
        }
        if (root.Element("rehearsalBindings") is XElement bindings)
        {
            XmlDocument document = new() { XmlResolver = null };
            document.LoadXml(bindings.ToString());
            restored.RehearsalBindings.AddRange(DirectXmlToObject.ObjectFromXml<List<CQFCinematicBinding>>(document.DocumentElement, false));
            if (restored.RehearsalBindings.GroupBy(binding => binding.key).Any(group => group.Count() > 1))
                throw new InvalidDataException("CQF_Cinematic_RoleDuplicate".Translate(restored.DisplayName));
        }
        return restored;
    }

    public string StepPath(CQFCinematicStep? selected)
    {
        return FindStepPath(Definition.steps, selected, "") ?? "";
    }

    public CQFCinematicStep? StepAtPath(string path)
    {
        if (string.IsNullOrEmpty(path)) return null;
        List<CQFCinematicStep> owner = Definition.steps;
        CQFCinematicStep? step = null;
        foreach (string part in path.Split('/'))
        {
            if (!int.TryParse(part, out int index) || index < 0 || index >= owner.Count) return null;
            step = owner[index];
            owner = step switch { CQFCinematicStep_Sequence sequence => sequence.steps, CQFCinematicStep_Parallel parallel => parallel.steps, _ => new() };
        }
        return step;
    }

    public void RemoveLocation(string key)
    {
        if (Definition.GetReferencedTargetKeys(Locations.Select(entry => entry.key)).Contains(key))
            throw new InvalidOperationException("CQF_Cinematic_LocationInUse".Translate());
        Locations.RemoveAll(binding => binding.key == key);
        Definition.bindings.RemoveAll(binding => binding.key == key);
        Targets.Remove(key);
        TargetNames.Remove(key);
    }

    private static string? FindStepPath(List<CQFCinematicStep> owner, CQFCinematicStep? selected, string prefix)
    {
        if (selected == null) return null;
        for (int index = 0; index < owner.Count; index++)
        {
            string path = prefix + index.ToString(System.Globalization.CultureInfo.InvariantCulture);
            if (ReferenceEquals(owner[index], selected)) return path;
            List<CQFCinematicStep>? children = owner[index] switch
            {
                CQFCinematicStep_Sequence sequence => sequence.steps,
                CQFCinematicStep_Parallel parallel => parallel.steps,
                _ => null
            };
            if (children != null && FindStepPath(children, selected, path + "/") is string found) return found;
        }
        return null;
    }

    private void BindEntityRehearsal(string key, CQFEntityReference reference)
    {
        if (Current.Game == null) return;
        GameComponent_EntityBook book = Current.Game.GetComponent<GameComponent_EntityBook>()
            ?? throw new InvalidOperationException("CQF_EntityBook_Unavailable".Translate());
        List<CQFEntityRecord> records = book.GetRecords(reference.SourceId, Quest)
            .Where(record => !reference.MemberIndex.HasValue || record.MemberIndex == reference.MemberIndex.Value).ToList();
        if (records.Count != 1 || records[0].Pawn is not { Spawned: true, Destroyed: false } pawn || pawn.Map != Find.CurrentMap) return;
        SetRehearsalPawn(key, pawn);
    }

    private void ReadScene(string path)
    {
        if (!File.Exists(path)) return;
        XElement scene = ReadXml(File.ReadAllText(path, Encoding.UTF8));
        if (scene.Name != "CQFCinematicScene" || scene.Element("definition")?.Value != Definition.defName)
            throw new InvalidDataException("CQF_Cinematic_InvalidScene".Translate());
        DisplayName = scene.Element("displayName")?.Value ?? DisplayName;
        QuestID = scene.Element("questID")?.Value ?? "";
        ReadLocations(scene.Element("locations"));
        foreach (XElement name in scene.Element("targetNames")?.Elements("li") ?? Enumerable.Empty<XElement>())
        {
            string key = (string?)name.Attribute("key") ?? "";
            if (string.IsNullOrEmpty(key) || TargetNames.ContainsKey(key)) throw new InvalidDataException("CQF_Cinematic_InvalidScene".Translate());
            TargetNames.Add(key, name.Value);
        }
        if (scene.Element("rehearsalBindings") is XElement bindings)
        {
            XmlDocument document = new() { XmlResolver = null };
            document.LoadXml(bindings.ToString());
            RehearsalBindings.AddRange(DirectXmlToObject.ObjectFromXml<List<CQFCinematicBinding>>(document.DocumentElement, false));
            if (RehearsalBindings.GroupBy(binding => binding.key).Any(group => group.Count() > 1))
                throw new InvalidDataException("CQF_Cinematic_RoleDuplicate".Translate(DisplayName));
        }
        foreach (XElement entry in scene.Element("subtitles")?.Elements("li") ?? Enumerable.Empty<XElement>())
        {
            string key = entry.Element("key")?.Value ?? "";
            if (!IsOwnedSubtitleKey(key) || Subtitles.Any(text => text.Key == key)) throw new InvalidDataException("CQF_Cinematic_DuplicateSubtitle".Translate());
            Subtitles.Add(new CQFCinematicSubtitleText(key, entry.Element("chinese")?.Value ?? "", entry.Element("english")?.Value ?? ""));
        }
    }

    private string ReadReviewText(string key)
    {
        string value = ReadTranslation(key, ChineseFolder);
        return value.Length > 0 ? value : key.Translate().ToString();
    }

    private void ReadLocations(XElement? element)
    {
        if (element == null) return;
        XmlDocument document = new() { XmlResolver = null };
        document.LoadXml(element.ToString());
        Locations.AddRange(DirectXmlToObject.ObjectFromXml<List<CQFCinematicBinding>>(document.DocumentElement, false));
        if (Locations.Any(binding => !string.IsNullOrEmpty(binding.thingID))
            || Locations.GroupBy(binding => binding.key).Any(group => group.Count() > 1))
            throw new InvalidDataException("CQF_Cinematic_InvalidScene".Translate());
    }

    private static CQFCinematicDef ReadDefinition(string xml)
    {
        XElement root = ReadXml(xml);
        if (root.Name == "Defs")
        {
            if (root.Elements().Count() != 1) throw new InvalidDataException("CQF_Cinematic_InvalidXml".Translate());
            root = root.Elements().Single();
        }
        if ((root.Name != typeof(CQFCinematicDef).FullName && root.Name != nameof(CQFCinematicDef)) || root.Attribute("Class") != null)
            throw new InvalidDataException("CQF_Cinematic_InvalidXml".Translate());
        XmlDocument document = new() { XmlResolver = null };
        document.LoadXml(root.ToString());
        CQFCinematicDef result = DirectXmlToObject.ObjectFromXml<CQFCinematicDef>(document.DocumentElement, false)
            ?? throw new InvalidDataException("CQF_Cinematic_InvalidXml".Translate());
        DirectXmlCrossRefLoader.ResolveAllWantedCrossReferences(FailMode.LogErrors);
        return result;
    }

    private static XElement ReadXml(string xml)
    {
        using StringReader text = new(xml);
        using XmlReader reader = XmlReader.Create(text, new XmlReaderSettings { DtdProcessing = DtdProcessing.Prohibit, XmlResolver = null, MaxCharactersInDocument = 2097152 });
        return XElement.Load(reader);
    }

    private static void Validate(CQFCinematicDef definition)
    {
        XmlConvert.VerifyNCName(definition.defName);
        int separator = definition.defName.IndexOf('_');
        if (separator <= 0 || separator == definition.defName.Length - 1 || definition.defName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
            throw new InvalidDataException("CQF_Cinematic_InvalidName".Translate());
        List<string> errors = definition.ConfigErrors().ToList();
        if (errors.Count > 0) throw new InvalidDataException("CQF_Cinematic_InvalidProject".Translate() + "\n" + string.Join("\n", errors));
    }

    private static IEnumerable<CQFCinematicStep> EnumerateSteps(IEnumerable<CQFCinematicStep> entries)
    {
        foreach (CQFCinematicStep step in entries)
        {
            yield return step;
            IEnumerable<CQFCinematicStep>? children = step switch
            {
                CQFCinematicStep_Sequence sequence => sequence.steps,
                CQFCinematicStep_Parallel parallel => parallel.steps,
                _ => null
            };
            if (children != null) foreach (CQFCinematicStep child in EnumerateSteps(children)) yield return child;
        }
    }

    private static string ReadTranslation(string key, string folder)
    {
        string directory = Path.Combine(RootDirectory, "Languages", folder, "Keyed");
        if (!Directory.Exists(directory)) return "";
        foreach (string file in Directory.EnumerateFiles(directory, "*.xml", SearchOption.AllDirectories))
        {
            XElement language = ReadXml(File.ReadAllText(file, Encoding.UTF8));
            XElement? entry = language.Element(key);
            if (entry != null) return entry.Value;
        }
        return "";
    }

    private static void PublishSubtitles(IEnumerable<CQFCinematicSubtitleText> subtitles)
    {
        foreach (LoadedLanguage language in new[] { LanguageDatabase.defaultLanguage, LanguageDatabase.activeLanguage }.Where(language => language != null).Distinct())
        {
            language.LoadData();
            bool chinese = language.folderName.StartsWith("ChineseSimplified", StringComparison.OrdinalIgnoreCase);
            foreach (CQFCinematicSubtitleText text in subtitles)
                language.keyedReplacements[text.Key] = new LoadedLanguage.KeyedReplacement
                {
                    key = text.Key, value = chinese ? text.Chinese : text.English,
                    fileSource = SubtitleFileName, fileSourceFullPath = Path.Combine(RootDirectory, "Languages", chinese ? ChineseFolder : "English", "Keyed", SubtitleFileName)
                };
        }
    }

    private static bool IsOwnedSubtitleKey(string key) => key.StartsWith(SubtitlePrefix, StringComparison.Ordinal)
        && Guid.TryParseExact(key.Substring(SubtitlePrefix.Length), "N", out _);

    private static void WriteFiles(Dictionary<string, string> files)
    {
        Dictionary<string, byte[]?> previous = files.Keys.ToDictionary(path => path, path => File.Exists(path) ? File.ReadAllBytes(path) : null, StringComparer.OrdinalIgnoreCase);
        Dictionary<string, string> temporary = files.Keys.ToDictionary(path => path,
            path => Path.Combine(Path.GetDirectoryName(path)!, "." + Path.GetFileName(path) + "." + Guid.NewGuid().ToString("N") + ".cqftmp"), StringComparer.OrdinalIgnoreCase);
        List<string> written = new();
        try
        {
            foreach (KeyValuePair<string, string> entry in files)
            {
                Directory.CreateDirectory(Path.GetDirectoryName(entry.Key)!);
                File.WriteAllText(temporary[entry.Key], entry.Value, new UTF8Encoding(false));
            }
            foreach (string path in files.Keys)
            {
                written.Add(path);
                ReplaceFile(temporary[path], path);
            }
        }
        catch (Exception error)
        {
            foreach (string path in written)
                try
                {
                    if (previous[path] is byte[] content)
                    {
                        File.WriteAllBytes(temporary[path], content);
                        ReplaceFile(temporary[path], path);
                    }
                    else if (File.Exists(path)) File.Delete(path);
                }
                catch (Exception rollbackError) { Log.Error("[CQF] Cinematic project rollback failed: " + path + "\n" + rollbackError); }
            throw new IOException("CQF_Cinematic_ProjectSaveFailed".Translate(), error);
        }
        finally
        {
            foreach (string path in temporary.Values)
                try { if (File.Exists(path)) File.Delete(path); }
                catch (Exception error) { Log.Error("[CQF] Cinematic project temporary file cleanup failed: " + path + "\n" + error); }
        }
    }

    private static void ReplaceFile(string source, string destination)
    {
#if RIMWORLD_1_7
        File.Move(source, destination, true);
#else
        if (File.Exists(destination)) File.Replace(source, destination, null);
        else File.Move(source, destination);
#endif
    }

    private const string ChineseFolder = "ChineseSimplified (简体中文)";
    private const string SubtitlePrefix = "CQF_Cinematic_Subtitle_";
    private const string SubtitleFileName = "CQF_CinematicSubtitles.xml";
}
