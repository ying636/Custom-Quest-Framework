using Verse;

namespace QuestEditor_Library;

internal sealed class CQFModuleFolder
{
    public CQFModuleFolder(ModContentPack content, string path, string module, bool provider)
    {
        Content = content;
        Path = path;
        Module = module;
        Provider = provider;
    }

    public ModContentPack Content { get; }
    public string Path { get; }
    public string Module { get; }
    public bool Provider { get; }
}
