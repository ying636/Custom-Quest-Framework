using System.Reflection;
using QuestEditor_Library;
using Verse;

internal sealed class CheckEditorModule : ICQFEditorModule
{
    public CheckEditorModule(object editor)
    {
        this.editor = editor;
    }

    public int ApiVersion => CQFEditorBridge.ApiVersion;
    public DutyMapDef CurrentEditingDutyMap => ((ICQFEditorModule)this.editor).CurrentEditingDutyMap;
    public CustomMapDataDef? CurrentEditingMap => ((ICQFEditorModule)this.editor).CurrentEditingMap;

    public void Initialize()
    {
        this.editor.GetType().GetMethod("RegisterAI")!.Invoke(this.editor, null);
    }

    public void OpenSettings()
    {
        throw new NotSupportedException();
    }

    public Window CreateDrawableWindow(IDrawable drawable, QuestBookDef? questBook = null)
        => ((ICQFEditorModule)this.editor).CreateDrawableWindow(drawable, questBook);

    public Window CreateTabWindow(IDrawTabable drawable)
        => ((ICQFEditorModule)this.editor).CreateTabWindow(drawable);

    public Window CreateInteractionWindow(InteractionOperation operation, Thing? owner = null)
        => ((ICQFEditorModule)this.editor).CreateInteractionWindow(operation, owner);

    public Window CreateDialogWindow(DialogTreeDef tree)
        => ((ICQFEditorModule)this.editor).CreateDialogWindow(tree);

    public Window CreateActionCompWindow(ActionComp comp, Thing? owner = null)
        => ((ICQFEditorModule)this.editor).CreateActionCompWindow(comp, owner);

    public Window CreatePawnMiscWindow(PawnSpawnData data)
        => ((ICQFEditorModule)this.editor).CreatePawnMiscWindow(data);

    private readonly object editor;
}
