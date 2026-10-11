using Verse.AI.Group;

namespace QuestEditor_Library
{
    public sealed class LordJob_MetaLord : MultLevelLordJobBase
    {
        public override StateGraph CreateGraph()
        {
            StateGraph graph = new StateGraph();
            graph.AddToil(new LordToil_MetaLord());
            return graph;
        }
    }
}
