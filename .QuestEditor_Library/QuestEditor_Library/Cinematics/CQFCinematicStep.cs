using System.Reflection;
using System.Xml;
using System.Xml.Linq;
using Verse;

namespace QuestEditor_Library;

public abstract class CQFCinematicStep : IExposable, ISaveable
{
    public virtual bool PreviewSafe => true;
    public virtual IEnumerable<string> Channels => Array.Empty<string>();

    public bool Advance(CQFCinematicContext context, CQFCinematicStepState state, float realSeconds, int gameTicks)
    {
        if (context.Instance.Outcome != CQFCinematicOutcome.Playing) return false;
        if (state.completed) return true;
        Prepare(context, state);
        if (context.Instance.Outcome != CQFCinematicOutcome.Playing) return false;
        if (state.completed) return true;
        state.elapsed += clock == CQFCinematicClock.RealTime ? realSeconds : gameTicks / 60f;
        if (!OnUpdate(context, state, realSeconds, gameTicks)) return false;
        state.completed = true;
        OnExit(context, state, false);
        return true;
    }

    public void Prepare(CQFCinematicContext context, CQFCinematicStepState state)
    {
        if (context.Instance.Outcome != CQFCinematicOutcome.Playing) return;
        if (state.completed) return;
        if (context.IsPreview && !PreviewSafe) { state.completed = true; return; }
        if (!state.entered)
        {
            state.entered = true;
            OnEnter(context, state);
        }
    }

    public virtual void Restore(CQFCinematicContext context, CQFCinematicStepState state) { }

    public virtual void Cancel(CQFCinematicContext context, CQFCinematicStepState state)
    {
        if (!state.entered || state.completed) return;
        state.completed = true;
        OnExit(context, state, true);
    }

    public virtual IEnumerable<string> ConfigErrors()
    {
        if (float.IsNaN(duration) || float.IsInfinity(duration) || duration < 0f)
            yield return GetType().Name + ": invalid duration.";
        if (!Enum.IsDefined(typeof(CQFCinematicClock), clock)) yield return GetType().Name + ": invalid clock.";
    }

    public virtual void ExposeData()
    {
        Scribe_Values.Look(ref clock, "clock");
        Scribe_Values.Look(ref duration, "duration", 1f);
    }

    public virtual XElement SaveToXElement(string nodeName)
    {
        XElement element = new(nodeName, new XAttribute("Class", GetType().FullName!));
        foreach (FieldInfo field in GetType().GetFields(BindingFlags.Instance | BindingFlags.Public))
            if (!field.IsDefined(typeof(UnsavedAttribute)))
            {
                object? value = field.GetValue(this);
                element.Add(value is Def definition ? new XElement(field.Name, definition.defName)
                    : CQFSerialization.XElementFromObject(value!, field.FieldType, field.Name, field, true));
            }
        return element;
    }

    public CQFCinematicStep Copy()
    {
        XmlDocument document = new();
        document.LoadXml(new XElement("steps", SaveToXElement("li")).ToString());
        CQFCinematicStep copy = DirectXmlToObject.ObjectFromXml<List<CQFCinematicStep>>(document.DocumentElement, false).Single();
        DirectXmlCrossRefLoader.ResolveAllWantedCrossReferences(FailMode.LogErrors);
        return copy;
    }

    protected virtual void OnEnter(CQFCinematicContext context, CQFCinematicStepState state) { }
    protected abstract bool OnUpdate(CQFCinematicContext context, CQFCinematicStepState state, float realSeconds, int gameTicks);
    protected virtual void OnExit(CQFCinematicContext context, CQFCinematicStepState state, bool cancelled) { }

    public CQFCinematicClock clock;
    public float duration = 1f;
}
