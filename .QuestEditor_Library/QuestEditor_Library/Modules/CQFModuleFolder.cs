using Verse;

namespace QuestEditor_Library;

internal sealed class CQFModuleFolder
{
    public CQFModuleFolder(ModContentPack content, string path, IReadOnlyList<string> modules, bool provider)
    {
        Content = content;
        Path = path;
        Modules = modules;
        Provider = provider;
    }

    public ModContentPack Content { get; }
    public string Path { get; }
    public IReadOnlyList<string> Modules { get; }
    public bool Provider { get; }
}
