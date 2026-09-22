namespace QuestEditor_Library
{
    public sealed class CQFEditorDiagnostic
    {
        public CQFEditorDiagnostic(string text, IDrawable editable = null)
        {
            this.Text = text;
            this.Editable = editable;
        }

        public string Text { get; }
        public IDrawable Editable { get; }
    }
}
