using System.Xml.Linq;
using RimWorld;
using Verse;

namespace QuestEditor_Library
{
    public sealed class CQFAIInventoryRuntime
    {
        public CQFAIInventoryRuntime(CQFAIRuntimeJournal journal) { this.journal = journal; }
        public XElement Help() => new XElement("inventory",
            new XElement("query", "kind=inventory,thing_id=exact spawned holder Thing ID,slot=inventory|equipment|apparel|contents,offset,limit. Pawn slot defaults inventory, other holders contents. Reads actual held Things, not generation recipes."),
            new XElement("operation", new XAttribute("kind", "inventory_add"), "thing_id,slot,definition=ThingDef,count=1..stackLimit,stuff=exact compatible Stuff if MadeFromStuff. Creates one Item stack in Pawn inventory or directly held contents; equipment/apparel slots are read-only."),
            new XElement("operation", new XAttribute("kind", "inventory_remove"), "thing_id,slot,item_id. Removes the exact entire held stack; undo restores that same instance. Not Destroy or consume actions."),
            new XElement("operation", new XAttribute("kind", "inventory_count"), "thing_id,slot,item_id,count=1..stackLimit. Changes an existing held Item's stackCount with undo."),
            new XElement("operation", new XAttribute("kind", "inventory_transfer"), "thing_id,slot,item_id,destination_id,destination_slot. Transfers the entire held Item instance between supported inventories without stack merging. Never takes a Pawn/building, equips/wears items, or despawns map objects."));
        public XElement Read(XElement request)
        {
            CQFAIRuntimeRequest.Fields(request, "kind", "thing_id", "slot", "offset", "limit");
            Thing thing = CQFAIRuntimeRequest.Thing(CQFAIRuntimeRequest.Text(request, "thing_id"));
            ThingOwner owner = Owner(thing, CQFAIRuntimeRequest.Text(request, "slot"), false);
            return CQFAIRuntimeRequest.Page("inventory", owner.Select(item => Item(item)), request);
        }
        public XElement Operate(XElement request)
        {
            string kind = CQFAIRuntimeRequest.Text(request, "kind");
            CQFAIRuntimeRequest.Fields(request, kind switch
            {
                "inventory_add" => new[] { "kind", "thing_id", "slot", "definition", "count", "stuff" },
                "inventory_remove" => new[] { "kind", "thing_id", "slot", "item_id" },
                "inventory_count" => new[] { "kind", "thing_id", "slot", "item_id", "count" },
                "inventory_transfer" => new[] { "kind", "thing_id", "slot", "item_id", "destination_id", "destination_slot" },
                _ => throw new InvalidDataException("CQF_AI_InvalidTool: inventory operation")
            });
            Thing thing = CQFAIRuntimeRequest.Thing(CQFAIRuntimeRequest.Text(request, "thing_id"));
            ThingOwner owner = Owner(thing, CQFAIRuntimeRequest.Text(request, "slot"), true);
            string key = "inventory:" + thing.ThingID + ":" + CQFAIRuntimeRequest.Text(request, "slot");
            if (kind == "inventory_add")
            {
                ThingDef def = CQFAIRuntimeRequest.Def<ThingDef>(CQFAIRuntimeRequest.Text(request, "definition"));
                int count = CQFAIRuntimeRequest.Int(request, "count", 1);
                string material = CQFAIRuntimeRequest.Text(request, "stuff");
                ThingDef? stuff = material.Length == 0 ? null : CQFAIRuntimeRequest.Def<ThingDef>(material);
                if (def.category != ThingCategory.Item || count < 1 || count > def.stackLimit || def.MadeFromStuff && (stuff == null || !GenStuff.AllowedStuffsFor(def).Contains(stuff)) || !def.MadeFromStuff && stuff != null)
                    throw new InvalidDataException("CQF_AI_InvalidValue: inventory Item/count/material");
                Thing item = ThingMaker.MakeThing(def, stuff); item.stackCount = count;
                if (owner.GetCountCanAccept(item, false) < count) { item.Destroy(); throw new InvalidDataException("CQF_AI_InvalidValue: holder cannot accept stack"); }
                return journal.Edit(key, () => { if (!owner.TryAdd(item, false)) throw new InvalidDataException("CQF_AI_ApplyMismatch: inventory add"); },
                    () => { if (owner.Contains(item) && !owner.Remove(item)) throw new InvalidOperationException("CQF_AI_RollbackMismatch: inventory add"); }, () => Snapshot(owner));
            }
            Thing target = owner.FirstOrDefault(item => item.ThingID == CQFAIRuntimeRequest.Text(request, "item_id")) ?? throw new InvalidDataException("CQF_AI_MissingResource: held item");
            if (target.def.category != ThingCategory.Item) throw new InvalidDataException("CQF_AI_InvalidValue: only Item stacks may be operated");
            if (kind == "inventory_count")
            {
                int count = CQFAIRuntimeRequest.Int(request, "count"), before = target.stackCount;
                if (count < 1 || count > target.def.stackLimit) throw new InvalidDataException("CQF_AI_InvalidValue: inventory count");
                return journal.Edit(key, () => target.stackCount = count, () => target.stackCount = before, () => Snapshot(owner));
            }
            if (kind == "inventory_remove") return journal.Edit(key,
                () => { if (!owner.Remove(target)) throw new InvalidDataException("CQF_AI_ApplyMismatch: inventory remove"); },
                () => { if (!owner.Contains(target) && !owner.TryAdd(target, false)) throw new InvalidOperationException("CQF_AI_RollbackMismatch: inventory remove"); }, () => Snapshot(owner));
            if (kind != "inventory_transfer") throw new InvalidDataException("CQF_AI_InvalidTool: inventory operation");
            Thing destination = CQFAIRuntimeRequest.Thing(CQFAIRuntimeRequest.Text(request, "destination_id"));
            ThingOwner other = Owner(destination, CQFAIRuntimeRequest.Text(request, "destination_slot"), true);
            if (ReferenceEquals(owner, other) || other.GetCountCanAccept(target, false) < target.stackCount) throw new InvalidDataException("CQF_AI_InvalidValue: invalid destination/capacity");
            return journal.Edit(key + ":to:" + destination.ThingID,
                () => { if (!owner.Remove(target) || !other.TryAdd(target, false)) throw new InvalidDataException("CQF_AI_ApplyMismatch: inventory transfer"); },
                () => { if (other.Contains(target) && !other.Remove(target) || !owner.Contains(target) && !owner.TryAdd(target, false)) throw new InvalidOperationException("CQF_AI_RollbackMismatch: inventory transfer"); },
                () => new XElement("transfer", Snapshot(owner), Snapshot(other)));
        }
        private static ThingOwner Owner(Thing thing, string slot, bool write)
        {
            if (thing is Pawn pawn)
            {
                if (slot is "" or "inventory") return pawn.inventory?.innerContainer ?? throw new InvalidDataException("CQF_AI_MissingResource: Pawn inventory");
                if (!write && slot == "equipment") return ((IThingHolder)pawn.equipment).GetDirectlyHeldThings();
                if (!write && slot == "apparel") return ((IThingHolder)pawn.apparel).GetDirectlyHeldThings();
            }
            else if (slot is "" or "contents" && thing is IThingHolder holder) return holder.GetDirectlyHeldThings() ?? throw new InvalidDataException("CQF_AI_MissingResource: holder contents");
            throw new InvalidDataException("CQF_AI_InvalidTool: unsupported holder slot");
        }
        private static XElement Item(Thing thing) => new XElement("item", new XAttribute("id", thing.ThingID), new XAttribute("def", thing.def.defName), new XAttribute("count", thing.stackCount), new XAttribute("stuff", thing.Stuff?.defName ?? ""), new XAttribute("hitPoints", thing.HitPoints));
        private static XElement Snapshot(ThingOwner owner) => new XElement("inventory", owner.OrderBy(item => item.ThingID, StringComparer.Ordinal).Select(Item));
        private readonly CQFAIRuntimeJournal journal;
    }
}
