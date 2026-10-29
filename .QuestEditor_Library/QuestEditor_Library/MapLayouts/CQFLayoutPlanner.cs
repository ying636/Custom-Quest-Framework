using Verse;

namespace QuestEditor_Library;

public sealed class CQFLayoutPlanner
{
    public CQFLayoutPlan Plan(CQFLayoutPlanningContext context, IEnumerable<CQFMapRegion?> references)
    {
        CustomMapStep_Layout settings = context.Settings;
        settings.Validate();
        List<CQFLayoutPartRule> rules = settings.parts.Count > 0 ? settings.parts : settings.layout!.parts;
        if (rules.Count == 0) throw new CQFLayoutException("CQF_Layout_NoRules");
        int index = 0;
        foreach (CQFMapRegion? reference in references)
        {
            reference?.Validate();
            context.Reference = reference;
            CQFLayoutWorker worker = settings.layout!.CreateWorker();
            worker.Prepare(context);
            foreach (CQFLayoutPartRule rule in rules)
            {
                if (rule.count.min < 0 || rule.count.max < rule.count.min || rule.count.max > 256)
                    throw new CQFLayoutException("CQF_Layout_InvalidCount: " + rule.role);
                int count = worker.Count(context, rule, context.Random.Next(rule.count.min, rule.count.max + 1));
                List<(CustomMapDataDef Data, float Weight)> candidates = rule.Candidates(context.Definitions).ToList();
                for (int n = 0; n < count; n++, index++)
                {
                    List<IntVec3> locations = worker.Locations(context, rule, index).Take(settings.maxAttempts).ToList();
                    List<(CQFLayoutPlacement Part, float Weight)> available = new List<(CQFLayoutPlacement, float)>();
                    foreach ((CustomMapDataDef data, float weight) in candidates)
                    {
                        context.Counts.TryGetValue(data.Origin, out int generated);
                        int limit = data.Origin.generationLimit;
                        if (limit > 0 && generated >= limit || rule.maxPerDefinition > 0 && generated >= rule.maxPerDefinition) continue;
                        CQFLayoutExecution.ValidateTemplate(data);
                        List<Rot4> rotations = rule.rotate && data.rot.IsValid ? Enumerable.Range(0, 4).Select(i => new Rot4(i)).ToList() : new List<Rot4> { data.rot.IsValid ? data.rot : Rot4.North };
                        int offset = context.Random.Next(rotations.Count);
                        CQFLayoutPlacement? found = null;
                        foreach (Rot4 rotation in rotations.Skip(offset).Concat(rotations.Take(offset)))
                        {
                            CQFLayoutPlacement part = new CQFLayoutPlacement(data, rotation) { Role = rule.role, ReferenceId = reference?.id ?? "" };
                            foreach (IntVec3 location in locations)
                            {
                                part.MoveCenter(location);
                                if (worker.Accept(context, part) && context.CanPlace(part)) { found = part; break; }
                            }
                            if (found != null) break;
                        }
                        if (found != null) available.Add((found, weight));
                    }
                    if (available.Count == 0)
                    {
                        string failure = "CQF_Layout_NoPlacement: " + rule.role + " [" + string.Join(",", rule.requiredTags.Concat(rule.pool.tags.Select(t => t.tag))) + "] " + n + "/" + count;
                        if (!rule.optional) throw new CQFLayoutException(failure);
                        context.Plan.Notices.Add(failure); break;
                    }
                    double pick = context.Random.NextDouble() * available.Sum(p => (double)p.Weight);
                    CQFLayoutPlacement chosen = available[available.Count - 1].Part;
                    foreach (var option in available) { pick -= option.Weight; if (pick < 0) { chosen = option.Part; break; } }
                    context.Add(chosen);
                }
            }
            worker.Finish(context);
        }
        if (settings.connectRoads) new CQFLayoutRoadPlanner().Connect(context);
        return context.Plan;
    }
}
