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
            this.forcePause = true;
            this.absorbInputAroundWindow = true;
            this.configurationName = CQFTargetKeyBook.CurrentConfigurationName;
            this.RefreshDrafts();
        }

        public override Vector2 InitialSize => new Vector2(800f, 680f);

        public override void DoWindowContents(Rect inRect)
        {
            if (this.revision != CQFTargetKeyBook.Revision)
            {
                this.RefreshDrafts();
            }
            Text.Font = GameFont.Medium;
            Widgets.Label(new Rect(0f, 0f, inRect.width - 40f, 32f), "CQF_TargetKeyBook_Title".Translate());
            Text.Font = GameFont.Small;
            Widgets.Label(new Rect(0f, 44f, 120f, 28f), "CQF_TargetKeyBook_ConfigurationName".Translate());
            this.configurationName = Widgets.TextField(new Rect(128f, 44f, inRect.width - 128f, 28f), this.configurationName);
            float buttonWidth = (inRect.width - 16f) / 3f;
            if (Widgets.ButtonText(new Rect(0f, 82f, buttonWidth, 30f), "CQF_TargetKeyBook_SaveConfiguration".Translate()))
            {
                this.Execute(() =>
                {
                    CQFTargetKeyBook.Replace(this.drafts.Where(value => !string.IsNullOrWhiteSpace(value)));
                    CQFTargetKeyBook.Save(this.configurationName);
                    this.RefreshDrafts();
                });
            }
            if (Widgets.ButtonText(new Rect(buttonWidth + 8f, 82f, buttonWidth, 30f), "CQF_TargetKeyBook_LoadConfiguration".Translate()))
            {
                this.ShowConfigurations(name =>
                {
                    CQFTargetKeyBook.Load(name);
                    this.configurationName = name;
                    this.RefreshDrafts();
                });
            }
            if (Widgets.ButtonText(new Rect((buttonWidth + 8f) * 2f, 82f, buttonWidth, 30f), "CQF_TargetKeyBook_SetDefaultConfiguration".Translate()))
            {
                this.ShowConfigurations(CQFTargetKeyBook.SetDefault);
            }
            Widgets.Label(new Rect(0f, 122f, inRect.width, 26f), "CQF_TargetKeyBook_CurrentConfiguration".Translate()
                + ": " + CQFTargetKeyBook.CurrentConfigurationName + (CQFTargetKeyBook.Modified || !this.drafts.SequenceEqual(CQFTargetKeyBook.Keys) ? " *" : ""));
            Widgets.Label(new Rect(0f, 152f, inRect.width - 138f, 28f), "CQF_TargetKeyBook_DefaultConfiguration".Translate()
                + ": " + CQFTargetKeyBook.DefaultConfigurationName);
            if (Widgets.ButtonText(new Rect(inRect.width - 130f, 150f, 130f, 28f), "CQF_TargetKeyBook_ClearDefaultConfiguration".Translate(), active: CQFTargetKeyBook.DefaultConfigurationName.Length > 0))
            {
                this.Execute(() => CQFTargetKeyBook.SetDefault(string.Empty));
            }
            Widgets.Label(new Rect(0f, 190f, 180f, 28f), "TargetKey".Translate());
            if (Widgets.ButtonText(new Rect(inRect.width - 130f, 186f, 130f, 30f), "CQF_TargetKeyBook_AddKey".Translate()))
            {
                this.drafts.Add(string.Empty);
                this.focusedEntry = this.drafts.Count - 1;
                this.scroll.y = Mathf.Max(0f, this.drafts.Count * 38f - (inRect.height - 279f));
            }
            Rect outRect = new Rect(0f, 229f, inRect.width, inRect.height - 279f);
            Rect viewRect = new Rect(0f, 0f, outRect.width - 20f, Mathf.Max(outRect.height, this.drafts.Count * 38f));
            Widgets.BeginScrollView(outRect, ref this.scroll, viewRect);
            bool changed = false;
            for (int index = 0; index < this.drafts.Count; index++)
            {
                float y = index * 38f;
                Widgets.Label(new Rect(0f, y + 3f, 30f, 26f), (index + 1).ToString());
                GUI.SetNextControlName("CQF_TargetKeyBook_Entry_" + index);
                string value = Widgets.TextField(new Rect(38f, y, viewRect.width - 130f, 30f), this.drafts[index]);
                if (value != this.drafts[index])
                {
                    this.drafts[index] = value;
                    changed = true;
                }
                if (Widgets.ButtonText(new Rect(viewRect.width - 84f, y, 84f, 30f), "Remove".Translate()))
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
                Widgets.Label(new Rect(0f, inRect.height - 40f, inRect.width - 146f, 36f), reason.Colorize(Color.red));
            }
            if (Widgets.ButtonText(new Rect(inRect.width - 130f, inRect.height - 36f, 130f, 32f), "Close".Translate()))
            {
                this.Close();
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

        private void ShowConfigurations(Action<string> selected)
        {
            this.Execute(() =>
            {
                List<FloatMenuOption> options = CQFTargetKeyBook.Store.ConfigurationNames
                    .Select(name => new FloatMenuOption(name, () => this.Execute(() => selected(name)))).ToList();
                if (options.Count == 0)
                {
                    options.Add(new FloatMenuOption("CQF_TargetKeyBook_NoConfigurations".Translate(), null));
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
        private string configurationName;
        private Vector2 scroll;
        private int focusedEntry = -1;
        private Exception? validationError;
    }
}
