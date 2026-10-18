using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using HarmonyLib;
using QuestEditor_Library;
using RimWorld;
using Verse;
using Verse.AI;

internal static class Program
{
    private static void Main(string[] args)
    {
        try
        {
            if (args.Contains("visuals")) { DomainVisualChecks.Run(); return; }
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
        CheckProtectionLifecycle(caster);
        CheckPausedWorker();
        Check(!typeof(DomainDef).Assembly.GetTypes().Any(type => type.IsDefined(typeof(HarmonyPatch), false)), "domain module contains no Harmony patches");
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
        DomainDef definition = new() { defName = "CQF_Check_CastingDomain", openingAbility = openingAbility };
        definition.ResolveReferences();
        definition.ResolveReferences();
        new DomainDef { defName = "CQF_Check_SharedAbility", openingAbility = openingAbility }.ResolveReferences();
        Check(openingAbility.comps.Count(properties => properties is AbilityCompProperties_DomainProtection) == 1,
            "repeated and shared domain definitions register one native ability component");
        Ability ability = (Ability)RuntimeHelpers.GetUninitializedObject(typeof(Ability));
        ability.def = openingAbility;
        ability.pawn = caster;
        AbilityComp_DomainProtection abilityComp = new() { parent = ability };
        abilityComp.Initialize(openingAbility.comps.Single());
        Comp_DomainProtection protection = caster.AllComps.OfType<Comp_DomainProtection>().Single();
        Check(ReferenceEquals(protection, Comp_DomainProtection.Ensure(caster)), "ability and domain reuse one damage component per caster");
        caster.jobs = (Pawn_JobTracker)RuntimeHelpers.GetUninitializedObject(typeof(Pawn_JobTracker));
        caster.jobs.curJob = new Job { ability = ability };
        Check(protection.IsInvulnerable(), "configured domain ability protects its caster before the opening spawns");
        caster.health.hediffSet = new HediffSet(caster);
        DamageInfo hit = new(new DamageDef { harmsHealth = true }, 999f);
        DynamicMethod nativeDamage = new("CQF_Check_NativeCompDamage", typeof(void),
            new[] { typeof(ThingWithComps), typeof(DamageInfo).MakeByRefType(), typeof(bool).MakeByRefType() }, typeof(Program).Module, true);
        ILGenerator il = nativeDamage.GetILGenerator();
        il.Emit(OpCodes.Ldarg_0);
        il.Emit(OpCodes.Ldarg_1);
        il.Emit(OpCodes.Ldarg_2);
        il.Emit(OpCodes.Call, typeof(ThingWithComps).GetMethod(nameof(ThingWithComps.PreApplyDamage))!);
        il.Emit(OpCodes.Ret);
        object[] arguments = { caster, hit, false };
        nativeDamage.Invoke(null, arguments);
        hit = (DamageInfo)arguments[1];
        bool absorbed = (bool)arguments[2];
        Check(absorbed && hit.Amount == 0f && domain.Health == 1000f,
            "native ThingWithComps damage pipeline invokes the caster component without Harmony");
        typeof(ThingWithComps).GetField("comps", BindingFlags.NonPublic | BindingFlags.Instance)!.SetValue(caster, new List<ThingComp>());
        abilityComp.Initialize(openingAbility.comps.Single());
        protection = caster.AllComps.OfType<Comp_DomainProtection>().Single();
        Check(protection.IsInvulnerable(), "ability initialization restores protection after native components are recreated");
        typeof(Pawn_HealthTracker).GetField("healthState", BindingFlags.NonPublic | BindingFlags.Instance)!
            .SetValue(caster.health, PawnHealthState.Down);
        Check(!protection.IsInvulnerable(), "downed pawn cannot keep casting invulnerability");
        typeof(Pawn_HealthTracker).GetField("healthState", BindingFlags.NonPublic | BindingFlags.Instance)!
            .SetValue(caster.health, PawnHealthState.Mobile);
        ability.def = new AbilityDef { defName = "CQF_Check_UnrelatedAbility" };
        Check(!protection.IsInvulnerable(), "ordinary ability does not grant domain invulnerability");
        caster.jobs.curJob = null;
        Check(!protection.IsInvulnerable(), "ending the casting job releases invulnerability");
        DamageInfo ordinary = new(new DamageDef { harmsHealth = true }, 30f);
        protection.PostPreApplyDamage(ref ordinary, out bool blocked);
        Check(!blocked && ordinary.Amount == 30f, "inactive protection lets normal damage continue");
        Set(domain, "caster", caster);
        Set(domain, "active", true);
        Set(domain, "health", 1000f);
        Hediff_DomainProtection hediff = new() { pawn = caster, def = new HediffDef { defName = "CQF_Check_Protection" } };
        hediff.Initialize(domain);
        hediff.EnsureProtection();
        Check(caster.AllComps.OfType<Comp_DomainProtection>().Count() == 1, "hediff and ability keep a single protection component");
        DamageInfo shared = new(new DamageDef { harmsHealth = true }, 125f);
        protection.PostPreApplyDamage(ref shared, out blocked);
        Check(blocked && shared.Amount == 0f && domain.Health == 875f, "ordinary domain damage still consumes shared health");
        hediff.PostRemoved();
        Check(protection.Protection == null && caster.AllComps.Contains(protection), "removing domain hediff releases shared health while preserving ability protection");
        ability.def = openingAbility;
        caster.jobs.curJob = new Job { ability = ability };
        Check(protection.IsInvulnerable(), "caster can start another protected domain after recall");
        caster.jobs.curJob = null;
    }

    private static void CheckProtectionLifecycle(Pawn caster)
    {
        Game previous = Current.Game;
        LoadSaveMode previousMode = Scribe.mode;
        Game game = (Game)RuntimeHelpers.GetUninitializedObject(typeof(Game));
        MapComponent_Domain domain = CreateDomain(1000f);
        typeof(Game).GetField("maps", BindingFlags.NonPublic | BindingFlags.Instance)!.SetValue(game, new List<Map> { domain.map });
        Current.Game = game;
        try
        {
            typeof(Thing).GetField("mapIndexOrState", BindingFlags.NonPublic | BindingFlags.Instance)!.SetValue(caster, (sbyte)0);
            Set(domain, "caster", caster);
            Set(domain, "active", true);
            Set(domain, "health", 1000f);
            Comp_DomainProtection component = Comp_DomainProtection.Ensure(caster);
            ProbeOpening opening = new() { caster = caster, active = true };
            domain.RegisterOpening(opening);
            DamageInfo damage = new(new DamageDef { harmsHealth = true }, 75f);
            component.PostPreApplyDamage(ref damage, out bool absorbed);
            Check(absorbed && damage.Amount == 0f && domain.Health == 1000f, "opening component blocks damage without consuming shared health");
            domain.UnregisterOpening(opening);
            Check(!component.IsInvulnerable(), "opening removal immediately releases component invulnerability");
            Set(domain, "entering", true);
            Check(component.IsInvulnerable(), "caster component covers synchronous transfer");
            Set(domain, "entering", false);
            Set(domain, "arrivalLocked", true);
            damage = new DamageInfo(new DamageDef { harmsHealth = true }, 75f);
            component.PostPreApplyDamage(ref damage, out absorbed);
            Check(absorbed && damage.Amount == 0f && domain.Health == 1000f, "arrival component blocks damage without consuming shared health");
            Set(domain, "arrivalLocked", false);
            Check(!component.IsInvulnerable(), "arrival completion releases component invulnerability");
            typeof(ThingWithComps).GetField("comps", BindingFlags.NonPublic | BindingFlags.Instance)!.SetValue(caster, new List<ThingComp>());
            domain.FinalizeInit();
            Check(caster.AllComps.OfType<Comp_DomainProtection>().Count() == 1, "domain finalization restores caster protection without an ability or hediff");
            caster.AllComps.Clear();
            Ability ability = (Ability)RuntimeHelpers.GetUninitializedObject(typeof(Ability));
            ability.pawn = caster;
            AbilityComp_DomainProtection abilityComp = new() { parent = ability };
            Scribe.mode = LoadSaveMode.PostLoadInit;
            abilityComp.PostExposeData();
            abilityComp.PostExposeData();
            Scribe.mode = previousMode;
            component = caster.AllComps.OfType<Comp_DomainProtection>().Single();
            Check(component.parent == caster, "ability post-load callback restores exactly one caster component");
            for (int i = 0; i < 16; i++) component.IsInvulnerable();
            long allocated = GC.GetAllocatedBytesForCurrentThread();
            for (int i = 0; i < 256; i++) component.IsInvulnerable();
            allocated = GC.GetAllocatedBytesForCurrentThread() - allocated;
            Check(allocated == 0, "inactive protection check allocates zero bytes across 256 hits (actual: " + allocated + ")");
        }
        finally
        {
            Scribe.mode = previousMode;
            caster.ForceSetStateToUnspawned();
            Current.Game = previous;
        }
    }

    private static void CheckPausedWorker()
    {
        Game previous = Current.Game;
        ProgramState previousState = Current.ProgramState;
        Game game = (Game)RuntimeHelpers.GetUninitializedObject(typeof(Game));
        game.components = new();
        game.currentMapIndex = -1;
        MapComponent_Domain domain = CreateDomain(1000f);
        domain.map.info = new MapInfo { parent = (MapParent_Domain)RuntimeHelpers.GetUninitializedObject(typeof(MapParent_Domain)) };
        domain.map.spawnedThings = new ThingOwner<Thing>(domain.map);
        domain.map.regionAndRoomUpdater = new RegionAndRoomUpdater(domain.map) { Enabled = true };
        typeof(Game).GetField("maps", BindingFlags.NonPublic | BindingFlags.Instance)!.SetValue(game, new List<Map> { domain.map });
        Current.Game = game;
        Current.ProgramState = ProgramState.Entry;
        try
        {
            GameComponent_MapManager host = new(game);
            game.components.Add(host);
            CQFDomainRuntime runtime = new(host);
            typeof(CQFDomainRuntime).GetField("domainMaps", BindingFlags.NonPublic | BindingFlags.Instance)!.SetValue(runtime, new List<Map> { domain.map });
            MethodInfo update = typeof(CQFDomainRuntime).GetMethod("UpdateDomains", BindingFlags.NonPublic | BindingFlags.Instance)!;
            ProbeWorker worker = domain.GetWorker<ProbeWorker>()!;
            update.Invoke(runtime, null);
            Check(worker.ManagedUpdates == 1, "active domain receives managed updates");
            host.Hibernate(domain.map);
            host.GameComponentUpdate();
            update.Invoke(runtime, null);
            Check(worker.ManagedUpdates == 1 && !game.Maps.Contains(domain.map), "hibernating domain cache is detached without running its worker");
            host.Activate(domain.map);
            update.Invoke(runtime, null);
            Check(worker.ManagedUpdates == 2 && host.TotalMapCount == 1, "domain activation resumes its worker without adding another map");
        }
        finally
        {
            Current.ProgramState = previousState;
            Current.Game = previous;
        }
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
