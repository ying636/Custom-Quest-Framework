using System.Xml.Linq;
using Verse;

namespace QuestEditor_Library
{
    public sealed class CQFSymmetryEditSession
    {
        public CQFSymmetryEditSession(Thing? owner)
        {
            if (owner == null || !owner.Spawned || owner is Pawn) return;
            CQFEditorSymmetry component = CQFEditorSymmetry.For(owner.Map);
            if (!component.Active) return;
            try
            {
                this.model = new CQFAIModel();
                this.before = this.model.Write(CQFAILiveMap.Configuration(owner));
                this.owner = owner;
                this.symmetry = component;
                this.transforms = component.Transforms().Where(transform => transform.Axes != 0).ToArray();
            }
            catch (Exception error)
            {
                Log.Error("[CQF] Symmetry could not capture configuration for " + owner.ThingID + ": " + error);
                Messages.Message("CQF_SymmetryConfigurationFailed".Translate(), RimWorld.MessageTypeDefOf.RejectInput, false);
            }
        }

        public void Complete()
        {
            if (this.completed) return;
            this.completed = true;
            if (this.owner == null || this.symmetry == null || this.model == null || this.before == null) return;
            if (!this.owner.Spawned || this.owner.Map != this.symmetry.map || !this.symmetry.Active) return;
            using CQFSymmetryScope scope = new CQFSymmetryScope(this.symmetry);
            try
            {
                XElement after = this.model.Write(CQFAILiveMap.Configuration(this.owner));
                List<XElement> changed = after.Elements().Where(element => !XNode.DeepEquals(this.before.Element(element.Name), element)).ToList();
                if (changed.Count == 0) return;
                HashSet<Thing> visited = new HashSet<Thing> { this.owner };
                List<(Thing target, CQFAILiveThingConfiguration value)> changes = new();
                foreach (CQFSymmetryTransform transform in this.transforms!)
                {
                    Thing? target = this.symmetry.Counterpart(this.owner, transform);
                    if (target == null) return;
                    if (!visited.Add(target)) continue;
                    CQFAILiveThingConfiguration original = CQFAILiveMap.Configuration(target);
                    XElement xml = this.model.Write(original);
                    foreach (XElement element in changed) xml.Element(element.Name)!.ReplaceWith(new XElement(element));
                    CQFAILiveThingConfiguration value = (CQFAILiveThingConfiguration)this.model.Read(xml, typeof(CQFAILiveThingConfiguration), original)!;
                    if (changed.Any(element => element.Name.LocalName == "zone") && value.zone != null)
                    {
                        value.zone.coreRotation = transform.Rotation(value.zone.coreRotation);
                        if ((transform.Axes & 1) != 0)
                            (value.zone.size.minX, value.zone.size.maxX) = (value.zone.size.maxX, value.zone.size.minX);
                        if ((transform.Axes & 2) != 0)
                            (value.zone.size.minZ, value.zone.size.maxZ) = (value.zone.size.maxZ, value.zone.size.minZ);
                    }
                    CQFAILiveMap.ValidateConfiguration(target, value);
                    changes.Add((target, value));
                }
                foreach (var change in changes) CQFAILiveMap.ApplyConfiguration(change.target, change.value);
            }
            catch (Exception error)
            {
                Log.Error("[CQF] Symmetry configuration synchronization failed for " + this.owner.ThingID + ": " + error);
                this.symmetry.Reject("CQF_SymmetryConfigurationFailed".Translate());
            }
        }

        private readonly Thing? owner;
        private readonly CQFEditorSymmetry? symmetry;
        private readonly CQFAIModel? model;
        private readonly XElement? before;
        private readonly CQFSymmetryTransform[]? transforms;
        private bool completed;
    }
}
