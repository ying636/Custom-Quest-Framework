using Verse;

namespace QuestEditor_Library
{
    [StaticConstructorOnStartup]
    public static class Root
    {
        static Root()
        {
            LongEventHandler.ExecuteWhenFinished(CQFSignalBook.LoadDefault);
            LongEventHandler.ExecuteWhenFinished(CQFTargetKeyBook.LoadDefault);
        }
    }
}
