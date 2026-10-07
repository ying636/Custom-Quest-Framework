using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace QuestEditor_Library
{
    public partial class QuestEditor_Dialog
    {
        public void CreateOption(DialogNode? owner, Vector2 position, Type? type = null)
        {
            DialogOption option = type == null ? new DialogOption() : (DialogOption)Activator.CreateInstance(type);
            option.text = "CQF_Dialog_Option_" + this.CurTree.curOptionIndex;
            option.editorX = position.x; option.editorY = position.y; option.editorPositionSet = true;
            this.CurTree.RegisterOption(option);
            if (owner != null) this.CurTree.LinkOption(owner, option);
            this.SelectOption(owner, option, false);
            this.RecordChanges();
        }

        public void AddSpecialOption(DialogNode? owner, Vector2 position)
        {
            CQFEditorTools.DrawFloatMenu(typeof(DialogOption).AllSubclassesNonAbstract(), type => this.CreateOption(owner, position, type), type => type.Name.Translate());
        }

        public void ShowOptionMenu(DialogOption option, DialogNode? owner = null, DialogResult? result = null)
        {
            List<FloatMenuOption> menu = new List<FloatMenuOption>
            {
                new FloatMenuOption("CQF_DialogGraph_ShowInspector".Translate(), () =>
                {
                    if (result != null) this.SelectResult(owner, option, result);
                    else this.SelectOption(owner, option);
                }),
                new FloatMenuOption("CQF_DialogGraph_LinkToNode".Translate(), () => Find.WindowStack.Add(new FloatMenu(
                    this.CurTree.nodeMoulds.Values.Where(node => !node.options.Contains(option)).Select(node => new FloatMenuOption(
                        this.DisplayText(node.text).Replace('\n', ' '), () => { this.CurTree.LinkOption(node, option); this.RecordChanges(); })).ToList()))),
                new FloatMenuOption("CQF_DialogGraph_AddResult".Translate(), () =>
                {
                    DialogResult added = new DialogResult { resultName = "CQF_Dialog_Result_" + option.results.Count };
                    option.results.Add(added);
                    this.SelectResult(owner, option, added);
                    this.RecordChanges();
                })
            };
            if (owner != null)
            {
                menu.Add(new FloatMenuOption("CQF_DialogGraph_UnlinkReference".Translate(), () => this.UnlinkOption(owner, option)));
                menu.Add(new FloatMenuOption("CQF_DialogGraph_MakeLocal".Translate(), () => this.CloneOption(option, owner)));
            }
            else
            {
                menu.Add(new FloatMenuOption("CQF_DialogGraph_UnlinkFrom".Translate(), () => Find.WindowStack.Add(new FloatMenu(
                    this.CurTree.nodeMoulds.Values.Where(node => node.options.Contains(option)).Select(node => new FloatMenuOption(
                        this.DisplayText(node.text).Replace('\n', ' '), () => this.UnlinkOption(node, option))).ToList()))));
                menu.Add(new FloatMenuOption("CQF_DialogGraph_CopyOption".Translate(), () => this.CloneOption(option, null)));
            }
            if (result != null)
            {
                menu.Add(new FloatMenuOption("CQF_DialogGraph_Disconnect".Translate(), result.nextIndex.HasValue ? () =>
                { result.nextIndex = null; this.InitCurTree(); } : null));
                menu.Add(new FloatMenuOption("CQF_DialogGraph_RemoveResult".Translate(), () =>
                { option.results.Remove(result); this.SelectOption(owner, option, false); this.InitCurTree(); }));
            }
            menu.Add(new FloatMenuOption("CQF_DialogGraph_DeleteOption".Translate(), () =>
            {
                int references = this.CurTree.nodeMoulds.Values.Count(node => node.options.Contains(option));
                if (references > 1) Find.WindowStack.Add(new Dialog_MessageBox("CQF_DialogGraph_ConfirmDeleteShared".Translate(references),
                    "Confirm".Translate(), () => this.DeleteOption(option), "Cancel".Translate()));
                else this.DeleteOption(option);
            }));
            Find.WindowStack.Add(new FloatMenu(menu));
        }

        private void UnlinkOption(DialogNode owner, DialogOption option)
        {
            this.CurTree.UnlinkOption(owner, option);
            this.SelectNode(owner, false);
            this.InitCurTree();
        }

        private void DeleteOption(DialogOption option)
        {
            this.CurTree.DeleteOption(option);
            if (this.selectedOption == option) { this.selectedOption = null; this.selectedResult = null; }
            this.InitCurTree();
        }

        private void CloneOption(DialogOption option, DialogNode? owner)
        {
            int id = this.CurTree.optionMoulds.Single(pair => pair.Value == option).Key;
            DialogOption copy = this.session.Copy(this.CurTree).optionMoulds[id];
            copy.editorX += 40f; copy.editorY += 40f;
            this.CurTree.RegisterOption(copy);
            if (owner != null) owner.options[owner.options.IndexOf(option)] = copy;
            this.CurTree.Update();
            this.SelectOption(owner, copy, false);
            this.RecordChanges();
        }

        private string DisplayText(string text) => text.CanTranslate() ? text.Translate().ToString() : text ?? string.Empty;
    }
}
