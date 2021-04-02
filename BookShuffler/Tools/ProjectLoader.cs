using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Avalonia;
using BookShuffler.Models;
using BookShuffler.Tools.Storage;
using BookShuffler.ViewModels;

namespace BookShuffler.Tools
{
    public class ProjectLoader
    {
        public const string SectionFolderName = "sections";
        public const string CardFolderName = "cards";

        private readonly Dictionary<Guid, SerializableSection> _sectionReps;
        private readonly Dictionary<Guid, SectionViewModel> _builtSections;
        private readonly IStorageProvider _storage;

        public ProjectLoader(IStorageProvider storage)
        {
            _storage = storage;
            _sectionReps = new Dictionary<Guid, SerializableSection>();
            _builtSections = new Dictionary<Guid, SectionViewModel>();
        }
        
        public LoadResult Load(string projectPath)
        {
            var result = new LoadResult {ProjectFolder = projectPath};
            var projectFilePath = _storage.Join(projectPath, "project.yaml");
            var sectionPath = _storage.Join(projectPath, SectionFolderName);
            var cardPath = _storage.Join(projectPath, CardFolderName);

            var deserializer = new YamlDotNet.Serialization.Deserializer();
            result.Info = deserializer.Deserialize<ProjectInfo>(_storage.Get(projectFilePath));

            var reader = new EntityReader(_storage);

            // Load all index cards from the index card directory and add them to the global dictionary of entity
            // view models.
            foreach (var file in _storage.List(cardPath))
            {
                var cardInfo = reader.LoadIndexCard(file);
                if (cardInfo is not null)
                    result.AllEntities[cardInfo.Id] = new IndexCardViewModel(cardInfo);
            }
            
            // Load all sections from the section directory and add them to both the global dictionary of entity
            // view models and the dictionary of section representations which will be used to add children later
            foreach (var file in _storage.List(sectionPath))
            {
                var item = reader.LoadSection(file);
                _sectionReps[item.Id] = item;
                var vm = new SectionViewModel(item.ToEntity());
                _builtSections[item.Id] = vm;
                result.AllEntities[item.Id] = vm;
            }
            
            // Identify the root node
            result.Root = _builtSections[result.Info.RootId];
            
            // Attach all children to all sections. We remove child IDs from the working set so that when we get to the
            // end all remaining IDs belong to top level unattached entities
            var working = result.AllEntities.Keys.ToHashSet();
            working.Remove(result.Info.RootId);
            foreach (var rep in _sectionReps.Values)
            {
                foreach (var child in rep.Children)
                {
                    if (working.Contains(child.Id))
                    {
                        working.Remove(child.Id);
                    }
                    else
                    {
                        // Error?
                    }
                    
                    _builtSections[rep.Id].Entities.Add(result.AllEntities[child.Id]);
                }
            }
            
            // Set the positions of everything
            foreach (var rep in _sectionReps.Values)
            {
                foreach (var child in rep.Children)
                {
                    var suspect = _builtSections[rep.Id].Entities
                        .FirstOrDefault(x => x.Id == child.Id);
                    if (suspect != null)
                        suspect.Position = new Point(child.X, child.Y);
                }
            }
            
            // Set unattached
            foreach (var id in working)
            {
                result.Unattached.Add(result.AllEntities[id]);
            }

            return result;
        }
       
    }
}