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

        private readonly Dictionary<Guid, IndexCard> _cards;
        private readonly Dictionary<Guid, SerializableSection> _sectionReps;
        private readonly Dictionary<Guid, SectionViewModel> _builtSections;
        private readonly Dictionary<Guid, IndexCardViewModel> _builtCards;
        private readonly IStorageProvider _storage;

        public ProjectLoader(IStorageProvider storage)
        {
            _storage = storage;
            _cards = new Dictionary<Guid, IndexCard>();
            _sectionReps = new Dictionary<Guid, SerializableSection>();
            _builtCards = new Dictionary<Guid, IndexCardViewModel>();
            _builtSections = new Dictionary<Guid, SectionViewModel>();
        }
        
        public LoadResult Load(string projectPath)
        {
            var result = new LoadResult();
            var projectFilePath = _storage.Join(projectPath, "project.yaml");
            var sectionPath = _storage.Join(projectPath, SectionFolderName);
            var cardPath = _storage.Join(projectPath, CardFolderName);

            var deserializer = new YamlDotNet.Serialization.Deserializer();
            result.Info = deserializer.Deserialize<ProjectInfo>(_storage.Get(projectFilePath));

            var reader = new EntityReader(_storage);

            foreach (var file in _storage.List(cardPath))
            {
                var cardInfo = reader.LoadIndexCard(file);
                if (cardInfo is not null)
                    _cards[cardInfo.Id] = cardInfo;
            }
            
            foreach (var file in _storage.List(sectionPath))
            {
                var item = reader.LoadSection(file);
                _sectionReps[item.Id] = item;
            }
            
            // Build the attached items
            result.Root = (SectionViewModel) LoadEntity(result.Info.RootId);
            
            // Build the unattached sections
            var remaining = _sectionReps.Keys.Where(k => !_builtSections.ContainsKey(k)).ToArray();
            while (remaining.Any())
            {
                // Find the ones with no parents
                var references = _sectionReps.Values
                    .SelectMany(r => r.Children, (section, child) => child.Id)
                    .ToHashSet();

                var orphans = remaining.Where(k => !references.Contains(k)).ToArray();

                foreach (var id in orphans)
                {
                    result.Unattached.Add(LoadEntity(id));
                }
                
                remaining = _sectionReps.Keys.Where(k => !_builtSections.ContainsKey(k)).ToArray();
            }
            
            // Build the unattached cards
            foreach (var card in _cards.Values)
            {
                if (!_builtCards.ContainsKey(card.Id))
                {
                    var viewModel = new IndexCardViewModel(_cards[card.Id]);
                    _builtCards[card.Id] = viewModel;
                    result.Unattached.Add(viewModel);
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

            foreach (var entity in _builtSections.Values)
            {
                result.AllEntities[entity.Id] = entity;
            }
            foreach (var entity in _builtCards.Values)
            {
                result.AllEntities[entity.Id] = entity;
            }
            return result;
        }

        private IEntityViewModel LoadEntity(Guid id)
        {
            if (_cards.ContainsKey(id))
            {
                var viewModel = new IndexCardViewModel(_cards[id]);
                _builtCards[id] = viewModel;
                return viewModel;
            }
            
            if (_sectionReps.ContainsKey(id))
            {
                var viewModel = new SectionViewModel(_sectionReps[id].ToEntity());
                _builtSections[id] = viewModel;

                foreach (var child in _sectionReps[id].Children)
                {
                    viewModel.Entities.Add(LoadEntity(child.Id));
                }

                return viewModel;
            }

            throw new KeyNotFoundException($"Id {id} was not found");
        }
        
    }
}