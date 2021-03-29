using System.Collections.Specialized;
using System.IO;
using BookShuffler.ViewModels;

namespace BookShuffler.Parsing
{
    public static class Serializer
    {
        public static void Serialize(this IEntityView entity, string projectPath)
        {
            var sectionPath = Path.Combine(projectPath, "sections");
            if (!Directory.Exists(sectionPath)) Directory.CreateDirectory(sectionPath);
            
            var cardPath = Path.Combine(projectPath, "cards");
            if (!Directory.Exists(cardPath)) Directory.CreateDirectory(cardPath);
            
            if (entity is SectionView section)
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
            else if (entity is IndexCardView card)
            {
                var outputPath = Path.Combine(cardPath, $"{entity.Id}.md");

                var serializer = new YamlDotNet.Serialization.Serializer();
                var frontMatter = serializer.Serialize(card.Model);
                
                File.WriteAllText(outputPath, $"---\n{frontMatter}\n---\n{card.Content}");
            }
        }

        public static SerializableSection ToSerializable(this SectionView section)
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