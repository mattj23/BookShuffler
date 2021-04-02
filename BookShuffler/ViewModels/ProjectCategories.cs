using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Windows.Input;
using Avalonia.Media;
using BookShuffler.Models;
using DynamicData;
using ReactiveUI;

namespace BookShuffler.ViewModels
{
    public interface IProjectCategories
    {
        ReadOnlyObservableCollection<CategoryViewModel> All { get; }
        IReadOnlyDictionary<int, CategoryViewModel> ById { get; }
    }

    /// <summary>
    /// Represents a collection of categories attached to a project
    /// </summary>
    public class ProjectCategories : ViewModelBase, IProjectCategories
    {
        private readonly ObservableCollection<CategoryViewModel> _categories;
        private readonly Dictionary<int, CategoryViewModel> _byId;
        private CategoryViewModel _selectedCategory;

        public ProjectCategories() : this(Enumerable.Empty<CategoryViewModel>()) {}
        
        public ProjectCategories(IEnumerable<CategoryViewModel> categories)
        {
            _categories = new ObservableCollection<CategoryViewModel>();
            _byId = new Dictionary<int, CategoryViewModel>();
            this.All = new ReadOnlyObservableCollection<CategoryViewModel>(_categories);

            foreach (var cat in categories)
            {
                _categories.Add(cat);
                _byId[cat.Id] = cat;
            }

            this.Colors = new ObservableCollection<string>();
            foreach (var info in typeof(Avalonia.Media.Colors).GetProperties())
            {
                this.Colors.Add(info.Name);
            }
            
            // Set up the commands
            this.CreateNew = ReactiveCommand.Create(() =>
            {
                var id = this._categories.Max(c => c.Id) + 1;
                var cat = new CategoryViewModel(new Category {Id = id, ColorName = "White", Name = "New Category"});
                _categories.Add(cat);
                _byId[cat.Id] = cat;
            });

            this.Delete = ReactiveCommand.Create(() =>
            {
                if (this.SelectedCategory is null) return;
                var remove = this.SelectedCategory;
                _byId.Remove(remove.Id);
                _categories.Remove(remove);
            }, this.WhenAnyValue(p => p.SelectedCategory).Select(c => c is not null));
        }
        
        public ICommand CreateNew { get; }
        public ICommand Delete { get; }
        
        public ObservableCollection<string> Colors { get; }

        public ReadOnlyObservableCollection<CategoryViewModel> All { get; }
        public IReadOnlyDictionary<int, CategoryViewModel> ById => _byId;

        public CategoryViewModel SelectedCategory
        {
            get => _selectedCategory;
            set => this.RaiseAndSetIfChanged(ref _selectedCategory, value);
        }
    }
}