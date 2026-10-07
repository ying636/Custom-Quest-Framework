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
        public System.Xml.Linq.XElement Save(string name) => new System.Xml.Linq.XElement(name, new System.Xml.Linq.XAttribute("input", Input), new System.Xml.Linq.XAttribute("output", Output),
            new System.Xml.Linq.XAttribute("total", Total), new System.Xml.Linq.XAttribute("reported", Reported), new System.Xml.Linq.XAttribute("unavailable", Unavailable));
        public void Restore(System.Xml.Linq.XElement value)
        {
            long input = (long)value.Attribute("input")!, output = (long)value.Attribute("output")!, total = (long)value.Attribute("total")!;
            int reported = (int)value.Attribute("reported")!, unavailable = (int)value.Attribute("unavailable")!;
            if (input < 0 || output < 0 || input > long.MaxValue - output || total != input + output || reported < 0 || unavailable < 0)
                throw new InvalidDataException("CQF_AI_InvalidHistory");
            Input = input; Output = output; Total = total; Reported = reported; Unavailable = unavailable;
        }
    }
}
