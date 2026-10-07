namespace QuestEditor_Library
{
    public sealed class CQFAIStreamUpdate
    {
        public CQFAIStreamUpdate(string text, string reasoning, string[] tools)
        {
            Text = text; Reasoning = reasoning; Tools = tools;
        }
        public string Text { get; }
        public string Reasoning { get; }
        public IReadOnlyList<string> Tools { get; }
    }
}
