#if RIMWORLD_1_7
using UnityEngine;
using Verse;

namespace QuestEditor_Library;

public sealed class CQFBackgroundStructureDef : Def
{
    public override IEnumerable<string> ConfigErrors()
    {
        foreach (string error in base.ConfigErrors()) yield return error;
        if (string.IsNullOrWhiteSpace(texturePath)) yield return "Missing texturePath.";
        if (size.x <= 0 || size.y <= 0 || height < 0) yield return "Invalid structure dimensions.";
        if (!float.IsFinite(wallLightRadius) || wallLightRadius < 0f) yield return "Invalid wallLightRadius.";
    }

    public CQFBackgroundStructureCategoryDef? category;
    public string texturePath = string.Empty;
    public Vector2 size = new Vector2(4f, 4f);
    public float height = 8f;
    public Color color = Color.white;
    public float wallLightRadius;
}
#endif
