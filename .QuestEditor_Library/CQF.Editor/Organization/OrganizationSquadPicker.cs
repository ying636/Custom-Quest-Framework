using RimWorld;
using UnityEngine;
using Verse;

namespace QuestEditor_Library;

internal sealed class OrganizationSquadPicker
{
    internal OrganizationSquadPicker(MetaLord? owner)
    {
        this.organization = owner;
    }

    internal void Draw(ref float y, Rect rect, MetaLordSquadSelector value, string label, bool allowTag = true)
    {
        Widgets.Label(new Rect(0, y, rect.width - 174, 28), label.Colorize(CQFUIStyle.Accent));
        if (this.organization is { Disposed: false } owner && CQFUIStyle.ButtonText(
            new Rect(rect.width - 164, y, 164, 27), "CQF_Organization_PickSquad".Translate()))
        {
            var options = new List<FloatMenuOption>();
            foreach (LevelLord level in owner.Levels.Where(level => level.level is { Disposed: false }))
            {
                Map map = level.level;
#if RIMWORLD_1_7
                var instance = GameComponent_MapManager.Instance?.GetCompositeMap(map);
                string? region = instance?.GetGroup(map) != null ? instance.RegionId(map) : null;
#else
                string? region = null;
#endif
                CustomMapDataDef? template = (map.Parent as MapParent_Custom)?.mapDataDef ?? (map.Parent as CustomSite)?.mapDef;
                foreach (var lord in level.Lords)
                {
                    if (lord?.LordJob is not MultLevelLordJobBase job) continue;
                    string mapLabel = region ?? template?.Origin.defName ?? map.Parent.LabelCap.ToString();
                    options.Add(new FloatMenuOption("CQF_Organization_SquadOption".Translate(job.groupName, mapLabel), () =>
                    {
                        value.name = job.groupName;
                        value.tag = null;
                        value.mapLookup = region.NullOrEmpty() ? CQFCompositeMapLookup.MapTemplate : CQFCompositeMapLookup.RegionId;
                        value.mapKey = region ?? template?.Origin.defName;
                    }));
                }
            }
            if (options.Count > 0) Find.WindowStack.Add(new FloatMenu(options));
            else Messages.Message("CQF_Organization_NoSquads".Translate(), MessageTypeDefOf.RejectInput);
        }
        y += 34;
        float labelWidth = Mathf.Min(144, rect.width * 0.40f);
        Widgets.Label(new Rect(0, y, labelWidth, 27), "LordName".Translate());
        value.name = Widgets.TextField(new Rect(labelWidth, y, rect.width - labelWidth, 27), value.name ?? string.Empty);
        y += 30;
        if (allowTag)
        {
            Widgets.Label(new Rect(0, y, labelWidth, 27), "CQF_Organization_SquadTag".Translate());
            value.tag = Widgets.TextField(new Rect(labelWidth, y, rect.width - labelWidth, 27), value.tag ?? string.Empty);
            y += 30;
        }
        Widgets.Label(new Rect(0, y, labelWidth, 27), "CQF_Organization_MapScope".Translate());
#if RIMWORLD_1_7
        if (CQFUIStyle.ButtonText(new Rect(labelWidth, y, rect.width - labelWidth, 27),
            ("CQF_Organization_Lookup_" + value.mapLookup).Translate()))
            CQFEditorTools.DrawFloatMenu(Enum.GetValues(typeof(CQFCompositeMapLookup)).Cast<CQFCompositeMapLookup>().ToList(), mode => value.mapLookup = mode,
                mode => ("CQF_Organization_Lookup_" + mode).Translate());
#else
        Widgets.Label(new Rect(labelWidth, y, rect.width - labelWidth, 27), "CQF_Organization_Lookup_MapTemplate".Translate());
#endif
        y += 30;
        Widgets.Label(new Rect(0, y, labelWidth, 27), "CQF_Organization_MapKey".Translate());
        value.mapKey = Widgets.TextField(new Rect(labelWidth, y, rect.width - labelWidth, 27), value.mapKey ?? string.Empty);
        y += 36;
    }

    private readonly MetaLord? organization;
}
