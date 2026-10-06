using System.Reflection;
using QuestEditor_Library;

internal sealed class CheckEditorModule : ICQFEditorModule
{
    public CheckEditorModule(object editor)
    {
        this.editor = editor;
    }

    public int ApiVersion => CQFEditorBridge.ApiVersion;

    public void Initialize()
    {
        this.editor.GetType().GetMethod("RegisterDrawers")!.Invoke(this.editor, null);
        this.editor.GetType().GetMethod("RegisterAI")!.Invoke(this.editor, null);
        CQFEditorBridge.Register("CQF_Check_Ref", typeof(ModuleCheckDrawers), nameof(ModuleCheckDrawers.RefValue));
        CQFEditorBridge.Register("CQF_Check_Generic", typeof(ModuleCheckDrawers), nameof(ModuleCheckDrawers.GenericValue));
        CQFEditorBridge.Register("CQF_Check_Error", typeof(ModuleCheckDrawers), nameof(ModuleCheckDrawers.FailingValue));
    }

    public void OpenSettings()
    {
        throw new NotSupportedException();
    }

    private readonly object editor;
}
