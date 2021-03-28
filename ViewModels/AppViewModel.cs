using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using Avalonia;
using Avalonia.Controls.Shapes;
using BookShuffler.Models;
using ReactiveUI;

namespace BookShuffler.ViewModels
{
    public class AppViewModel : ViewModelBase
    {
        private string? _projectPath;
        private SectionView _projectRoot;
        private SectionView _activeSection;
        private IEntityView? _selectedEntity;

        public AppViewModel()
        {
            var root = new Entity
            {
                Label = WorkflowLabel.ToDo,
                Summary = "This is the root level node",
            };
            var rootView = new SectionView(root) { Position = new Point(0, 0) };

            var cards = Enumerable.Range(0, 5).Select(i =>
                new IndexCard
                {
                    Id = Guid.NewGuid(),
                    Content = $"This is card {i}'s text content, which is a lot of smart stuff.",
                    Label = WorkflowLabel.InProgress,
                    Notes = $"These are some notes on card {i}",
                    Summary = $"Card {i} summary"
                });

            var r = new Random();
            foreach (var card in cards)
            {
                var vm = new IndexCardView(card) {Position = new Point(r.NextDouble() * 200, r.NextDouble() * 200)};
                rootView.Entities.Add(vm);
            }

            var chapter0 = new Entity
            {
                Summary = "Chapter SingleFancyWordName"
            };
            var chapterView0 = new SectionView(chapter0) {Position = new Point(400, 0)};
            
            var subCards = Enumerable.Range(0, 3).Select(i =>
                new IndexCard
                {
                    Id = Guid.NewGuid(),
                    Content = $"This is sub card {i}'s text content, which is a lot of smart stuff.",
                    Label = WorkflowLabel.InProgress,
                    Notes = $"These are some notes on card {i}",
                    Summary = $"Lower Card {i} summary"
                });

            foreach (var card in subCards)
            {
                var vm = new IndexCardView(card) {Position = new Point(r.NextDouble() * 200, r.NextDouble() * 200)};
                chapterView0.Entities.Add(vm);
            }
            rootView.Entities.Add(chapterView0);

            this.RootItem = new ObservableCollection<IEntityView> {rootView};
            this.ActiveSection = rootView;
        }

        public ObservableCollection<IEntityView> RootItem { get; }

        public string? ProjectPath
        {
            get => _projectPath;
            set
            {
                this.RaiseAndSetIfChanged(ref _projectPath, value); 
                this.RaisePropertyChanged(nameof(HasActiveProject));
                this.RaisePropertyChanged(nameof(AppTitle));
            }
        }

        public bool HasActiveProject => !string.IsNullOrEmpty(this.ProjectPath);

        public string AppTitle => this.HasActiveProject
            ? "Book Shuffler [no project]"
            : $"Book Shuffler {ProjectPath}";

        public SectionView ActiveSection
        {
            get => _activeSection;
            set => this.RaiseAndSetIfChanged(ref _activeSection, value);
        }

        public IEntityView? SelectedEntity
        {
            get => _selectedEntity;
            set
            {
                this.RaiseAndSetIfChanged(ref _selectedEntity, value);
                if (_selectedEntity is SectionView)
                {
                    this.ActiveSection = _selectedEntity as SectionView;
                }
            }
        }
    }
}
