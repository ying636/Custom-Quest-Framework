using System.Reflection;
using System.Runtime.CompilerServices;
using HarmonyLib;
using QuestEditor_Library;
using Verse;
using Verse.AI;

internal static class CinematicJobBindingChecks
{
    public static int Run()
    {
        int checks = 0;
        Type bindingType = typeof(CQFCinematicStep_PawnJob).Assembly.GetType("QuestEditor_Library.CQFCinematicJobBinding", true)!;
        Type patchType = bindingType.Assembly.GetType("QuestEditor_Library.Patch_CQFCinematicJobDriver", true)!;
        Harmony harmony = new("CQF.CinematicChecks.JobBinding");
        harmony.Patch(AccessTools.Method(typeof(Job), nameof(Job.MakeDriver)), postfix: new HarmonyMethod(patchType.GetMethod("Postfix", BindingFlags.Static | BindingFlags.NonPublic)!));
        try
        {
            Job job = CreateJob();
            CQFCinematicStepState result = new();
            using (IDisposable binding = Bind(bindingType, job, result))
            {
                Job unrelated = CreateJob();
                Driver(unrelated).CompleteImmediately(JobCondition.Succeeded);
                Check(!result.flag, "unrelated job drivers cannot complete a cinematic binding"); checks++;
                CinematicBindingTestDriver driver = Driver(job);
                ReleasePending(bindingType, binding);
                driver.CompleteImmediately(JobCondition.Succeeded);
                Check(result.flag && result.intValue == (int)JobCondition.Succeeded, "native MakeDriver attaches before synchronous success"); checks++;
            }

            job = CreateJob(); result = new();
            using (IDisposable binding = Bind(bindingType, job, result))
            {
                Driver(job).CompleteImmediately(JobCondition.Incompletable);
                Check(result.flag && result.intValue == (int)JobCondition.Incompletable, "synchronous failure keeps its real JobCondition"); checks++;
            }

            job = CreateJob(); result = new();
            IDisposable disposed = Bind(bindingType, job, result);
            disposed.Dispose(); disposed.Dispose();
            Driver(job).CompleteImmediately(JobCondition.Succeeded);
            Check(!result.flag, "idempotent disposal removes a pending binding"); checks++;

            job = CreateJob(); result = new();
            using (IDisposable binding = Bind(bindingType, job, result))
            {
                ReleasePending(bindingType, binding);
                Driver(job).CompleteImmediately(JobCondition.Succeeded);
                Check(!result.flag, "failed startup finally clears pending registration"); checks++;
                using IDisposable replacement = Bind(bindingType, job, new CQFCinematicStepState());
                Check(replacement != null, "released job identity can be registered again"); checks++;
            }

            job = CreateJob(); result = new();
            CinematicBindingTestDriver restoredDriver = Driver(job);
            using (IDisposable binding = (IDisposable)Activator.CreateInstance(bindingType, restoredDriver, result)!)
            {
                restoredDriver.CompleteImmediately(JobCondition.Succeeded);
                Check(result.flag && result.intValue == (int)JobCondition.Succeeded, "restored driver completion listener is rebuilt"); checks++;
            }

            job = CreateJob(); result = new();
            disposed = Bind(bindingType, job, result);
            CinematicBindingTestDriver cancelledDriver = Driver(job);
            disposed.Dispose();
            cancelledDriver.CompleteImmediately(JobCondition.InterruptForced);
            Check(!result.flag, "disposed attached callbacks cannot mutate a cancelled step"); checks++;

            job = CreateJob(); result = new();
            using (IDisposable binding = Bind(bindingType, job, result))
            {
                CinematicBindingTestDriver original = Driver(job);
                Driver(job).CompleteImmediately(JobCondition.Incompletable);
                Check(!result.flag, "a pending binding attaches only to its first driver"); checks++;
                original.CompleteImmediately(JobCondition.Succeeded);
                Check(result.flag && result.intValue == (int)JobCondition.Succeeded, "the first driver retains its own completion listener"); checks++;
            }
            return checks;
        }
        finally
        {
            harmony.Unpatch(AccessTools.Method(typeof(Job), nameof(Job.MakeDriver)), HarmonyPatchType.All, harmony.Id);
        }
    }

    private static IDisposable Bind(Type bindingType, Job job, CQFCinematicStepState result)
    {
        return (IDisposable)Activator.CreateInstance(bindingType, job, result)!;
    }

    private static void ReleasePending(Type bindingType, IDisposable binding)
    {
        bindingType.GetMethod("ReleasePending")!.Invoke(binding, null);
    }

    private static Job CreateJob()
    {
        Job job = (Job)RuntimeHelpers.GetUninitializedObject(typeof(Job));
        job.def = new JobDef { defName = "CQF_CinematicBindingCheck", driverClass = typeof(CinematicBindingTestDriver) };
        return job;
    }

    private static CinematicBindingTestDriver Driver(Job job)
    {
        Pawn pawn = (Pawn)RuntimeHelpers.GetUninitializedObject(typeof(Pawn));
        return (CinematicBindingTestDriver)job.MakeDriver(pawn);
    }

    private static void Check(bool value, string message)
    {
        if (!value) throw new InvalidOperationException("Cinematic job binding check failed: " + message);
    }
}
