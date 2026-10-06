using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace QuestEditor_Library
{
    public sealed class Window_CQFSignalBook : Window
    {
        public Window_CQFSignalBook()
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
            if (this.revision != CQFSignalBook.Revision)
            {
                this.RefreshDrafts();
            }
            Text.Font = GameFont.Small;
            Widgets.Label(new Rect(0f, 0f, inRect.width - 148f, 24f), "CQF_SignalBook_Title".Translate() + " (" + this.drafts.Count + ")");
            float toolbarX = inRect.width - 138f;
            if (Widgets.ButtonImage(new Rect(toolbarX, 1f, 22f, 22f), TexButton.NewFile,
                tooltip: "CQF_SignalBook_NewConfiguration".Translate()))
            {
                this.OpenConfigurationName(true);
            }
            if (Widgets.ButtonImage(new Rect(toolbarX + 28f, 1f, 22f, 22f), TexButton.Save,
                tooltip: "CQF_SignalBook_SaveConfiguration".Translate()))
            {
                if (CQFSignalBook.CurrentConfigurationName.Length == 0)
                {
                    this.OpenConfigurationName(false);
                }
                else
                {
                    this.Execute(() => this.SaveConfiguration(CQFSignalBook.CurrentConfigurationName));
                }
            }
            if (Widgets.ButtonImage(new Rect(toolbarX + 56f, 1f, 22f, 22f), TexButton.Reload,
                tooltip: "CQF_SignalBook_LoadConfiguration".Translate()))
            {
                this.ShowConfigurations(this.LoadConfiguration);
            }
            string defaultName = CQFSignalBook.DefaultConfigurationName.Length > 0
                ? CQFSignalBook.DefaultConfigurationName : "CQF_SignalBook_NoDefaultConfiguration".Translate().ToString();
            if (Widgets.ButtonImage(new Rect(toolbarX + 84f, 1f, 22f, 22f), TexButton.AutoHomeArea,
                tooltip: "CQF_SignalBook_SetDefaultConfiguration".Translate() + "\n"
                    + "CQF_SignalBook_DefaultConfiguration".Translate() + ": " + defaultName))
            {
                this.ShowConfigurations(CQFSignalBook.SetDefault, true);
            }
            string currentName = CQFSignalBook.CurrentConfigurationName.Length > 0
                ? CQFSignalBook.CurrentConfigurationName : "CQF_SignalBook_UnsavedConfiguration".Translate().ToString();
            string configurationText = "CQF_SignalBook_CurrentConfiguration".Translate() + ": " + currentName;
            if (CQFSignalBook.CurrentConfigurationName.Length > 0 && CQFSignalBook.CurrentConfigurationName == CQFSignalBook.DefaultConfigurationName)
            {
                configurationText += " " + "CQF_SignalBook_DefaultIndicator".Translate();
            }
            if (CQFSignalBook.Modified || !this.drafts.SequenceEqual(CQFSignalBook.Signals))
            {
                configurationText += " *";
            }
            Rect configurationRect = new Rect(0f, 30f, inRect.width - 30f, 24f);
            if (Widgets.ButtonText(configurationRect, configurationText.Truncate(configurationRect.width), drawBackground: false, overrideTextAnchor: TextAnchor.MiddleLeft))
            {
                this.ShowConfigurations(this.LoadConfiguration);
            }
            TooltipHandler.TipRegion(configurationRect, configurationText + "\n" + "CQF_SignalBook_SelectConfiguration".Translate());
            float errorHeight = this.validationError == null ? 0f : Text.CalcHeight(
                this.validationError.Message.CanTranslate() ? this.validationError.Message.Translate().ToString() : this.validationError.Message,
                inRect.width) + 6f;
            Rect outRect = new Rect(0f, 62f, inRect.width, inRect.height - 66f - errorHeight);
            if (Widgets.ButtonImage(new Rect(inRect.width - 22f, 30f, 22f, 22f), TexButton.Plus,
                tooltip: "CQF_SignalBook_AddSignal".Translate()))
            {
                this.drafts.Add(string.Empty);
                this.focusedEntry = this.drafts.Count - 1;
                this.scroll.y = Mathf.Max(0f, this.drafts.Count * 28f - outRect.height);
            }
            Rect viewRect = new Rect(0f, 0f, outRect.width - 20f, Mathf.Max(outRect.height, this.drafts.Count * 28f));
            Widgets.BeginScrollView(outRect, ref this.scroll, viewRect);
            bool changed = false;
            for (int index = 0; index < this.drafts.Count; index++)
            {
                float y = index * 28f;
                Widgets.Label(new Rect(0f, y + 2f, 24f, 24f), (index + 1).ToString());
                GUI.SetNextControlName("CQF_SignalBook_Entry_" + index);
                string value = Widgets.TextField(new Rect(28f, y, viewRect.width - 60f, 24f), this.drafts[index]);
                if (value != this.drafts[index])
                {
                    this.drafts[index] = value;
                    changed = true;
                }
                if (Widgets.ButtonImage(new Rect(viewRect.width - 22f, y + 1f, 20f, 20f), TexButton.Delete, tooltip: "Remove".Translate()))
                {
                    this.drafts.RemoveAt(index);
                    changed = true;
                    break;
                }
            }
            if (this.focusedEntry >= 0)
            {
                UI.FocusControl("CQF_SignalBook_Entry_" + this.focusedEntry, this);
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
                CQFSignalBook.ReportError(this.validationError);
            }
            base.Close(doCloseSound);
        }

        private void SynchronizeDrafts()
        {
            try
            {
                CQFSignalBook.Replace(this.drafts.Where(value => !string.IsNullOrWhiteSpace(value)));
                this.revision = CQFSignalBook.Revision;
                this.validationError = null;
            }
            catch (ArgumentException exception)
            {
                this.validationError = exception;
            }
            catch (Exception exception)
            {
                this.validationError = exception;
                CQFSignalBook.ReportError(exception);
            }
        }

        private void OpenConfigurationName(bool createEmpty)
        {
            Find.WindowStack.Add(new Dialog_CQFSignalBookConfiguration(
                createEmpty ? "CQF_SignalBook_NewConfiguration" : "CQF_SignalBook_SaveConfiguration", name =>
                {
                    if (CQFSignalBook.Store.ConfigurationNames.Contains(name, StringComparer.OrdinalIgnoreCase))
                    {
                        throw new ArgumentException("CQF_SignalBook_ConfigurationExists");
                    }
                    if (createEmpty)
                    {
                        CQFSignalBook.Store.Save(name, Array.Empty<string>());
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
            CQFSignalBook.Replace(this.drafts.Where(value => !string.IsNullOrWhiteSpace(value)));
            CQFSignalBook.Save(name);
            this.RefreshDrafts();
        }

        private void LoadConfiguration(string name)
        {
            CQFSignalBook.Load(name);
            this.RefreshDrafts();
            this.scroll = Vector2.zero;
        }

        private void ShowConfigurations(Action<string> selected, bool selectDefault = false)
        {
            this.Execute(() =>
            {
                List<FloatMenuOption> options = CQFSignalBook.Store.ConfigurationNames
                    .Select(name => new FloatMenuOption(name, () => this.Execute(() => selected(name)))).ToList();
                if (options.Count == 0)
                {
                    options.Add(new FloatMenuOption("CQF_SignalBook_NoConfigurations".Translate(), null));
                }
                if (selectDefault && CQFSignalBook.DefaultConfigurationName.Length > 0)
                {
                    options.Add(new FloatMenuOption("CQF_SignalBook_ClearDefaultConfiguration".Translate(),
                        () => this.Execute(() => CQFSignalBook.SetDefault(string.Empty))));
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
                CQFSignalBook.ReportError(exception);
            }
        }

        private void RefreshDrafts()
        {
            this.drafts = CQFSignalBook.Signals.ToList();
            this.revision = CQFSignalBook.Revision;
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
