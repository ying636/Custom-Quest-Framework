using Verse;

namespace QuestEditor_Library;

public sealed class CQFLayoutException : InvalidOperationException
{
    public CQFLayoutException(string detail) : base(detail)
    {
    }

    public override string Message
    {
        get
        {
            int separator = base.Message.IndexOf(':');
            string key = separator < 0 ? base.Message : base.Message.Substring(0, separator);
            string translated = LanguageDatabase.activeLanguage == null ? key : key.Translate().ToString();
            return translated + (separator < 0 ? "" : base.Message.Substring(separator));
        }
    }
}
