#if RIMWORLD_1_7
using System.Xml.Linq;
using Verse;

namespace QuestEditor_Library
{
    internal sealed class CQFAIExternalSession
    {
        public CQFAIExternalSession(CQFAITargetCatalog targets, CQFAIModel model, bool editing, bool allowText)
        {
            CQFAIResourceCatalog resources = new CQFAIResourceCatalog(model);
            CQFAIConversation conversation = new CQFAIConversation(model, resources);
            harness = new CQFAIHarness(model, resources, conversation, null, "", editing, allowText, true, targets);
        }

        public bool IsBusy => active != null;
        public bool CanUndo => harness.Transaction?.CanUndo == true && harness.Transaction.IsCurrent;

        public void Update(CQFAIExternalPipe pipe)
        {
            if (active == null) active = pipe.Take();
            if (active is not { } pending) return;
            try
            {
                if (harness.HasPendingResponse)
                {
                    if (!harness.AdvanceResponse()) return;
                    Complete(pending, new XElement(harness.LastResults.Single()));
                    return;
                }

                XElement request = pending.Request;
                switch (request.Name.LocalName)
                {
                    case "status":
                        Complete(pending, new XElement("cqf_session", new XAttribute("gameLoaded", Current.Game != null),
                            new XAttribute("editingAllowed", harness.Registry.EditingAllowed), new XAttribute("canUndo", CanUndo)));
                        break;
                    case "discover":
                        string search = (string?)request.Attribute("search") ?? "";
                        if (search.Length > 100) throw new InvalidDataException("CQF_AI_InvalidTool");
                        Complete(pending, new XElement("tools", harness.Registry.Tools.Where(tool =>
                            tool.Name.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                            tool.Description.Contains(search, StringComparison.OrdinalIgnoreCase)).Select(tool => tool.Definition)));
                        break;
                    case "call":
                        CQFAIToolCall call = CQFAIToolCall.FromXml(request);
                        if (receipts.TryGetValue(call.Id, out var prior))
                        {
                            if (!XNode.DeepEquals(prior.Request, request)) throw new InvalidDataException("CQF_AI_ExternalDuplicate");
                            Complete(pending, prior.Result, remember: false);
                            break;
                        }
                        if (receipts.Count >= 512) throw new InvalidDataException("CQF_AI_ExternalLimit");
                        harness.BeginResponse(new XElement("assistant", new XElement("tools", call.ToXml())).ToString(SaveOptions.DisableFormatting));
                        break;
                    default:
                        throw new InvalidDataException("CQF_AI_InvalidTool");
                }
            }
            catch (Exception error)
            {
                Log.Warning("CQF MCP: " + error);
                bool remember = harness.HasPendingResponse;
                try { harness.CancelResponse(); }
                catch (Exception cleanup) { Log.Error("CQF MCP: " + cleanup); }
                Complete(pending, new XElement("error", error.Message), remember);
            }
        }

        public void Undo()
        {
            if (!IsBusy && CanUndo) harness.Transaction!.Undo();
        }

        public void Close()
        {
            try { harness.CancelResponse(); }
            finally
            {
                active?.Reply.TrySetResult(new XElement("error", "CQF_DialogAI_Cancelled"));
                active = null;
            }
        }

        private void Complete((XElement Request, TaskCompletionSource<XElement> Reply) pending, XElement result, bool remember = true)
        {
            if (remember && pending.Request.Name == "call" && pending.Request.Attribute("id") is { } id)
                receipts.Add(id.Value, (pending.Request, result));
            active = null;
            pending.Reply.TrySetResult(result);
        }

        private readonly CQFAIHarness harness;
        private readonly Dictionary<string, (XElement Request, XElement Result)> receipts = new Dictionary<string, (XElement, XElement)>(StringComparer.Ordinal);
        private (XElement Request, TaskCompletionSource<XElement> Reply)? active;
    }
}
#endif
