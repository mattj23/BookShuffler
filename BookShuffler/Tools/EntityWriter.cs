using BookShuffler.Models;
using BookShuffler.Tools.Storage;
using BookShuffler.ViewModels;

namespace BookShuffler.Tools
{
    public class EntityWriter
    {
        private readonly IStorageProvider _storage;

        public EntityWriter(IStorageProvider storage)
        {
            _storage = storage;
        }

        public void Serialize(IEntityViewModel entity, string projectPath)
        {
            var sectionPath = _storage.Join(projectPath, ProjectLoader.SectionFolderName);
            var cardPath = _storage.Join(projectPath, ProjectLoader.CardFolderName);
            
            if (entity is SectionViewModel section)
            {
                // Serialize this entity
                var outputPath = _storage.Join(sectionPath, $"{entity.Id}.yaml");
                var data = ToSerializable(section);
                var serializer = new YamlDotNet.Serialization.Serializer();

                _storage.Put(outputPath, serializer.Serialize(data));
            }
            else if (entity is IndexCardViewModel card)
            {
                var outputPath = _storage.Join(cardPath, $"{entity.Id}.md");

                var serializer = new YamlDotNet.Serialization.Serializer();
                var frontMatter = serializer.Serialize(card.Model);
                
                _storage.Put(outputPath, $"---\n{frontMatter}\n---\n{card.Content}");
            }
        }

        public void ClearData(string projectPath)
        {
            var sectionPath = _storage.Join(projectPath, ProjectLoader.SectionFolderName);
            _storage.Delete(sectionPath);
            
            var cardPath = _storage.Join(projectPath, ProjectLoader.CardFolderName);
            _storage.Delete(cardPath);
        }
        

        public static SerializableSection ToSerializable(SectionViewModel section)
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