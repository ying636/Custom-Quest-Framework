using System.Runtime.CompilerServices;
using Verse.AI;

namespace QuestEditor_Library;

internal sealed class CQFCinematicJobBinding : IDisposable
{
    public CQFCinematicJobBinding(JobDriver driver, CQFCinematicStepState result)
    {
        this.result = result;
        driver.AddFinishAction(OnFinished);
    }

    public CQFCinematicJobBinding(Job job, CQFCinematicStepState result)
    {
        this.result = result;
        pendingJob = job;
        pending.Add(job, this);
    }

    public static void AttachDriver(Job job, JobDriver driver)
    {
        if (!pending.TryGetValue(job, out CQFCinematicJobBinding? binding) || binding.disposed) return;
        binding.ReleasePending();
        driver.AddFinishAction(binding.OnFinished);
    }

    public void ReleasePending()
    {
        if (pendingJob == null) return;
        if (pending.TryGetValue(pendingJob, out CQFCinematicJobBinding? binding) && ReferenceEquals(binding, this))
            pending.Remove(pendingJob);
        pendingJob = null;
    }

    public void Dispose()
    {
        if (disposed) return;
        disposed = true;
        ReleasePending();
    }

    private void OnFinished(JobCondition condition)
    {
        if (disposed) return;
        result.intValue = (int)condition;
        result.flag = true;
    }

    private readonly CQFCinematicStepState result;
    private Job? pendingJob;
    private bool disposed;
    private static readonly ConditionalWeakTable<Job, CQFCinematicJobBinding> pending = new();
}
