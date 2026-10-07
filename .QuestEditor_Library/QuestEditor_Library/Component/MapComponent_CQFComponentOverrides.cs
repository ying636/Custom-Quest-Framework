using Verse;

namespace QuestEditor_Library
{
    public sealed class MapComponent_CQFComponentOverrides : MapComponent
    {
        public MapComponent_CQFComponentOverrides(Map map) : base(map) { }
        public CQFComponentOverride? Get(ThingWithComps thing, int index) => entries?.FirstOrDefault(entry => entry.thing == thing && entry.componentIndex == index);
        public void Set(ThingWithComps thing, int index, CQFComponentOverride? value)
        {
            entries ??= new List<CQFComponentOverride>();
            entries.RemoveAll(entry => entry.thing == thing && entry.componentIndex == index);
            if (value != null) entries.Add(value);
        }
        public override void FinalizeInit()
        {
            base.FinalizeInit();
            foreach (CQFComponentOverride entry in entries ?? new List<CQFComponentOverride>())
                if (!entry.Apply()) Log.Error("CQF_ComponentOverrideInvalid: " + entry.thing?.ThingID + ":" + entry.componentIndex + ":" + entry.componentType);
        }
        public override void ExposeData()
        {
            base.ExposeData();
            if (Scribe.mode == LoadSaveMode.Saving && entries != null)
            {
                entries.RemoveAll(entry => entry == null || entry.thing == null || entry.thing.Destroyed);
                for (int index = 0; index < entries.Count; index++)
                {
                    CQFComponentOverride entry = entries[index];
                    if (entry.componentIndex >= 0 && entry.componentIndex < entry.thing!.AllComps.Count && entry.thing.AllComps[entry.componentIndex].GetType().FullName == entry.componentType)
                        entries[index] = new CQFComponentOverride(entry.thing.AllComps[entry.componentIndex], entry.componentIndex);
                    else Log.Error("CQF_ComponentOverrideInvalid: " + entry.thing.ThingID + ":" + entry.componentIndex + ":" + entry.componentType);
                }
            }
            Scribe_Collections.Look(ref entries, "CQF_componentOverrides", LookMode.Deep);
        }
        private List<CQFComponentOverride>? entries;
    }
}
