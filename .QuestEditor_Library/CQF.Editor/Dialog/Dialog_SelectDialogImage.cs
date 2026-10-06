using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace QuestEditor_Library
{
    public class Dialog_SelectDialogImage : Window
    {
        public Dialog_SelectDialogImage(Action<string> selectAction, string currentPath = null)
        {
            this.selectAction = selectAction;
            this.currentPath = currentPath;
            this.forcePause = true;
            this.absorbInputAroundWindow = true;
            this.closeOnClickedOutside = false;
            this.doCloseX = true;
            this.images = LoadImages();
            this.modNames = this.images.Select(x => x.modName).Distinct().OrderBy(x => x).ToList();
            this.filteredImages = this.images;
        }

        public override Vector2 InitialSize => new Vector2(900f, 700f);

        public override void DoWindowContents(Rect inRect)
        {
            Text.Font = GameFont.Medium;
            Widgets.Label(new Rect(0f, 0f, inRect.width, 35f), "DialogImage_Title".Translate());
            Text.Font = GameFont.Small;

            Widgets.Label(new Rect(0f, 40f, 80f, 25f), "DialogImage_Mod".Translate());
            if (Widgets.ButtonText(new Rect(85f, 40f, 260f, 25f), this.selectedMod.NullOrEmpty() ? "DialogImage_All".Translate() : this.selectedMod, false))
            {
                List<FloatMenuOption> options = new List<FloatMenuOption>
                {
                    new FloatMenuOption("DialogImage_All".Translate(), () =>
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

            Widgets.Label(new Rect(360f, 40f, 60f, 25f), "DialogImage_Search".Translate());
            string text = Widgets.TextField(new Rect(425f, 40f, 300f, 25f), this.searchTerm);
            if (text != this.searchTerm)
            {
                this.searchTerm = text;
                this.UpdateFilteredImages();
            }

            float top = 75f;
            float viewHeight = Math.Max(620f, this.filteredImages.Count * 70f + 10f);
            Widgets.BeginScrollView(new Rect(0f, top, inRect.width, inRect.height - top), ref this.scrollPosition,
                new Rect(0f, 0f, inRect.width - 16f, viewHeight));

            float y = 0f;
            string lastMod = null;
            foreach (DialogImageResource image in this.filteredImages)
            {
                if (lastMod != image.modName)
                {
                    Widgets.Label(new Rect(0f, y, 500f, 25f), image.modName.Colorize(ColorLibrary.SkyBlue));
                    y += 30f;
                    lastMod = image.modName;
                }

                Rect rowRect = new Rect(0f, y, 840f, 60f);
                if (image.path == this.currentPath)
                {
                    Widgets.DrawHighlightSelected(rowRect);
                }

                Texture2D texture = ContentFinder<Texture2D>.Get(image.path, false);
                Rect imageRect = new Rect(0f, y, 60f, 60f);
                if (texture != null)
                {
                    Widgets.DrawTextureFitted(imageRect, texture, 1f);
                }
                if (Widgets.ButtonInvisible(rowRect))
                {
                    this.selectAction(image.path);
                    this.Close();
                }
                Widgets.Label(new Rect(70f, y, 760f, 25f), image.fileName);
                Widgets.Label(new Rect(70f, y + 28f, 760f, 25f), image.packageId);
                TooltipHandler.TipRegion(rowRect, image.modName + "\n" + image.packageId + "\n" + image.path);
                y += 65f;
            }

            Widgets.EndScrollView();
        }

        private List<DialogImageResource> LoadImages()
        {
            List<DialogImageResource> result = new List<DialogImageResource>();
            foreach (ModContentPack mod in LoadedModManager.RunningModsListForReading)
            {
                string dir = Path.Combine(mod.RootDir, "Textures", "UI", "CQF", "DialogImage");
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
                    string withoutExtension = Path.Combine("UI", "CQF", "DialogImage", Path.ChangeExtension(relativePath, null) ?? string.Empty)
                        .Replace("\\", "/");
                    result.Add(new DialogImageResource(mod.Name, mod.PackageIdPlayerFacing, withoutExtension, Path.GetFileNameWithoutExtension(file)));
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
        private readonly List<DialogImageResource> images;
        private readonly List<string> modNames;
        private List<DialogImageResource> filteredImages;
        private string selectedMod = string.Empty;
        private string searchTerm = string.Empty;
        private Vector2 scrollPosition;
    }

    public class DialogImageResource
    {
        public DialogImageResource(string modName, string packageId, string path, string fileName)
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
