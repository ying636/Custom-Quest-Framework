using System;
using Verse;

namespace QuestEditor_Library
{
    public sealed class CQFSignalEndpoint
    {
        public CQFSignalEndpoint(object owner, string signal, string sourceLabel, bool receiver, bool automatic,
            bool template, bool questScoped, bool partScoped, Thing? sourceThing, string prefix = "", string partName = "",
            bool canChangePartScope = true, bool selectable = true, string? displaySignal = null)
        {
            this.Owner = owner;
            this.Signal = signal ?? string.Empty;
            this.SourceLabel = sourceLabel;
            this.IsReceiver = receiver;
            this.IsAutomatic = automatic;
            this.IsTemplate = template;
            this.QuestScoped = questScoped;
            this.PartScoped = partScoped;
            this.SourceThing = sourceThing;
            this.Prefix = prefix;
            this.PartName = partName;
            this.CanChangePartScope = canChangePartScope;
            this.Selectable = selectable;
            this.displaySignal = displaySignal;
        }

        public object Owner { get; }
        public string Signal { get; }
        public string SourceLabel { get; }
        public bool IsReceiver { get; }
        public bool IsAutomatic { get; }
        public bool IsTemplate { get; }
        public bool QuestScoped { get; }
        public bool PartScoped { get; }
        public Thing? SourceThing { get; }
        public string Prefix { get; }
        public string PartName { get; }
        public bool CanChangePartScope { get; }
        public bool Selectable { get; }
        public bool CanLocate => this.SourceThing?.Spawned == true;
        public string ScopeLabel => (this.PartScoped ? "CQF_MapSignals_Part" : this.QuestScoped ? "CQF_MapSignals_Quest" : "CQF_MapSignals_Global").Translate();
        public string DisplaySignal => this.displaySignal ?? (this.IsTemplate
            ? (this.QuestScoped ? "Quest{id}." : "") + this.Signal + (this.PartScoped ? "{" + (this.PartName.NullOrEmpty() ? "part" : this.PartName) + ":index}" : "")
            : this.Signal);

        public bool Matches(CQFSignalEndpoint other)
        {
            return this.Signal == other.Signal && this.IsTemplate == other.IsTemplate
                && (!this.IsTemplate || this.QuestScoped == other.QuestScoped && this.PartScoped == other.PartScoped
                    && (!this.PartScoped || this.PartName == other.PartName));
        }

        public void Locate()
        {
            if (!this.CanLocate)
            {
                return;
            }
            CameraJumper.TryJump(this.SourceThing!.Position, this.SourceThing.Map);
            Find.Selector.ClearSelection();
            Find.Selector.Select(this.SourceThing);
        }

        private readonly string? displaySignal;
    }
}
