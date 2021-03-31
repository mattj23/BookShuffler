using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace BookShuffler.ViewModels
{
    public interface IProjectCategories
    {
        ReadOnlyObservableCollection<Category> All { get; }
        IReadOnlyDictionary<int, Category> ById { get; }
    }

    /// <summary>
    /// Represents a collection of categories attached to a project
    /// </summary>
    public class ProjectCategories : IProjectCategories
    {
        private readonly ObservableCollection<Category> _categories;
        private readonly Dictionary<int, Category> _byId;

        public ProjectCategories(IEnumerable<Category> categories)
        {
            _categories = new ObservableCollection<Category>();
            _byId = new Dictionary<int, Category>();
            this.All = new ReadOnlyObservableCollection<Category>(_categories);

            foreach (var cat in categories)
            {
                _categories.Add(cat);
                _byId[cat.Id] = cat;
            }
        }

        public ReadOnlyObservableCollection<Category> All { get; }
        public IReadOnlyDictionary<int, Category> ById => _byId;
    }
}