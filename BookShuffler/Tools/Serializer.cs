using System.Collections.Specialized;
using System.IO;
using BookShuffler.Models;
using BookShuffler.ViewModels;

namespace BookShuffler.Parsing
{
    public static class Serializer
    {
        public static void Serialize(this IEntityViewModel entity, string projectPath)
        {
            var sectionPath = Path.Combine(projectPath, ProjectLoader.SectionFolderName);
            if (!Directory.Exists(sectionPath)) Directory.CreateDirectory(sectionPath);
            
            var cardPath = Path.Combine(projectPath, ProjectLoader.CardFolderName);
            if (!Directory.Exists(cardPath)) Directory.CreateDirectory(cardPath);
            
            if (entity is SectionViewModel section)
            {
                // Serialize this entity
                var outputPath = Path.Combine(sectionPath, $"{entity.Id}.yaml");
                var data = section.ToSerializable();
                var serializer = new YamlDotNet.Serialization.Serializer();
                File.WriteAllText(outputPath, serializer.Serialize(data));

                // Now serialize all of its children
                foreach (var child in section.Entities)
                {
                    child.Serialize(projectPath);
                }
            }
            else if (entity is IndexCardViewModel card)
            {
                var outputPath = Path.Combine(cardPath, $"{entity.Id}.md");

                var serializer = new YamlDotNet.Serialization.Serializer();
                var frontMatter = serializer.Serialize(card.Model);
                
                File.WriteAllText(outputPath, $"---\n{frontMatter}\n---\n{card.Content}");
            }
        }

        public static void ClearData(string projectPath)
        {
            var sectionPath = Path.Combine(projectPath, ProjectLoader.SectionFolderName);
            if (Directory.Exists(sectionPath)) Directory.Delete(sectionPath, true);
            
            var cardPath = Path.Combine(projectPath, ProjectLoader.CardFolderName);
            if (Directory.Exists(cardPath)) Directory.Delete(cardPath, true);
        }
        
        public static SerializableSection LoadSection(string file)
        {
            var deserializer = new YamlDotNet.Serialization.Deserializer();
            return deserializer.Deserialize<SerializableSection>(File.ReadAllText(file));
        }

        public static IndexCard? LoadIndexCard(string file)
        {
            var deserializer = new YamlDotNet.Serialization.Deserializer();
            var text = File.ReadAllText(file);
            var parts = text.Split("---", 3);

            // TODO: should this be logged?
            if (parts.Length < 3) return null;

            var cardInfo = deserializer.Deserialize<IndexCard>(parts[1]);
            cardInfo.Content = parts[2].Trim();
            return cardInfo;
        }

        public static SerializableSection ToSerializable(this SectionViewModel section)
        {
            var output = new SerializableSection
            {
                Id = section.Id,
                Summary = section.Summary,
                Notes = section.Notes,
                Label = section.Label,
            };

            foreach (var entity in section.Entities)
            {
                output.Children.Add(new SerializableSection.Child
                {
                    Id = entity.Id,
                    X = entity.Position.X,
                    Y = entity.Position.Y
                });
            }

            return output;
        }
        
    }
}