using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace QuestEditor_Library
{
    public partial class DialogTreeDef
    {
        public override void ResolveReferences()
        {
            base.ResolveReferences();
            this.Update();
        }

        public void ResolveOptions()
        {
            if (this.optionMoulds == null || this.optionMoulds.Any(pair => pair.Key < 0 || pair.Value == null)
                || this.optionMoulds.Values.Distinct().Count() != this.optionMoulds.Count)
                throw new InvalidDataException("CQF_DialogGraph_InvalidOptions");
            foreach (DialogNode node in this.nodeMoulds.Values)
            {
                if (node == null || node.options == null || node.optionIds == null)
                    throw new InvalidDataException("CQF_DialogGraph_InvalidOptions");
                if (node.optionsResolved || node.optionIds.Count == 0 && node.options.Count > 0)
                {
                    if (node.options.Any(option => option == null) || node.options.Distinct().Count() != node.options.Count)
                        throw new InvalidDataException("CQF_DialogGraph_InvalidOptions");
                    node.optionIds = node.options.Select(this.RegisterOption).ToList();
                }
                else
                {
                    if (node.optionIds.Distinct().Count() != node.optionIds.Count
                        || node.optionIds.Any(id => !this.optionMoulds.ContainsKey(id)))
                        throw new InvalidDataException("CQF_DialogGraph_InvalidOptions");
                    node.options = node.optionIds.Select(id => this.optionMoulds[id]).ToList();
                }
                node.optionsResolved = true;
            }
        }

        public int RegisterOption(DialogOption option)
        {
            foreach (var pair in this.optionMoulds)
                if (ReferenceEquals(pair.Value, option)) return pair.Key;
            while (this.optionMoulds.ContainsKey(this.curOptionIndex)) this.curOptionIndex = checked(this.curOptionIndex + 1);
            int id = this.curOptionIndex;
            this.curOptionIndex = checked(this.curOptionIndex + 1);
            this.optionMoulds.Add(id, option);
            return id;
        }

        public void LinkOption(DialogNode node, DialogOption option)
        {
            this.ResolveOptions();
            this.RegisterOption(option);
            if (!node.options.Contains(option)) node.options.Add(option);
            this.Update();
        }

        public void UnlinkOption(DialogNode node, DialogOption option)
        {
            this.ResolveOptions();
            node.options.Remove(option);
            this.Update();
        }

        public void DeleteOption(DialogOption option)
        {
            this.ResolveOptions();
            foreach (DialogNode node in this.nodeMoulds.Values) node.options.Remove(option);
            int? id = this.optionMoulds.Where(pair => ReferenceEquals(pair.Value, option)).Select(pair => (int?)pair.Key).SingleOrDefault();
            if (id.HasValue) this.optionMoulds.Remove(id.Value);
            this.Update();
        }

        public Dictionary<int, DialogOption> optionMoulds = new Dictionary<int, DialogOption>();
        public int curOptionIndex;
    }
}
