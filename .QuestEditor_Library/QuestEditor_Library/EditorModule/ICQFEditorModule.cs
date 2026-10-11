using Verse;

namespace QuestEditor_Library
{
    public interface ICQFEditorModule
    {
        int ApiVersion { get; }
        DutyMapDef CurrentEditingDutyMap { get; }
        CustomMapDataDef? CurrentEditingMap { get; }
        void Initialize();
        void OpenSettings();
        Window CreateDrawableWindow(IDrawable drawable, QuestBookDef? questBook = null);
        Window CreateTabWindow(IDrawTabable drawable);
        Window CreateInteractionWindow(InteractionOperation operation, Thing? owner = null);
        Window CreateDialogWindow(DialogTreeDef tree);
        Window CreateActionCompWindow(ActionComp comp, Thing? owner = null);
        Window CreatePawnMiscWindow(PawnSpawnData data);
    }
}
