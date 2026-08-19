using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace QuestEditor_Library
{
    public class Designator_Blueprint : Designator
    {
        public Designator_Blueprint()
        {
            this.defaultLabel = "CQF_BlueprintDesignatorEmpty".Translate();
            this.defaultDesc = "CQF_BlueprintDesignatorDesc".Translate();
            this.icon = TexButton.Copy;
            this.useMouseIcon = true;
            this.selectedBlueprint = BlueprintRepository.AllBlueprints.FirstOrDefault();
            if (this.selectedBlueprint != null)
            {
                this.rotation = this.selectedBlueprint.rot.IsValid ? this.selectedBlueprint.rot : Rot4.North;
                this.icon = BlueprintRepository.GetIcon(this.selectedBlueprint);
            }
        }

        public CustomMapDataDef SelectedBlueprint => this.selectedBlueprint;

        public Rot4 CurrentRotation => this.rotation;

        public override bool Visible => DebugSettings.godMode;

        public override DrawStyleCategoryDef DrawStyleCategory => null;

        public override bool DragDrawMeasurements => false;

        public override string Label => this.selectedBlueprint == null
            ? this.defaultLabel
            : "CQF_BlueprintDesignator".Translate(this.selectedBlueprint.label ?? this.selectedBlueprint.defName);

        public override string Desc => this.defaultDesc;

        public override IEnumerable<FloatMenuOption> RightClickFloatMenuOptions
        {
            get
            {
                yield return new FloatMenuOption("CQF_OpenFloatingPalette".Translate(),
                    () => Find.WindowStack.Add(new Window_BlueprintPalette(this)));
                yield return new FloatMenuOption("CQF_ImportLoadedMaps".Translate(),
                    BlueprintRepository.ConfirmImportLoadedBlueprints);
            }
        }

        public override void ProcessInput(UnityEngine.Event ev)
        {
            this.HandleRotationInput(ev);
            if (this.selectedBlueprint == null)
            {
                Find.WindowStack.Add(new Window_BlueprintPalette(this));
                return;
            }
            base.ProcessInput(ev);
        }

        public void SelectBlueprint(CustomMapDataDef blueprint)
        {
            if (blueprint == null)
            {
                return;
            }
            this.selectedBlueprint = blueprint;
            this.rotation = blueprint.rot.IsValid ? blueprint.rot : Rot4.North;
            this.rotationSource = null;
            this.rotatedBlueprint = null;
            this.defaultLabel = "CQF_BlueprintDesignator".Translate(blueprint.label ?? blueprint.defName);
            this.icon = BlueprintRepository.GetIcon(blueprint);
            Find.DesignatorManager.Select(this);
        }

        public void ClearBlueprint()
        {
            this.selectedBlueprint = null;
            this.rotation = Rot4.North;
            this.rotationSource = null;
            this.rotatedBlueprint = null;
            this.defaultLabel = "CQF_BlueprintDesignatorEmpty".Translate();
            this.icon = TexButton.Copy;
        }

        public override void DesignateSingleCell(IntVec3 loc)
        {
            if (this.selectedBlueprint == null || !loc.InBounds(Find.CurrentMap))
            {
                return;
            }
            CustomMapDataDef placementBlueprint = this.GetPlacementBlueprint();
            if (!GetBlueprintRect(loc, placementBlueprint).FullyContainedWithin(CellRect.WholeMap(Find.CurrentMap)))
            {
                Messages.Message("CQF_BlueprintOutOfBounds".Translate(), MessageTypeDefOf.RejectInput);
                return;
            }
            List<Thing> generated = placementBlueprint.GenerateByCore(
                loc, Find.CurrentMap, null,
                true, true, false, true);
            if (generated == null)
            {
                Messages.Message("CQF_BlueprintPlacementFailed".Translate(this.selectedBlueprint.defName),
                    MessageTypeDefOf.RejectInput);
            }
        }

        public override void DesignateMultiCell(IEnumerable<IntVec3> cells)
        {
            List<IntVec3> selectedCells = cells.ToList();
            if (selectedCells.Count == 0)
            {
                return;
            }
            int minX = selectedCells.Min(cell => cell.x);
            int maxX = selectedCells.Max(cell => cell.x);
            int minZ = selectedCells.Min(cell => cell.z);
            int maxZ = selectedCells.Max(cell => cell.z);
            this.DesignateSingleCell(new IntVec3((minX + maxX) / 2, 0, (minZ + maxZ) / 2));
        }

        public override void SelectedUpdate()
        {
            base.SelectedUpdate();
            GenUI.RenderMouseoverBracket();
            if (this.selectedBlueprint == null || Find.CurrentMap == null)
            {
                return;
            }
            IntVec3 cell = UI.MouseCell();
            if (!cell.InBounds(Find.CurrentMap))
            {
                return;
            }
            CustomMapDataDef placementBlueprint = this.GetPlacementBlueprint();
            CellRect rect = GetBlueprintRect(cell, placementBlueprint);
            this.DrawBlueprintGhosts(cell, placementBlueprint,
                rect.FullyContainedWithin(CellRect.WholeMap(Find.CurrentMap)));
        }

        public override void SelectedProcessInput(UnityEngine.Event ev)
        {
            this.HandleRotationInput(ev);
            if (!KeyBindingDefOf.Designator_RotateLeft.KeyDownEvent
                && !KeyBindingDefOf.Designator_RotateRight.KeyDownEvent)
            {
                base.SelectedProcessInput(ev);
            }
        }

        public override AcceptanceReport CanDesignateCell(IntVec3 loc)
        {
            if (Find.CurrentMap == null)
            {
                return "CQF_NoCurrentMap".Translate();
            }
            if (this.selectedBlueprint == null)
            {
                return "CQF_NoBlueprintSelected".Translate();
            }
            if (!GetBlueprintRect(loc, this.GetPlacementBlueprint()).FullyContainedWithin(CellRect.WholeMap(Find.CurrentMap)))
            {
                return "CQF_BlueprintOutOfBounds".Translate();
            }
            return true;
        }

        public override void DrawMouseAttachments()
        {
            if (this.selectedBlueprint != null)
            {
                string label = "CQF_BlueprintMouseAttachment".Translate(
                    this.selectedBlueprint.label ?? this.selectedBlueprint.defName,
                    this.rotation.ToStringHuman());
                GenUI.DrawMouseAttachment(this.icon, label, this.iconAngle, this.iconOffset);
            }
        }

        private void RotateClockwise()
        {
            this.rotation = this.rotation.Rotated(RotationDirection.Clockwise);
            this.rotatedBlueprint = null;
        }

        private void RotateCounterclockwise()
        {
            this.rotation = this.rotation.Rotated(RotationDirection.Counterclockwise);
            this.rotatedBlueprint = null;
        }

        private void HandleRotationInput(UnityEngine.Event ev)
        {
            if (ev == null)
            {
                return;
            }
            bool rotateLeft = KeyBindingDefOf.Designator_RotateLeft.KeyDownEvent
                || (ev.type == EventType.KeyDown && ev.keyCode == KeyCode.Q);
            bool rotateRight = KeyBindingDefOf.Designator_RotateRight.KeyDownEvent
                || (ev.type == EventType.KeyDown && ev.keyCode == KeyCode.E);
            if (rotateLeft)
            {
                this.RotateCounterclockwise();
                if (ev.type == EventType.KeyDown)
                {
                    ev.Use();
                }
            }
            else if (rotateRight)
            {
                this.RotateClockwise();
                if (ev.type == EventType.KeyDown)
                {
                    ev.Use();
                }
            }
        }

        private void DrawBlueprintGhosts(IntVec3 center, CustomMapDataDef blueprint, bool valid)
        {
            if (blueprint == null)
            {
                return;
            }
            Color ghostColor = valid
                ? new Color(0.35f, 0.85f, 1f, 0.65f)
                : new Color(1f, 0.25f, 0.25f, 0.65f);
            IntVec3 origin = this.GetPreviewOrigin(center, blueprint);
            this.DrawTerrainGhosts(origin, blueprint, ghostColor);
            foreach (ThingData data in blueprint.thingDatas)
            {
                if (data?.def?.graphic == null)
                {
                    continue;
                }
                foreach (IntVec3 position in this.GetThingPositions(data))
                {
                    IntVec3 drawPosition = origin + position;
                    if (!Find.CameraDriver.CurrentViewRect.ExpandedBy(1).Contains(drawPosition))
                    {
                        continue;
                    }
                    this.DrawThingGhost(drawPosition, data.rotation, data.def,
                        data.style?.Graphic ?? data.def.graphic, data.stuff, ghostColor);
                }
            }
            foreach (CustomThingData data in blueprint.customThings)
            {
                this.DrawCustomThingGhost(origin, data, ghostColor);
            }
            foreach (CustomThingData data in blueprint.zoneCores)
            {
                this.DrawCustomThingGhost(origin, data, ghostColor);
            }
        }

        private void DrawCustomThingGhost(IntVec3 origin, CustomThingData data, Color ghostColor)
        {
            if (data?.def?.graphic == null)
            {
                return;
            }
            IntVec3 drawPosition = origin + data.position;
            if (!Find.CameraDriver.CurrentViewRect.ExpandedBy(1).Contains(drawPosition))
            {
                return;
            }
            this.DrawThingGhost(drawPosition, data.rotation, data.def,
                data.style?.Graphic ?? data.def.graphic, data.stuff, ghostColor);
        }

        private void DrawThingGhost(IntVec3 position, Rot4 rotation, ThingDef def,
            Graphic graphic, ThingDef stuff, Color ghostColor)
        {
            if (def == null || graphic == null)
            {
                return;
            }
            try
            {
                bool linkedGraphic = def.graphicData?.Linked == true
                    || (def.IsDoor && def.building?.isSupportDoor != true);
                bool ghostPathAvailable = def.useSameGraphicForGhost
                    || (linkedGraphic
                        ? !def.uiIconPath.NullOrEmpty()
                        : !graphic.path.NullOrEmpty());
                if (ghostPathAvailable)
                {
                    GhostDrawer.DrawGhostThing(position, rotation, def, graphic,
                        ghostColor, AltitudeLayer.Blueprint, null, false, stuff);
                    return;
                }
                Vector3 drawPosition = GenThing.TrueCenter(position, rotation, def.Size,
                    AltitudeLayer.Blueprint.AltitudeFor());
                graphic.DrawFromDef(drawPosition, rotation, def);
            }
            catch (Exception exception)
            {
                Log.ErrorOnce($"Draw blueprint ghost failed: Def={def.defName}, Error={exception}",
                    Gen.HashCombineInt(def.shortHash, 193741));
            }
        }

        private void DrawTerrainGhosts(IntVec3 origin, CustomMapDataDef blueprint, Color ghostColor)
        {
            foreach (KeyValuePair<string, List<IntVec3>> terrainData in blueprint.terrains)
            {
                TerrainDef terrain = DefDatabase<TerrainDef>.GetNamedSilentFail(terrainData.Key);
                foreach (IntVec3 position in terrainData.Value)
                {
                    this.DrawTerrainGhost(origin + position, terrain, ghostColor);
                }
            }
            foreach (KeyValuePair<string, List<CellRect>> terrainData in blueprint.terrainsRect)
            {
                TerrainDef terrain = DefDatabase<TerrainDef>.GetNamedSilentFail(terrainData.Key);
                CellRect viewRect = Find.CameraDriver.CurrentViewRect.ExpandedBy(1);
                foreach (CellRect rect in terrainData.Value)
                {
                    int minX = Math.Max(rect.minX + origin.x, viewRect.minX);
                    int maxX = Math.Min(rect.maxX + origin.x, viewRect.maxX);
                    int minZ = Math.Max(rect.minZ + origin.z, viewRect.minZ);
                    int maxZ = Math.Min(rect.maxZ + origin.z, viewRect.maxZ);
                    for (int x = minX; x <= maxX; x++)
                    {
                        for (int z = minZ; z <= maxZ; z++)
                        {
                            this.DrawTerrainGhost(new IntVec3(x, 0, z), terrain, ghostColor);
                        }
                    }
                }
            }
            foreach (KeyValuePair<ColorDef, List<CellRect>> colorData in blueprint.terrainsColorRect)
            {
                Color color = colorData.Key?.color ?? Color.white;
                foreach (CellRect rect in colorData.Value)
                {
                    foreach (IntVec3 position in rect.Cells)
                    {
                        this.DrawTerrainGhost(origin + position, null,
                            Color.Lerp(color, ghostColor, 0.35f));
                    }
                }
            }
        }

        private void DrawTerrainGhost(IntVec3 position, TerrainDef terrain, Color ghostColor)
        {
            if (!position.InBounds(Find.CurrentMap))
            {
                return;
            }
            Texture2D texture = terrain?.graphic?.MatSingle?.mainTexture as Texture2D ?? BaseContent.WhiteTex;
            Color materialColor = Color.Lerp(terrain?.DrawColor ?? Color.white, ghostColor, 0.25f);
            materialColor.a = 0.62f;
            Material material = MaterialPool.MatFrom(texture,
                ShaderDatabase.Transparent, materialColor, 2900);
            Matrix4x4 matrix = Matrix4x4.TRS(
                position.ToVector3ShiftedWithAltitude(AltitudeLayer.Blueprint), Quaternion.identity, Vector3.one);
            Graphics.DrawMesh(MeshPool.plane10, matrix, material, 0);
        }

        private IEnumerable<IntVec3> GetThingPositions(ThingData data)
        {
            if (!data.allPositions.NullOrEmpty())
            {
                foreach (IntVec3 position in data.allPositions)
                {
                    yield return position;
                }
                yield break;
            }
            if (!data.allRect.NullOrEmpty())
            {
                foreach (CellRect rect in data.allRect)
                {
                    foreach (IntVec3 position in rect.Cells)
                    {
                        yield return position;
                    }
                }
                yield break;
            }
            yield return data.position;
        }

        private CustomMapDataDef GetPlacementBlueprint()
        {
            Rot4 sourceRotation = this.selectedBlueprint?.rot.IsValid == true
                ? this.selectedBlueprint.rot
                : Rot4.North;
            if (this.selectedBlueprint == null || this.rotation == sourceRotation)
            {
                return this.selectedBlueprint;
            }
            if (this.rotatedBlueprint == null)
            {
                this.rotationSource = this.selectedBlueprint;
                if (!this.rotationSource.rot.IsValid)
                {
                    this.rotationSource = this.selectedBlueprint.Copy("_BlueprintRotationSource");
                    this.rotationSource.rot = Rot4.North;
                }
                this.rotatedBlueprint = this.rotationSource.GetRotated(this.rotation);
            }
            return this.rotatedBlueprint ?? this.selectedBlueprint;
        }

        private CellRect GetBlueprintRect(IntVec3 center, CustomMapDataDef blueprint)
        {
            if (blueprint == null)
            {
                return new CellRect(center.x, center.z, 1, 1);
            }
            List<IntVec3> positions = blueprint.GetAllPosition();
            if (!positions.NullOrEmpty())
            {
                int minX = positions.Min(position => position.x);
                int maxX = positions.Max(position => position.x);
                int minZ = positions.Min(position => position.z);
                int maxZ = positions.Max(position => position.z);
                return CellRect.FromLimits(center.x + minX, center.z + minZ,
                    center.x + maxX, center.z + maxZ);
            }
            if (blueprint.size.IsValid)
            {
                return new CellRect(center.x, center.z, blueprint.size.x, blueprint.size.z);
            }
            return new CellRect(center.x, center.z, 1, 1);
        }

        private IntVec3 GetPreviewOrigin(IntVec3 center, CustomMapDataDef blueprint)
        {
            // GenerateByCore 将传入位置作为蓝图局部坐标 0 点。
            return center;
        }

        private CustomMapDataDef selectedBlueprint;
        private CustomMapDataDef rotationSource;
        private CustomMapDataDef rotatedBlueprint;
        private Rot4 rotation = Rot4.North;
    }
}
