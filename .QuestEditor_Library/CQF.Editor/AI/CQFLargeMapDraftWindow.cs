using System.Xml.Linq;
using UnityEngine;
using Verse;

namespace QuestEditor_Library
{
    public sealed class CQFLargeMapDraftWindow : Window
    {
        public CQFLargeMapDraftWindow(Map map)
        {
            Draft = drafts.GetValue(map, value => new CQFLargeMapDraft(value));
            draggable = true;
            resizeable = true;
            doCloseX = true;
            closeOnAccept = false;
            absorbInputAroundWindow = false;
            preventCameraMotion = false;
            forcePause = true;
        }

        public CQFLargeMapDraft Draft { get; }
        public override Vector2 InitialSize => new Vector2(Mathf.Min(UI.screenWidth - 50f, 760f), Mathf.Min(UI.screenHeight - 50f, 740f));

        public static XElement Status(Map map) => drafts.TryGetValue(map, out CQFLargeMapDraft? draft)
            ? draft.Summary() : new XElement("mapDraft", new XAttribute("exists", false));

        public static CQFLargeMapDraftWindow Open(Map map)
        {
            CQFLargeMapDraftWindow? window = Find.WindowStack.Windows.OfType<CQFLargeMapDraftWindow>().FirstOrDefault(item => item.Draft.Map == map);
            if (window != null)
            {
                Find.WindowStack.Notify_ManuallySetFocus(window);
                return window;
            }
            window = new CQFLargeMapDraftWindow(map);
            Find.WindowStack.Add(window);
            return window;
        }

        public override void WindowUpdate()
        {
            base.WindowUpdate();
            if (!Draft.IsValid && !Draft.Busy)
            {
                Close();
                return;
            }
            if (!Draft.Busy) return;
            Draft.Advance();
            if (!Draft.Busy && Draft.Error != null) Report(Draft.Error);
        }

        public override void DoWindowContents(Rect inRect)
        {
            using CQFUIScope scope = new CQFUIScope(inRect.width, inRect.height);
            Widgets.Label(new Rect(0f, 0f, inRect.width - 24f, 28f), "CQF_AI_LargeDraft".Translate());
            float y = 34f;
            Widgets.Label(new Rect(0f, y, inRect.width, 24f), "CQF_AI_LargeDraftCounts".Translate(Draft.Terrain.Count, Draft.Roofs.Count, Draft.Placements.Count));
            y += 28f;
            Rect preview = new Rect(0f, y, inRect.width, Mathf.Max(80f, inRect.height - y - 125f));
            if (previewRevision != Draft.Revision || texture == null) BuildPreview();
            GUI.DrawTexture(preview, texture, ScaleMode.ScaleToFit, false);
            y = preview.yMax + 5f;
            if (Draft.Busy)
                Widgets.Label(new Rect(0f, y, inRect.width, 26f), Draft.Phase.Translate().ToString() + " " + Draft.Completed + "/" + Draft.Total);
            else if (Draft.Error != null)
                Widgets.Label(new Rect(0f, y, inRect.width, 26f), ErrorText(Draft.Error));
            else if (Draft.PlacementConflicts.Count > 0 || roadBlocked)
                Widgets.Label(new Rect(0f, y, inRect.width, 26f), "CQF_AI_LargeDraftExportConflict".Translate());

            y += 30f;
            bool enabled = GUI.enabled;
            GUI.enabled = enabled && !Draft.Busy;
            float width = (inRect.width - 18f) / 4f;
            if (CQFAIIconButton.DrawText(new Rect(0f, y, width, 32f), (Draft.CanUndo ? "CQF_AI_LargeDraftNew" : "CQF_AI_LargeDraftClear").Translate()))
                if (Draft.CanUndo)
                    Find.WindowStack.Add(Dialog_MessageBox.CreateConfirmation("CQF_AI_LargeDraftNewConfirm".Translate(),
                        () => Attempt(Draft.StartNew)));
                else Attempt(Draft.Clear);
            GUI.enabled = enabled && !Draft.Busy && !Draft.CanUndo && (Draft.Terrain.Count + Draft.Roofs.Count + Draft.Placements.Count > 0);
            if (CQFAIIconButton.DrawText(new Rect(width + 6f, y, width, 32f), "CQF_AI_LargeDraftApply".Translate()))
            {
                int revision = Draft.Revision;
                Find.WindowStack.Add(Dialog_MessageBox.CreateConfirmation("CQF_AI_LargeDraftApplyConfirm".Translate(), () => Attempt(() =>
                {
                    if (CustomQuestFramework_ModSetting.setting.dialogAIAllowEditing) Draft.BeginApply(revision);
                    else throw new InvalidOperationException("CQF_AI_ReadOnly");
                })));
            }
            GUI.enabled = enabled && !Draft.Busy && Draft.CanUndo;
            if (CQFAIIconButton.DrawText(new Rect((width + 6f) * 2f, y, width, 32f), "CQF_DialogGraph_Undo".Translate()))
                Attempt(Draft.BeginUndo);
            GUI.enabled = enabled && !Draft.Busy;
            if (CQFAIIconButton.DrawText(new Rect((width + 6f) * 3f, y, width, 32f), "CQF_AI_LargeDraftExport".Translate()))
            {
                string name = exportName;
                LongEventHandler.QueueLongEvent(() => Attempt(() =>
                    Messages.Message("SaveSucceed".Translate(Draft.Export(name)), RimWorld.MessageTypeDefOf.PositiveEvent)),
                    "SaveToFile", false, Report);
            }
            y += 38f;
            var nameField = new Rect(0, y, inRect.width, 30f);
            exportName = Widgets.TextField(nameField, exportName);
            TooltipHandler.TipRegion(nameField, "CQF_AI_LargeDraftExportName".Translate());
            GUI.enabled = enabled;
        }

