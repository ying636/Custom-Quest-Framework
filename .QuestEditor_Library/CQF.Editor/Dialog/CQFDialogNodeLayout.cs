namespace QuestEditor_Library
{
    public sealed class CQFDialogNodeLayout
    {
        public CQFDialogNodeLayout(DialogNode node, float bodyHeight)
        {
            BodyHeight = Math.Max(48f, Math.Min(120f, bodyHeight));
            OptionsTop = 48f + BodyHeight;
            float y = OptionsTop;
            foreach (DialogOption option in node.options)
            {
                options.Add(option, y + OptionHeight / 2f);
                y += OptionHeight;
            }
            Height = y + 6f;
        }

        public CQFDialogNodeLayout(DialogOption option, float bodyHeight)
        {
            BodyHeight = Math.Max(48f, Math.Min(120f, bodyHeight));
            OptionsTop = 48f + BodyHeight;
            float y = OptionsTop;
            foreach (DialogResult result in option.results)
            {
                results.Add(result, y + ResultHeight / 2f);
                y += ResultHeight;
            }
            Height = OptionsTop + Math.Max(1, option.results.Count) * ResultHeight + 10f;
        }

        public float BodyHeight { get; }
        public float OptionsTop { get; }
        public float Height { get; }
        public IReadOnlyDictionary<DialogOption, float> Options => options;
        public IReadOnlyDictionary<DialogResult, float> Results => results;

        public const float Width = 290f;
        public const float HeaderHeight = 30f;
        public const float OptionHeight = 30f;
        public const float ResultHeight = 30f;
        private readonly Dictionary<DialogOption, float> options = new Dictionary<DialogOption, float>();
        private readonly Dictionary<DialogResult, float> results = new Dictionary<DialogResult, float>();
    }
}
