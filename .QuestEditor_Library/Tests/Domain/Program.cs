using System.Reflection;
using System.Runtime.CompilerServices;
using HarmonyLib;
using QuestEditor_Library;
using RimWorld;
using Verse;
using Verse.AI;

internal static class Program
{
    private static void Main()
    {
        try
        {
            Run();
            Console.WriteLine("Domain checks passed: " + checks);
        }
        catch (Exception error)
        {
            Console.WriteLine(error);
            Environment.Exit(1);
        }
    }

    private static void Run()
    {
        Check(!typeof(DomainDef).Assembly.GetReferencedAssemblies().Any(reference => reference.Name == "RSH"), "framework has no blood sea dependency");
        MapComponent_Domain domain = CreateDomain(1000f);
        ProbeWorker worker = domain.GetWorker<ProbeWorker>()!;
        Check(worker.Domain == domain, "worker is bound to its own domain");
        Check(domain.Available && !domain.Active, "configured domain begins idle");
        domain.DrainHealth(100f);
        Check(domain.Health == 0f, "idle domain ignores health consumption");
        Set(domain, "active", true);
        Set(domain, "health", 1000f);
        Pawn caster = (Pawn)RuntimeHelpers.GetUninitializedObject(typeof(Pawn));
        caster.health = (Pawn_HealthTracker)RuntimeHelpers.GetUninitializedObject(typeof(Pawn_HealthTracker));
        typeof(Pawn_HealthTracker).GetField("healthState", BindingFlags.NonPublic | BindingFlags.Instance)!
            .SetValue(caster.health, PawnHealthState.Mobile);
        Set(domain, "caster", caster);
        ProbeAnchor anchor = new() { def = new ThingDef { building = new BuildingProperties() } };
        anchor.Initialize(domain.map);
        Set(domain, "anchor", anchor);
        Check(anchor.HitPoints == 1000, "anchor starts with the domain health");
        DamageInfo hit = new(new DamageDef { harmsHealth = true }, 125f);
        Check(domain.AbsorbDamage(caster, ref hit) && hit.Amount == 0f && domain.Health == 875f, "caster damage consumes shared health");
        Check(anchor.HitPoints == 875, "caster damage synchronizes the external anchor");
        DamageInfo external = new(new DamageDef { harmsHealth = true }, 800f);
        anchor.PreApplyDamage(ref external, out bool absorbed);
        Check(absorbed && domain.Health == 525f && anchor.HitPoints == 525, "anchor hit obeys its 350 damage cap without charging twice");
        domain.RestoreHealth(2000f);
        Check(domain.Health == 1000f && anchor.HitPoints == 1000, "healing clamps to maximum and synchronizes the anchor");
        domain.DrainHealth(0.25f);
        Check(domain.Health == 999.75f && anchor.HitPoints == 1000, "fractional domain health survives integer anchor projection");
        float before = domain.Health;
        DamageInfo other = new(new DamageDef { harmsHealth = true }, 30f);
        Pawn outsider = (Pawn)RuntimeHelpers.GetUninitializedObject(typeof(Pawn));
        Check(!domain.AbsorbDamage(outsider, ref other) && domain.Health == before && other.Amount == 30f, "unrelated pawns do not consume domain health");
        Throws<ArgumentOutOfRangeException>(() => domain.DrainHealth(float.NaN), "NaN damage is rejected");
        DamageInfo lethal = new(new DamageDef { harmsHealth = true }, 1200f);
        Check(!domain.AbsorbDamage(caster, ref lethal) && Math.Abs(lethal.Amount - 200.25f) < 0.001f, "damage exceeding domain health remains on the caster");
        Check(domain.Health == 0f && worker.Collapses == 1 && anchor.Removals == 1, "depletion collapses and removes the anchor once");
        domain.Collapse("repeat");
        domain.RestoreHealth(1000f);
        Check(worker.Collapses == 1 && domain.Health == 0f, "collapsed domain cannot be healed or collapsed again");
        Throws<InvalidOperationException>(() => domain.Configure(new DomainDef()), "occupied domain cannot change its definition");
        MapComponent_Domain otherDomain = CreateDomain(250f);
        Check(otherDomain.GetWorker<ProbeWorker>() != worker && otherDomain.MaximumHealth == 250f, "domain instances keep workers and health configuration separate");
        DomainDef invalid = new()
        {
            defName = "CQF_Check_InvalidDomain",
            workerClass = typeof(string),
            maximumHealth = float.NaN,
            maximumAnchorDamagePerHit = -1f,
            size = new IntVec3(0, 1, 0)
        };
        string[] errors = invalid.ConfigErrors().ToArray();
        Check(errors.Any(error => error.Contains("worker")) && errors.Any(error => error.Contains("maximum health"))
            && errors.Any(error => error.Contains("damage limit")) && errors.Any(error => error.Contains("map size")), "invalid domain definitions report their errors");
        CheckOpeningProtection(caster);
    }

