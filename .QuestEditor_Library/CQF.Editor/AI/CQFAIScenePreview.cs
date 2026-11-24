using System.Collections;
using System.Xml.Linq;
using UnityEngine;
using Verse;

namespace QuestEditor_Library
{
    internal static class CQFAIScenePreview
    {
        public static XElement Run(string action, string name, int x, int z, int zoom, bool editing)
        {
            if (Current.Game == null) throw new InvalidDataException("CQF_AI_MissingResource: active game");
            if (name.Length is < 1 or > 150) throw new InvalidDataException("CQF_AI_InvalidTool: scene name");
            IEnumerable<Map> matches = Current.Game.Maps.Where(map => Matches(map, name)).OrderByDescending(map => map.uniqueID);
            if (action == "status")
                return new XElement("scenePreview", new XAttribute("name", name), matches.Take(8).Select(map =>
                    new XElement("map", new XAttribute("id", map.uniqueID), new XAttribute("width", map.Size.x),
                        new XAttribute("height", map.Size.z), new XAttribute("current", map == Find.CurrentMap))));
            if (action is not ("open" or "capture")) throw new InvalidDataException("CQF_AI_InvalidTool: scene preview action");
            if (!editing) throw new InvalidDataException("CQF_AI_ReadOnly");
            if (LongEventHandler.AnyEventNowOrWaiting) throw new InvalidDataException("CQF_AI_StaleTarget");
            if (action == "open")
            {
                CustomMapDataDef definition = DefDatabase<CustomMapDataDef>.GetNamedSilentFail(name)
                    ?? throw new InvalidDataException("CQF_AI_MissingResource: " + name);
                if (definition.isPart) throw new InvalidDataException("CQF_AI_InvalidValue: complete map definition required");
                CQFAIMapPlan.Validate(definition);
                Map? prior = matches.FirstOrDefault();
                QuestEditor_CustomQuestMap.GenerateMap(definition.size, definition, false);
                return new XElement("scenePreview", new XAttribute("name", name), new XAttribute("queued", true),
                    prior == null ? null : new XAttribute("previousMapId", prior.uniqueID));
            }

            Map? current = Find.CurrentMap;
            if (current == null || !Matches(current, name))
                throw new InvalidDataException("CQF_AI_MissingTarget: open the matching scene preview");
            int maxZoom = Math.Max(60, Math.Max(current.Size.x, current.Size.z));
            if (x < 0 || z < 0 || x >= current.Size.x || z >= current.Size.z || zoom < 10 || zoom > maxZoom)
                throw new InvalidDataException("CQF_AI_InvalidValue: preview camera");
            if (captureDriver != null) throw new InvalidDataException("CQF_AI_StaleTarget");
            string directory = Path.Combine(GenFilePaths.SaveDataFolderPath, "CQFScenePreviews");
            Directory.CreateDirectory(directory);
            string path = Path.Combine(directory, "CQF_" + Guid.NewGuid().ToString("N") + ".png");
            CameraDriver driver = Find.CameraDriver;
            Vector3 previousPosition = current.rememberedCameraPos.rootPos;
            float previousSize = driver.RootSize;
            driver.SetRootPosAndSize(new Vector3(x + 0.5f, 0f, z + 0.5f), zoom);
            Camera camera = driver.GetComponent<Camera>();
            if (camera.transform.position.y > camera.farClipPlane - 5f)
            {
                IEnumerable<Camera> subcameras = Current.SubcameraDriver == null
                    ? Array.Empty<Camera>() : Current.SubcameraDriver.GetComponentsInChildren<Camera>(true);
                var previousClips = subcameras.Append(camera).Distinct().Select(value => (Camera: value, Far: value.farClipPlane)).ToArray();
                foreach (var entry in previousClips)
                    entry.Camera.farClipPlane = Mathf.Max(entry.Far, entry.Camera.transform.position.y + 20f);
                captureDriver = driver;
                driver.StartCoroutine(RestoreCaptureCamera(driver, current, previousPosition, previousSize, previousClips));
            }
            ScreenCapture.CaptureScreenshot(path);
            return new XElement("sceneCapture", new XAttribute("mapId", current.uniqueID), new XAttribute("queued", true), new XElement("path", path));
        }

        private static bool Matches(Map map, string name) => map.Parent is EditorMapObject parent && parent.mapDef?.defName == name;

        private static IEnumerator RestoreCaptureCamera(CameraDriver driver, Map map, Vector3 position, float size,
            (Camera Camera, float Far)[] previousClips)
        {
            yield return null;
            yield return null;
            foreach (var entry in previousClips)
                if (entry.Camera != null) entry.Camera.farClipPlane = entry.Far;
            if (Find.CurrentMap == map) driver.SetRootPosAndSize(position, size);
            captureDriver = null;
        }

        private static CameraDriver? captureDriver;
    }
}
