using RimWorld;
using System.Xml.Linq;
using Verse;

namespace QuestEditor_Library;

public sealed class CQFCinematicDef : Def, ISaveable
{
    public override IEnumerable<string> ConfigErrors()
    {
        foreach (string error in base.ConfigErrors()) yield return error;
        if (string.IsNullOrWhiteSpace(defName) || defName.IndexOf('_') <= 0)
            yield return "Cinematic defName requires a mod prefix.";
        if (steps == null || steps.Count == 0) yield return "Cinematic requires steps.";
        else
        {
            string? topologyError = CheckTopology(steps, new HashSet<CQFCinematicStep>(), 0);
            if (topologyError != null) { yield return topologyError; yield break; }
            foreach (CQFCinematicStep step in steps)
            {
                if (step == null) { yield return "Cinematic contains a null step."; continue; }
                foreach (string error in step.ConfigErrors()) yield return error;
            }
        }
        if (float.IsNaN(maxDuration) || float.IsInfinity(maxDuration) || maxDuration <= 0f)
            yield return "Cinematic requires a finite positive timeout.";
        if (skipActions == null || skipActions.Any(action => action == null)) yield return "Cinematic contains invalid skip actions.";
        if (bindings == null || bindings.Any(binding => binding == null || string.IsNullOrWhiteSpace(binding.key)
            || string.IsNullOrWhiteSpace(binding.worldSeed) || binding.mapID < 0 || !binding.cell.IsValid))
            yield return "Cinematic contains invalid recorded bindings.";
        else if (bindings.Select(binding => binding.key).Distinct(StringComparer.Ordinal).Count() != bindings.Count)
            yield return "Cinematic contains duplicate recorded binding keys.";
        if (roles == null) yield return "CQF_Cinematic_RoleKeyRequired".Translate(defName);
        else
        {
            foreach (CQFCinematicRole role in roles)
            {
                if (role == null) { yield return "CQF_Cinematic_RoleKeyRequired".Translate(defName); continue; }
                foreach (string error in role.ConfigErrors()) yield return error;
            }
            foreach (IGrouping<string, CQFCinematicRole> duplicate in roles.Where(role => role != null).GroupBy(role => role.key, StringComparer.Ordinal).Where(group => group.Count() > 1))
                yield return "CQF_Cinematic_RoleDuplicate".Translate(duplicate.First().DisplayName);
        }
    }

    public HashSet<string> GetReferencedTargetKeys(IEnumerable<string>? candidates = null)
    {
        HashSet<string> known = new(candidates ?? bindings.Select(binding => binding.key).Concat(roles.Select(role => role.key)), StringComparer.Ordinal);
        XElement content = new("cinematic", CQFSerialization.SaveList_Saveable(steps, "steps"),
            CQFSerialization.SaveList_Saveable(skipActions, "skipActions"));
        return new HashSet<string>(content.Descendants().Where(element => !element.HasElements)
            .Select(element => element.Value).Where(known.Contains), StringComparer.Ordinal);
    }

    public Dictionary<string, TargetInfo> ResolveBindings(Map fallbackMap, IDictionary<string, TargetInfo>? overrides = null, Quest? quest = null)
    {
        Dictionary<string, TargetInfo> result = new(StringComparer.Ordinal);
        HashSet<string> referenced = GetReferencedTargetKeys();
        HashSet<string> roleKeys = new(StringComparer.Ordinal);
        foreach (CQFCinematicRole role in roles)
        {
            if (!referenced.Contains(role.key)) continue;
            if (!roleKeys.Add(role.key)) throw new InvalidOperationException("CQF_Cinematic_RoleDuplicate".Translate(role.DisplayName));
            result.Add(role.key, role.Resolve(fallbackMap, bindings, overrides, quest));
        }
        foreach (CQFCinematicBinding binding in bindings)
        {
            if (!referenced.Contains(binding.key) || roleKeys.Contains(binding.key)) continue;
            if (overrides != null && overrides.ContainsKey(binding.key)) continue;
            if (result.ContainsKey(binding.key)) throw new InvalidOperationException("[CQF] Duplicate cinematic binding: " + binding.key);
            result.Add(binding.key, binding.Resolve(fallbackMap));
        }
        if (overrides != null)
            foreach (KeyValuePair<string, TargetInfo> entry in overrides) result[entry.Key] = entry.Value;
        return result;
    }

    public XElement SaveToXElement(string nodeName)
    {
        return new XElement(nodeName,
            new XElement("defName", defName),
            new XElement("label", label ?? defName),
            new XElement("allowSkip", allowSkip), new XElement("lockInput", lockInput),
            new XElement("restoreCamera", restoreCamera), new XElement("pauseGame", pauseGame),
            new XElement("maxDuration", maxDuration), new XElement("completedSignal", completedSignal),
            new XElement("skippedSignal", skippedSignal), new XElement("cancelledSignal", cancelledSignal),
            new XElement("failedSignal", failedSignal), new XElement("addQuestPrefix", addQuestPrefix),
            CQFSerialization.SaveList_Saveable(steps, "steps"), CQFSerialization.SaveList_Saveable(skipActions, "skipActions"),
            CQFSerialization.SaveList_Saveable(bindings, "bindings"), CQFSerialization.SaveList_Saveable(roles, "roles"));
    }

    private string? CheckTopology(List<CQFCinematicStep> entries, HashSet<CQFCinematicStep> ancestors, int depth)
    {
        if (depth > 32) return "Cinematic nesting exceeds 32 levels.";
        foreach (CQFCinematicStep step in entries)
        {
            if (step == null) continue;
            if (!ancestors.Add(step)) return "Cinematic steps contain a cycle.";
            List<CQFCinematicStep>? children = step switch
            {
                CQFCinematicStep_Sequence sequence => sequence.steps,
                CQFCinematicStep_Parallel parallel => parallel.steps,
                _ => null
            };
            if (children != null)
            {
                string? error = CheckTopology(children, ancestors, depth + 1);
                if (error != null) return error;
            }
            else if (pauseGame && (step.clock == CQFCinematicClock.GameTime
                || step is CQFCinematicStep_PawnMove or CQFCinematicStep_PawnWait or CQFCinematicStep_PawnJob))
                return "A paused cinematic cannot contain game-time steps: " + step.GetType().Name;
            ancestors.Remove(step);
        }
        return null;
    }

    public List<CQFCinematicStep> steps = new();
    public List<CQFCinematicBinding> bindings = new();
    public List<CQFCinematicRole> roles = new();
    public bool allowSkip = true;
    public bool lockInput = true;
    public bool restoreCamera = true;
    public bool pauseGame;
    public float maxDuration = 600f;
    public string completedSignal = "";
    public string skippedSignal = "";
    public string cancelledSignal = "";
    public string failedSignal = "";
    public bool addQuestPrefix = true;
    public List<CQFAction> skipActions = new();
}