    private static void CheckOpeningProtection(Pawn caster)
    {
        caster.ForceSetStateToUnspawned();
        MapComponent_Domain domain = CreateDomain(1000f);
        Set(domain, "caster", caster);
        Set(domain, "active", true);
        Set(domain, "health", 1000f);
        Check(!domain.ProtectsFromOpening(caster), "running domain has no permanent invulnerability");
        ProbeOpening opening = new() { caster = caster, active = true };
        domain.RegisterOpening(opening);
        domain.RegisterOpening(opening);
        Check(domain.ProtectsFromOpening(caster), "registered generic opening protects its caster");
        Pawn other = (Pawn)RuntimeHelpers.GetUninitializedObject(typeof(Pawn));
        other.health = (Pawn_HealthTracker)RuntimeHelpers.GetUninitializedObject(typeof(Pawn_HealthTracker));
        typeof(Pawn_HealthTracker).GetField("healthState", BindingFlags.NonPublic | BindingFlags.Instance)!
            .SetValue(other.health, PawnHealthState.Mobile);
        Check(!domain.ProtectsFromOpening(other), "opening does not protect unrelated participants");
        opening.active = false;
        Check(!domain.ProtectsFromOpening(caster), "inactive opening releases invulnerability");
        opening.active = true;
        domain.UnregisterOpening(opening);
        Check(!domain.ProtectsFromOpening(caster), "one unregister removes a repeatedly registered opening");
        Set(domain, "arrivalLocked", true);
        Check(domain.ProtectsFromOpening(caster) && !domain.ProtectsFromOpening(other), "arrival lock protects only the domain caster");
        Set(domain, "arrivalLocked", false);
        Check(!domain.ProtectsFromOpening(caster), "arrival unlock releases invulnerability");
        Set(domain, "entering", true);
        Check(domain.ProtectsFromOpening(caster), "synchronous domain transfer also protects its caster");
        Set(domain, "entering", false);
        Check(!domain.ProtectsFromOpening(caster), "transfer completion releases its temporary protection");
        AbilityDef openingAbility = new() { defName = "CQF_Check_OpeningAbility" };
        DefDatabase<DomainDef>.Add(new DomainDef { defName = "CQF_Check_CastingDomain", openingAbility = openingAbility });
        Ability ability = (Ability)RuntimeHelpers.GetUninitializedObject(typeof(Ability));
        ability.def = openingAbility;
        caster.jobs = (Pawn_JobTracker)RuntimeHelpers.GetUninitializedObject(typeof(Pawn_JobTracker));
        caster.jobs.curJob = new Job { ability = ability };
        Check(Patch_DomainOpeningProtection.IsInvulnerable(caster), "configured domain ability protects its caster before the opening spawns");
        Harmony harmony = new("CQF.Checks.DomainOpeningProtection");
        try
        {
            harmony.CreateClassProcessor(typeof(Patch_DomainOpeningProtection)).Patch();
            DamageInfo hit = new(new DamageDef { harmsHealth = true }, 999f);
            caster.PreApplyDamage(ref hit, out bool absorbed);
            Check(absorbed && hit.Amount == 0f && domain.Health == 1000f,
                "registered generic Harmony prefix blocks damage without spending domain health");
        }
        finally { harmony.UnpatchAll(harmony.Id); }
        typeof(Pawn_HealthTracker).GetField("healthState", BindingFlags.NonPublic | BindingFlags.Instance)!
            .SetValue(caster.health, PawnHealthState.Down);
        Check(!Patch_DomainOpeningProtection.IsInvulnerable(caster), "downed pawn cannot keep casting invulnerability");
        typeof(Pawn_HealthTracker).GetField("healthState", BindingFlags.NonPublic | BindingFlags.Instance)!
            .SetValue(caster.health, PawnHealthState.Mobile);
        ability.def = new AbilityDef { defName = "CQF_Check_UnrelatedAbility" };
        Check(!Patch_DomainOpeningProtection.IsInvulnerable(caster), "ordinary ability does not grant domain invulnerability");
        caster.jobs.curJob = null;
        Check(!Patch_DomainOpeningProtection.IsInvulnerable(caster), "ending the casting job releases invulnerability");
    }

    private static MapComponent_Domain CreateDomain(float maximum)
    {
        Map map = (Map)RuntimeHelpers.GetUninitializedObject(typeof(Map));
        MapComponent_Domain domain = new(map);
        typeof(Map).GetField("components", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)!
            .SetValue(map, new List<MapComponent> { domain });
        domain.Configure(new DomainDef
        {
            defName = "CQF_Check_Domain",
            maximumHealth = maximum,
            maximumAnchorDamagePerHit = 350f,
            workerClass = typeof(ProbeWorker)
        });
        return domain;
    }

    private static void Set(object target, string name, object value)
    {
        typeof(MapComponent_Domain).GetField(name, BindingFlags.NonPublic | BindingFlags.Instance)!.SetValue(target, value);
    }

    private static void Check(bool condition, string name)
    {
        if (!condition) throw new InvalidOperationException(name);
        checks++;
        Console.WriteLine("PASS " + name);
    }

    private static void Throws<T>(Action action, string name) where T : Exception
    {
        try { action(); }
        catch (T) { Check(true, name); return; }
        throw new InvalidOperationException(name);
    }

    private static int checks;
}
