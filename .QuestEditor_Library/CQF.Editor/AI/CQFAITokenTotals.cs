namespace QuestEditor_Library
{
    public sealed class CQFAITokenTotals
    {
        public long Input { get; private set; }
        public long Output { get; private set; }
        public long Total { get; private set; }
        public int Reported { get; private set; }
        public int Unavailable { get; private set; }
        public void Add(CQFAITokenUsage? usage)
        {
            if (usage == null) { Unavailable++; return; }
            long input = checked(Input + usage.Input), output = checked(Output + usage.Output), total = checked(Total + usage.Total);
            Input = input; Output = output; Total = total; Reported++;
        }
        public void Clear() { Input = 0; Output = 0; Total = 0; Reported = 0; Unavailable = 0; }
    }
}
