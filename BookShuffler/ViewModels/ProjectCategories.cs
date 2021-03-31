using System.Collections.Generic;
using System.Collections.ObjectModel;

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
    public class ProjectCategories : IProjectCategories
    {
        private readonly ObservableCollection<CategoryViewModel> _categories;
        private readonly Dictionary<int, CategoryViewModel> _byId;

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
        }

        public ReadOnlyObservableCollection<CategoryViewModel> All { get; }
        public IReadOnlyDictionary<int, CategoryViewModel> ById => _byId;
    }
}