namespace QuestEditor_Library;

public partial class DialogTreeDef
{
    public CQFDialogPresentation? ActivePresentation
    {
        get
        {
            if (string.IsNullOrEmpty(dialogStyle)) return null;
            CQFDialogPresentation[] matches = presentations.Where(item => item.Id == dialogStyle).ToArray();
            if (matches.Length != 1) throw new InvalidOperationException($"CQF dialog {defName}: presentation '{dialogStyle}' must have exactly one configuration.");
            return matches[0];
        }
    }

    public IEnumerable<string> PresentationErrors()
    {
        if (presentations == null || presentations.Any(item => item == null))
        {
            yield return $"CQF dialog {defName}: null presentation configuration.";
            yield break;
        }
        if (presentations.GroupBy(item => item.Id).Any(group => group.Count() > 1))
            yield return $"CQF dialog {defName}: duplicate presentation configuration.";
        if (!string.IsNullOrEmpty(dialogStyle) && !presentations.Any(item => item.Id == dialogStyle))
            yield return $"CQF dialog {defName}: missing presentation '{dialogStyle}'.";
        foreach (CQFDialogPresentation presentation in presentations.Where(item => item.Id == dialogStyle))
            foreach (string error in presentation.ConfigErrors()) yield return $"CQF dialog {defName}: {error}";
        foreach (var pair in nodeMoulds)
        {
            if (pair.Value.presentations == null || pair.Value.presentations.Any(item => item == null))
            {
                yield return $"CQF dialog {defName}, node {pair.Key}: null presentation configuration.";
                continue;
            }
            if (pair.Value.presentations.GroupBy(item => item.StyleId).Any(group => group.Count() > 1))
                yield return $"CQF dialog {defName}, node {pair.Key}: duplicate presentation configuration.";
            foreach (CQFDialogNodePresentation settings in pair.Value.presentations.Where(item => item.StyleId == dialogStyle))
                foreach (string error in settings.ConfigErrors()) yield return $"CQF dialog {defName}, node {pair.Key}: {error}";
        }
    }

    public override IEnumerable<string> ConfigErrors()
    {
        foreach (string error in base.ConfigErrors()) yield return error;
        foreach (string error in PresentationErrors()) yield return error;
    }

    public string dialogStyle = string.Empty;
    public List<CQFDialogPresentation> presentations = new();
}
