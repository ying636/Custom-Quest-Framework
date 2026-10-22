using System.Text;
using System.Xml;
using System.Xml.Linq;
using Verse;

namespace QuestEditor_Library
{
    public sealed class CQFEntityRecipeStore
    {
        public CQFEntityRecipeStore(string directory)
        {
            DirectoryPath = Path.GetFullPath(directory);
        }

        public string DirectoryPath { get; }

        public IReadOnlyList<PawnSpawnData> Load()
        {
            List<PawnSpawnData> result = new();
            if (!Directory.Exists(DirectoryPath)) return result;
            foreach (string path in Directory.EnumerateFiles(DirectoryPath, "*.cqfentity").OrderBy(value => value, StringComparer.OrdinalIgnoreCase))
            {
                using Stream stream = File.OpenRead(path);
                using XmlReader reader = XmlReader.Create(stream, new XmlReaderSettings { DtdProcessing = DtdProcessing.Prohibit, XmlResolver = null, MaxCharactersInDocument = 2097152 });
                XElement root = XElement.Load(reader);
                string id = root.Element("id")?.Value ?? throw new InvalidDataException("CQF_EntityBook_InvalidAsset".Translate(path));
                ValidateId(id);
                if (root.Name != "CQFEntityRecipe" || Path.GetFileNameWithoutExtension(path) != id)
                    throw new InvalidDataException("CQF_EntityBook_InvalidAsset".Translate(path));
                PawnSpawnData recipe = FromElement(root.Element("recipe") ?? throw new InvalidDataException("CQF_EntityBook_InvalidAsset".Translate(path)));
                if (recipe.EntityBookSourceId != "spawn:" + id)
                    throw new InvalidDataException("CQF_EntityBook_InvalidAsset".Translate(path));
                result.Add(recipe);
            }
            return result;
        }

        public PawnSpawnData Save(PawnSpawnData recipe)
        {
            if (!recipe.CanSaveToMap()) throw new InvalidDataException("CQF_EntityBook_InvalidRecipe".Translate());
            string id = recipe.EntityBookSourceId.Substring("spawn:".Length);
            ValidateId(id);
            XElement root = new("CQFEntityRecipe", new XElement("id", id), recipe.SaveToXElement("recipe"));
            PawnSpawnData copy = FromElement(root.Element("recipe")!);
            Directory.CreateDirectory(DirectoryPath);
            string target = Path.Combine(DirectoryPath, id + ".cqfentity");
            string temporary = target + "." + Guid.NewGuid().ToString("N") + ".tmp";
            try
            {
                using (XmlWriter writer = XmlWriter.Create(temporary, new XmlWriterSettings { Encoding = new UTF8Encoding(false), Indent = true })) root.Save(writer);
                if (File.Exists(target)) File.Replace(temporary, target, target + ".bak");
                else File.Move(temporary, target);
            }
            finally
            {
                if (File.Exists(temporary)) File.Delete(temporary);
            }
            return copy;
        }

        public static PawnSpawnData Copy(PawnSpawnData recipe) => FromElement(recipe.SaveToXElement("recipe"));

        public static PawnSpawnData CopyAsNew(PawnSpawnData recipe)
        {
            XElement element = recipe.SaveToXElement("recipe");
            foreach (XElement identifier in element.Descendants("entityBookId").ToList()) identifier.Remove();
            PawnSpawnData copy = FromElement(element);
            _ = copy.EntityBookSourceId;
            return copy;
        }

        private static PawnSpawnData FromElement(XElement element)
        {
            XmlDocument document = new() { XmlResolver = null };
            document.LoadXml(element.ToString(SaveOptions.DisableFormatting));
            PawnSpawnData recipe = DirectXmlToObject.ObjectFromXml<PawnSpawnData>(document.DocumentElement, false)
                ?? throw new InvalidDataException("CQF_EntityBook_InvalidRecipe".Translate());
            DirectXmlCrossRefLoader.ResolveAllWantedCrossReferences(FailMode.LogErrors);
            return recipe;
        }

        private static void ValidateId(string id)
        {
            if (id.Length == 0 || id.Length > 128 || id.Any(character => !char.IsLetterOrDigit(character) && character != '_' && character != '-'))
                throw new InvalidDataException("CQF_EntityBook_InvalidIdentity".Translate());
        }
    }
}
