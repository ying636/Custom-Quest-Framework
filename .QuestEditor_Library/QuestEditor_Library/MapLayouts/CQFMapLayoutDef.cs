using Verse;

namespace QuestEditor_Library;

public sealed class CQFMapLayoutDef : Def
{
    public CQFLayoutWorker CreateWorker()
    {
        if (workerClass == null || workerClass.IsAbstract || !typeof(CQFLayoutWorker).IsAssignableFrom(workerClass))
            throw new CQFLayoutException("CQF_Layout_InvalidWorker: " + defName);
        return (CQFLayoutWorker)Activator.CreateInstance(workerClass)!;
    }

    public override IEnumerable<string> ConfigErrors()
    {
        foreach (string error in base.ConfigErrors()) yield return error;
        if (workerClass == null || workerClass.IsAbstract || !typeof(CQFLayoutWorker).IsAssignableFrom(workerClass)) yield return "CQF_Layout_InvalidWorker: " + defName;
    }

    public Type workerClass = typeof(CQFLayoutWorker_Scatter);
    public List<CQFLayoutPartRule> parts = new List<CQFLayoutPartRule>();
}
