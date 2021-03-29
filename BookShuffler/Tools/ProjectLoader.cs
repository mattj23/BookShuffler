using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Avalonia;
using BookShuffler.Models;
using BookShuffler.ViewModels;

namespace BookShuffler.Parsing
{
    public class ProjectLoader
    {
        private readonly Dictionary<Guid, IndexCard> _cards;
        private readonly Dictionary<Guid, SerializableSection> _sectionReps;
        private readonly Dictionary<Guid, SectionView> _builtSections;
        private readonly Dictionary<Guid, IndexCardView> _builtCards;

        public ProjectLoader()
        {
            _cards = new Dictionary<Guid, IndexCard>();
            _sectionReps = new Dictionary<Guid, SerializableSection>();
            _builtCards = new Dictionary<Guid, IndexCardView>();
            _builtSections = new Dictionary<Guid, SectionView>();
        }
        
        public LoadResult Load(string projectFilePath)
        {
            var result = new LoadResult();
            var fileInfo = new FileInfo(projectFilePath);
            var sectionPath = Path.Combine(fileInfo.DirectoryName, "sections");
            var cardPath = Path.Combine(fileInfo.DirectoryName, "cards");

            var deserializer = new YamlDotNet.Serialization.Deserializer();

            var info = deserializer.Deserialize<ProjectInfo>(File.ReadAllText(projectFilePath));

            foreach (var file in Directory.EnumerateFiles(cardPath))
            {
                var cardInfo = Serializer.LoadIndexCard(file);
                if (cardInfo is not null)
                    _cards[cardInfo.Id] = cardInfo;
            }
            
            foreach (var file in Directory.EnumerateFiles(sectionPath))
            {
                var item = Serializer.LoadSection(file);
                _sectionReps[item.Id] = item;
            }
            
            // Build the attached items
            result.Root = (SectionView) LoadEntity(info.RootId);
            
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
                    var viewModel = new IndexCardView(_cards[card.Id]);
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
            
            return result;
        }

        private IEntityView LoadEntity(Guid id)
        {
            if (_cards.ContainsKey(id))
            {
                var viewModel = new IndexCardView(_cards[id]);
                _builtCards[id] = viewModel;
                return viewModel;
            }
            
            if (_sectionReps.ContainsKey(id))
            {
                var viewModel = new SectionView(_sectionReps[id].ToEntity());
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