using QuestEditor_Library;
using Verse;

public sealed class EntityBookState : IExposable
{
    public EntityBookState()
    {
        Book = new GameComponent_EntityBook(Current.Game);
        _ = Book.GetRecords("spawn:CQF_CheckRecipe");
        _ = Book.Records;
    }

    public GameComponent_EntityBook Book { get; }

    public void ExposeData() => Book.ExposeData();
}
