using HarmonyLib;
using Verse;

namespace QuestEditor_Library
{
    public sealed class CQFEditorModule : ICQFEditorModule
    {
        public int ApiVersion => CQFEditorBridge.ApiVersion;
        public DutyMapDef CurrentEditingDutyMap => QuestEditor_DutyMap.CurrentEditingDutyMap;
        public CustomMapDataDef? CurrentEditingMap => Find.WindowStack.WindowOfType<QuestEditor_SaveMapToFile>() == null ? null : QuestEditor_SaveMapToFile.def;

        public void Initialize()
        {
            new Harmony("CQF_Editor").PatchAll(typeof(CQFEditorModule).Assembly);
            CQFEntityBookCatalog.Register();
            RegisterAI();
        }

        public void RegisterAI()
        {
            CQFAIBridge.Register(CQFAIWindow.Open, map => QuestEditor_CustomQuestMap.GenerateMap(map.size, map));
        }

        public void OpenSettings()
        {
            if (Find.WindowStack.TryGetWindow<CQFAISettingsWindow>(out CQFAISettingsWindow settings))
                Find.WindowStack.Notify_ManuallySetFocus(settings);
            else
                Find.WindowStack.Add(new CQFAISettingsWindow());
        }

        public Window CreateDrawableWindow(IDrawable drawable, QuestBookDef? questBook = null)
        {
            return new Dialog_EditIDrawable(drawable, questBook);
        }

        public Window CreateTabWindow(IDrawTabable drawable)
        {
            return new Dialog_EditIDrawTabable(drawable);
        }

        public Window CreateInteractionWindow(InteractionOperation operation, Thing? owner = null)
        {
            return new Dialog_InteractionOption(operation, owner);
        }

        public Window CreateDialogWindow(DialogTreeDef tree)
        {
            return new QuestEditor_Dialog
            {
                CurTree = tree
            };
        }

        public Window CreateActionCompWindow(ActionComp comp, Thing? owner = null)
        {
            return new QuestEditor_EditActionComp(comp, owner);
        }

        public Window CreatePawnMiscWindow(PawnSpawnData data)
        {
            return new QuestEditor_PawnDataMisc(data);
        }
    }
}
