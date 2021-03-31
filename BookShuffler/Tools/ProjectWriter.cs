using System.Linq;
using BookShuffler.Models;
using BookShuffler.Tools.Storage;
using BookShuffler.ViewModels;

namespace BookShuffler.Tools
{
    public class ProjectWriter
    {
        private readonly IStorageProvider _storage;

        public ProjectWriter(IStorageProvider storage)
        {
            _storage = storage;
        }

        public void Save(ProjectViewModel project)
        {
            var writer = new EntityWriter(_storage);
            writer.ClearData(project.ProjectFolder);

            foreach (var entity in project.Entities.Values)
            {
                writer.Serialize(entity, project.ProjectFolder);
            }

            var info = new ProjectInfo
            {
                RootId = project.Root.Id,
                Categories = project.Categories.All.Select(c => c.Model).ToList()
            };
            var projectFile = _storage.Join(project.ProjectFolder, "project.yaml");
            
            var serializer = new YamlDotNet.Serialization.Serializer();
            _storage.Put(projectFile, serializer.Serialize(info));
        }
    }
}