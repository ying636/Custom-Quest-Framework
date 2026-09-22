using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using Verse;

namespace QuestEditor_Library
{
    public sealed class CQFInteractionDraft
    {
        public CQFInteractionDraft(IEnumerable<InteractionOperation> operations, IEnumerable<InteractionDataDef> definitions = null)
        {
            this.documents = (operations ?? Enumerable.Empty<InteractionOperation>())
                .Concat((definitions ?? Enumerable.Empty<InteractionDataDef>()).SelectMany(d => d.interactions))
                .Select(o => o.SaveToXElement("li")).ToList();
            foreach (XElement element in this.documents.SelectMany(d => d.Descendants()))
            {
                string kind = this.GetReferenceKind(element);
                if (kind == null || string.IsNullOrWhiteSpace(element.Value)
                    || (kind == "CQF_EditorTargetKey" && CQFEditorTools.TargetTexts.Contains(element.Value)))
                {
                    continue;
                }
                CQFConfigurationReference reference = this.References.Find(r => r.Kind == kind && r.Original == element.Value);
                if (reference == null)
                {
                    reference = new CQFConfigurationReference(kind, element.Value);
                    this.References.Add(reference);
                }
                reference.Elements.Add(element);
            }
        }

        public int Count => this.documents.Count;
        public List<CQFConfigurationReference> References { get; } = new List<CQFConfigurationReference>();

        public List<InteractionOperation> CreateOperations()
        {
            foreach (CQFConfigurationReference reference in this.References)
            {
                if (string.IsNullOrWhiteSpace(reference.Value))
                {
                    throw new InvalidOperationException(reference.Kind + ": " + reference.Original);
                }
                foreach (XElement element in reference.Elements)
                {
                    element.Value = reference.Value.Trim();
                }
            }
            List<InteractionOperation> operations = new List<InteractionOperation>();
            foreach (XElement document in this.documents)
            {
                XmlDocument xml = new XmlDocument();
                xml.LoadXml(document.ToString());
                operations.Add(DirectXmlToObject.ObjectFromXml<InteractionOperation>(xml.DocumentElement, false));
            }
            DirectXmlCrossRefLoader.ResolveAllWantedCrossReferences(FailMode.LogErrors);
            return operations;
        }

        private string GetReferenceKind(XElement element)
        {
            if (element.HasElements)
            {
                return null;
            }
            string name = element.Name.LocalName;
            if (name == "signal" || name == "inSignal")
            {
                return "CQF_EditorSignal";
            }
            if (name == "keyOfBool" || name == "boolName")
            {
                return "CQF_EditorStateKey";
            }
            if (name == "targetText" || name == "recordKey" || name == "targetKey" || name == "targetA"
                || name == "targetB" || name == "positionName" || name == "interviewerText" || name == "intervieeText"
                || name == "entranceText" || name == "exitText" || name == "skipedTargetText" || name == "targetLocationText" || name == "stateTargetText"
                || (name == "li" && element.Parent?.Name.LocalName == "targetsText"))
            {
                return "CQF_EditorTargetKey";
            }
            return null;
        }

        private readonly List<XElement> documents;
    }
}