        public override void Close(bool doCloseSound = true)
        {
            if (Draft.Busy) return;
            base.Close(doCloseSound);
        }

        public override void PostClose()
        {
            if (texture != null) UnityEngine.Object.Destroy(texture);
            base.PostClose();
        }

        private void BuildPreview()
        {
            var map = Draft.Map;
            if (texture == null)
                texture = new Texture2D(map.Size.x, map.Size.z, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point };
            Color32[] pixels = new Color32[map.Size.x * map.Size.z];
            var changes = Draft.Terrain;
            foreach (IntVec3 cell in CellRect.WholeMap(map))
            {
                bool changed = changes.TryGetValue(cell, out TerrainDef def);
                def ??= map.terrainGrid.TopTerrainAt(cell);
                Color color = def.DrawColor;
                if (changed) color = Color.Lerp(color, def.IsWater ? new Color(0.15f, 0.5f, 0.9f) : new Color(0.45f, 0.85f, 0.48f), 0.42f);
                else if (cell.GetEdifice(map) != null) color = new Color(0.35f, 0.37f, 0.41f);
                pixels[cell.z * map.Size.x + cell.x] = color;
            }
            foreach (ThingData placement in Draft.Placements)
                foreach (IntVec3 cell in GenAdj.OccupiedRect(placement.position, placement.rotation, placement.def.size))
                    pixels[cell.z * map.Size.x + cell.x] = new Color(0.94f, 0.77f, 0.34f);
            foreach (IntVec3 cell in Draft.Roofs.Keys)
                pixels[cell.z * map.Size.x + cell.x] = new Color(0.5f, 0.67f, 0.86f);
            var blocked = Draft.BlockedRoad;
            roadBlocked = blocked.Count > 0;
            foreach (IntVec3 cell in Draft.PlacementConflicts.Concat(blocked))
                pixels[cell.z * map.Size.x + cell.x] = new Color(0.96f, 0.28f, 0.24f);
            texture.SetPixels32(pixels);
            texture.Apply(false);
            previewRevision = Draft.Revision;
        }

        private static void Attempt(Action action)
        {
            try { action(); }
            catch (Exception error) { Report(error); }
        }

        private static void Report(Exception error)
        {
            Log.Warning("CQF map draft: " + error);
            Messages.Message(ErrorText(error), RimWorld.MessageTypeDefOf.RejectInput);
        }

        private static string ErrorText(Exception error)
        {
            string key = error.Message.Split(':')[0];
            return key.CanTranslate() ? key.Translate().ToString() : error.Message;
        }

        private static readonly System.Runtime.CompilerServices.ConditionalWeakTable<Map, CQFLargeMapDraft> drafts = new System.Runtime.CompilerServices.ConditionalWeakTable<Map, CQFLargeMapDraft>();
        private Texture2D? texture;
        private int previewRevision = -1;
        private bool roadBlocked;
        private string exportName = "CQF_MapDraft";
    }
}
