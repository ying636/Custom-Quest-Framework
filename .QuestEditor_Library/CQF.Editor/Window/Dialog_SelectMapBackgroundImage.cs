using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using Verse;

namespace QuestEditor_Library
{
    public class Dialog_SelectMapBackgroundImage : Window
    {
        public Dialog_SelectMapBackgroundImage(Action<string> selectAction, string currentPath = null)
        {
            this.selectAction = selectAction;
            this.currentPath = currentPath;
            this.forcePause = true;
            this.absorbInputAroundWindow = true;
            this.closeOnClickedOutside = false;
            this.doCloseX = true;
            this.images = this.LoadImages();
            this.modNames = this.images.Select(x => x.modName).Distinct().OrderBy(x => x).ToList();
            this.filteredImages = this.images;
        }

        public override Vector2 InitialSize => new Vector2(900f, 700f);

        public override void DoWindowContents(Rect inRect)
        {
            using CQFUIScope cqfUIScope = new CQFUIScope(inRect.width, inRect.height);
            Text.Font = GameFont.Medium;
            Widgets.Label(new Rect(0f, 0f, Mathf.Max(0f, inRect.width - 30f), 35f), "MapBackgroundImage_Title".Translate());
            Text.Font = GameFont.Small;

            Widgets.Label(new Rect(0f, 40f, 80f, 25f), "MapBackgroundImage_Mod".Translate());
            if (CQFUIStyle.ButtonText(new Rect(85f, 40f, Mathf.Max(40f, Mathf.Min(260f, inRect.width - 85f)), 25f), this.selectedMod.NullOrEmpty() ? "MapBackgroundImage_All".Translate() : this.selectedMod, false))
            {
                List<FloatMenuOption> options = new List<FloatMenuOption>
                {
                    new FloatMenuOption("MapBackgroundImage_All".Translate(), () =>
                    {
                        this.selectedMod = string.Empty;
                        this.UpdateFilteredImages();
                    })
                };
                this.modNames.ForEach(x => options.Add(new FloatMenuOption(x, () =>
                {
                    this.selectedMod = x;
                    this.UpdateFilteredImages();
                })));
                Find.WindowStack.Add(new FloatMenu(options));
            }

            float searchY = inRect.width < 760f ? 75f : 40f;
            float searchX = inRect.width < 760f ? 0f : 360f;
            Widgets.Label(new Rect(searchX, searchY, 60f, 25f), "MapBackgroundImage_Search".Translate());
            string text = Widgets.TextField(new Rect(searchX + 65f, searchY, Mathf.Max(40f, inRect.width - searchX - 65f), 25f), this.searchTerm);
            if (text != this.searchTerm)
            {
                this.searchTerm = text;
                this.UpdateFilteredImages();
            }

            float top = searchY + 35f;
            float contentWidth = inRect.width - 16f;
            float viewHeight = Mathf.Max(inRect.height - top, this.contentHeight);
            Widgets.BeginScrollView(new Rect(0f, top, inRect.width, inRect.height - top), ref this.scrollPosition,
                new Rect(0f, 0f, contentWidth, viewHeight));
            using CQFUIScope contentScope = new CQFUIScope(contentWidth);

            float y = 0f;
            string lastMod = null;
            foreach (MapBackgroundImageResource image in this.filteredImages)
            {
                if (lastMod != image.modName)
                {
                    Widgets.Label(new Rect(0f, y, contentWidth, 25f), image.modName.Colorize(CQFUIStyle.Accent));
                    y += 30f;
                    lastMod = image.modName;
                }

                Rect rowRect = new Rect(0f, y, contentWidth, 90f);
                if (image.path == this.currentPath)
                {
                    CQFUIStyle.DrawHighlightSelected(rowRect);
                }

                Texture2D texture = ContentFinder<Texture2D>.Get(image.path, false);
                Rect imageRect = new Rect(0f, y, 120f, 90f);
                if (texture != null)
                {
                    Widgets.DrawTextureFitted(imageRect, texture, 1f);
                }
                if (Widgets.ButtonInvisible(rowRect))
                {
                    this.selectAction(image.path);
                    this.Close();
                }
                Widgets.Label(new Rect(130f, y, contentWidth - 140f, 25f), image.fileName);
                Widgets.Label(new Rect(130f, y + 28f, contentWidth - 140f, 25f), image.packageId);
                Widgets.Label(new Rect(130f, y + 56f, contentWidth - 140f, 25f), image.path);
                TooltipHandler.TipRegion(rowRect, image.modName + "\n" + image.packageId + "\n" + image.path);
                y += 95f;
            }

            this.contentHeight = y + 10f;
            Widgets.EndScrollView();
        }

        private List<MapBackgroundImageResource> LoadImages()
        {
            List<MapBackgroundImageResource> result = new List<MapBackgroundImageResource>();
            foreach (ModContentPack mod in LoadedModManager.RunningModsListForReading)
            {
                string dir = Path.Combine(mod.RootDir, "Textures", "UI", "CQF", "MapBackgroup");
                if (!Directory.Exists(dir))
                {
                    continue;
                }
                foreach (string file in Directory.GetFiles(dir, "*.*", SearchOption.AllDirectories))
                {
                    string ext = Path.GetExtension(file).ToLowerInvariant();
                    if (ext != ".png" && ext != ".jpg" && ext != ".jpeg")
                    {
                        continue;
                    }
                    string relativePath = file.Substring(dir.Length).TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                    string withoutExtension = Path.Combine("UI", "CQF", "MapBackgroup", Path.ChangeExtension(relativePath, null) ?? string.Empty)
                        .Replace("\\", "/");
                    result.Add(new MapBackgroundImageResource(mod.Name, mod.PackageIdPlayerFacing, withoutExtension, Path.GetFileNameWithoutExtension(file)));
                }
            }
            return result.OrderBy(x => x.modName).ThenBy(x => x.path).ToList();
        }

        private void UpdateFilteredImages()
        {
            this.filteredImages = this.images.Where(x =>
                (this.selectedMod.NullOrEmpty() || x.modName == this.selectedMod) &&
                (this.searchTerm.NullOrEmpty() || x.path.IndexOf(this.searchTerm, StringComparison.OrdinalIgnoreCase) >= 0)).ToList();
        }

        private readonly Action<string> selectAction;
        private readonly string currentPath;
        private readonly List<MapBackgroundImageResource> images;
        private readonly List<string> modNames;
        private List<MapBackgroundImageResource> filteredImages;
        private string selectedMod = string.Empty;
        private string searchTerm = string.Empty;
        private Vector2 scrollPosition;
        private float contentHeight;
    }

    public class MapBackgroundImageResource
    {
        public MapBackgroundImageResource(string modName, string packageId, string path, string fileName)
        {
            this.modName = modName;
            this.packageId = packageId;
            this.path = path;
            this.fileName = fileName;
        }

        public string modName;
        public string packageId;
        public string path;
        public string fileName;
    }
}
