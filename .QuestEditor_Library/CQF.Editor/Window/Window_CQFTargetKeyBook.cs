using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace QuestEditor_Library
{
    public sealed class Window_CQFTargetKeyBook : Window
    {
        public Window_CQFTargetKeyBook()
        {
            this.doCloseX = true;
            this.draggable = true;
            this.closeOnAccept = false;
            this.forcePause = true;
            this.absorbInputAroundWindow = true;
            this.RefreshDrafts();
        }

        public override Vector2 InitialSize => new Vector2(400f, 320f);

        protected override float Margin => 8f;

        public override void DoWindowContents(Rect inRect)
        {
            using CQFUIScope cqfUIScope = new CQFUIScope(inRect.width, inRect.height);
            if (this.revision != CQFTargetKeyBook.Revision)
            {
                this.RefreshDrafts();
            }
            Text.Font = GameFont.Small;
            Widgets.Label(new Rect(0f, 0f, inRect.width - 148f, 24f), "CQF_TargetKeyBook_Title".Translate() + " (" + this.drafts.Count + ")");
            float toolbarX = inRect.width - 138f;
            if (CQFUIStyle.ButtonImage(new Rect(toolbarX, 1f, 22f, 22f), TexButton.NewFile,
                tooltip: "CQF_TargetKeyBook_NewConfiguration".Translate()))
            {
                this.OpenConfigurationName(true);
            }
            if (CQFUIStyle.ButtonImage(new Rect(toolbarX + 28f, 1f, 22f, 22f), TexButton.Save,
                tooltip: "CQF_TargetKeyBook_SaveConfiguration".Translate()))
            {
                if (CQFTargetKeyBook.CurrentConfigurationName.Length == 0)
                {
                    this.OpenConfigurationName(false);
                }
                else
                {
                    this.Execute(() => this.SaveConfiguration(CQFTargetKeyBook.CurrentConfigurationName));
                }
            }
            if (CQFUIStyle.ButtonImage(new Rect(toolbarX + 56f, 1f, 22f, 22f), TexButton.Reload,
                tooltip: "CQF_TargetKeyBook_LoadConfiguration".Translate()))
            {
                this.ShowConfigurations(this.LoadConfiguration);
            }
            string defaultName = CQFTargetKeyBook.DefaultConfigurationName.Length > 0
                ? CQFTargetKeyBook.DefaultConfigurationName : "CQF_TargetKeyBook_NoDefaultConfiguration".Translate().ToString();
            if (CQFUIStyle.ButtonImage(new Rect(toolbarX + 84f, 1f, 22f, 22f), TexButton.AutoHomeArea,
                tooltip: "CQF_TargetKeyBook_SetDefaultConfiguration".Translate() + "\n"
                    + "CQF_TargetKeyBook_DefaultConfiguration".Translate() + ": " + defaultName))
            {
                this.ShowConfigurations(CQFTargetKeyBook.SetDefault, true);
            }
            string currentName = CQFTargetKeyBook.CurrentConfigurationName.Length > 0
                ? CQFTargetKeyBook.CurrentConfigurationName : "CQF_TargetKeyBook_UnsavedConfiguration".Translate().ToString();
            string configurationText = "CQF_TargetKeyBook_CurrentConfiguration".Translate() + ": " + currentName;
            if (CQFTargetKeyBook.CurrentConfigurationName.Length > 0 && CQFTargetKeyBook.CurrentConfigurationName == CQFTargetKeyBook.DefaultConfigurationName)
            {
                configurationText += " " + "CQF_TargetKeyBook_DefaultIndicator".Translate();
            }
            if (CQFTargetKeyBook.Modified || !this.drafts.SequenceEqual(CQFTargetKeyBook.Keys))
            {
                configurationText += " *";
            }
            Rect configurationRect = new Rect(0f, 30f, inRect.width - 30f, 24f);
            if (CQFUIStyle.ButtonText(configurationRect, configurationText.Truncate(configurationRect.width), drawBackground: false, overrideTextAnchor: TextAnchor.MiddleLeft))
            {
                this.ShowConfigurations(this.LoadConfiguration);
            }
            TooltipHandler.TipRegion(configurationRect, configurationText + "\n" + "CQF_TargetKeyBook_SelectConfiguration".Translate());
            float errorHeight = this.validationError == null ? 0f : Text.CalcHeight(
                this.validationError.Message.CanTranslate() ? this.validationError.Message.Translate().ToString() : this.validationError.Message,
                inRect.width) + 6f;
            Rect outRect = new Rect(0f, 62f, inRect.width, inRect.height - 66f - errorHeight);
            if (CQFUIStyle.ButtonImage(new Rect(inRect.width - 22f, 30f, 22f, 22f), TexButton.Plus,
                tooltip: "CQF_TargetKeyBook_AddKey".Translate()))
            {
                this.drafts.Add(string.Empty);
                this.focusedEntry = this.drafts.Count - 1;
                this.scroll.y = Mathf.Max(0f, this.drafts.Count * 28f - outRect.height);
            }
            Rect viewRect = new Rect(0f, 0f, outRect.width - 20f, Mathf.Max(outRect.height, this.drafts.Count * 28f));
            Widgets.BeginScrollView(outRect, ref this.scroll, viewRect);
            using CQFUIScope cqfContentScope1 = new CQFUIScope(viewRect.width);
            bool changed = false;
            for (int index = 0; index < this.drafts.Count; index++)
            {
                float y = index * 28f;
                Widgets.Label(new Rect(0f, y + 2f, 24f, 24f), (index + 1).ToString());
                GUI.SetNextControlName("CQF_TargetKeyBook_Entry_" + index);
                string value = Widgets.TextField(new Rect(28f, y, viewRect.width - 60f, 24f), this.drafts[index]);
                if (value != this.drafts[index])
                {
                    this.drafts[index] = value;
                    changed = true;
                }
                if (CQFUIStyle.ButtonImage(new Rect(viewRect.width - 22f, y + 1f, 20f, 20f), TexButton.Delete, tooltip: "Remove".Translate()))
                {
                    this.drafts.RemoveAt(index);
                    changed = true;
                    break;
                }
            }
            if (this.focusedEntry >= 0)
            {
                UI.FocusControl("CQF_TargetKeyBook_Entry_" + this.focusedEntry, this);
                this.focusedEntry = -1;
            }
            Widgets.EndScrollView();
            if (changed)
            {
                this.SynchronizeDrafts();
            }
            if (this.validationError != null)
            {
                string reason = this.validationError.Message.CanTranslate() ? this.validationError.Message.Translate().ToString() : this.validationError.Message;
                float height = Text.CalcHeight(reason, inRect.width);
                Widgets.Label(new Rect(0f, inRect.height - height - 2f, inRect.width, height), reason.Colorize(Color.red));
            }
        }

        public override void Close(bool doCloseSound = true)
        {
            if (this.validationError != null)
            {
                CQFTargetKeyBook.ReportError(this.validationError);
            }
            base.Close(doCloseSound);
        }

        private void SynchronizeDrafts()
        {
            try
            {
                CQFTargetKeyBook.Replace(this.drafts.Where(value => !string.IsNullOrWhiteSpace(value)));
                this.revision = CQFTargetKeyBook.Revision;
                this.validationError = null;
            }
            catch (ArgumentException exception)
            {
                this.validationError = exception;
            }
            catch (Exception exception)
            {
                this.validationError = exception;
                CQFTargetKeyBook.ReportError(exception);
            }
        }

        private void OpenConfigurationName(bool createEmpty)
        {
            Find.WindowStack.Add(new Dialog_CQFTargetKeyBookConfiguration(
                createEmpty ? "CQF_TargetKeyBook_NewConfiguration" : "CQF_TargetKeyBook_SaveConfiguration", name =>
                {
                    if (CQFTargetKeyBook.Store.ConfigurationNames.Contains(name, StringComparer.OrdinalIgnoreCase))
                    {
                        throw new ArgumentException("CQF_TargetKeyBook_ConfigurationExists");
                    }
                    if (createEmpty)
                    {
                        CQFTargetKeyBook.Store.Save(name, Array.Empty<string>());
                        this.LoadConfiguration(name);
                    }
                    else
                    {
                        this.SaveConfiguration(name);
                    }
                }));
        }

        private void SaveConfiguration(string name)
        {
            CQFTargetKeyBook.Replace(this.drafts.Where(value => !string.IsNullOrWhiteSpace(value)));
            CQFTargetKeyBook.Save(name);
            this.RefreshDrafts();
        }

        private void LoadConfiguration(string name)
        {
            CQFTargetKeyBook.Load(name);
            this.RefreshDrafts();
            this.scroll = Vector2.zero;
        }

        private void ShowConfigurations(Action<string> selected, bool selectDefault = false)
        {
            this.Execute(() =>
            {
                List<FloatMenuOption> options = CQFTargetKeyBook.Store.ConfigurationNames
                    .Select(name => new FloatMenuOption(name, () => this.Execute(() => selected(name)))).ToList();
                if (options.Count == 0)
                {
                    options.Add(new FloatMenuOption("CQF_TargetKeyBook_NoConfigurations".Translate(), null));
                }
                if (selectDefault && CQFTargetKeyBook.DefaultConfigurationName.Length > 0)
                {
                    options.Add(new FloatMenuOption("CQF_TargetKeyBook_ClearDefaultConfiguration".Translate(),
                        () => this.Execute(() => CQFTargetKeyBook.SetDefault(string.Empty))));
                }
                Find.WindowStack.Add(new FloatMenu(options));
            });
        }

        private void Execute(Action action)
        {
            try
            {
                action();
            }
            catch (Exception exception)
            {
                CQFTargetKeyBook.ReportError(exception);
            }
        }

        private void RefreshDrafts()
        {
            this.drafts = CQFTargetKeyBook.Keys.ToList();
            this.revision = CQFTargetKeyBook.Revision;
            this.validationError = null;
            this.focusedEntry = -1;
        }

        private List<string> drafts = new List<string>();
        private int revision;
        private Vector2 scroll;
        private int focusedEntry = -1;
        private Exception? validationError;
    }
}
