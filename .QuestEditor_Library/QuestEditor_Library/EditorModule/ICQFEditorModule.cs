namespace QuestEditor_Library
{
    public interface ICQFEditorModule
    {
        int ApiVersion { get; }
        void Initialize();
        void OpenSettings();
    }
}
