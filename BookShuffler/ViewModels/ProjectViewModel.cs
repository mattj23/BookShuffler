using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using BookShuffler.Models;
using BookShuffler.Tools;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;
using ReactiveUI;

namespace BookShuffler.ViewModels
{
    /// <summary>
    /// This view model represents an entire project, which consists of global project settings (root node, categories,
    /// etc), and the full list of all index cards and sections.
    /// </summary>
    public class ProjectViewModel : ViewModelBase, IDisposable
    {
        private readonly SectionViewModel _root;
        private readonly Dictionary<Guid, IEntityViewModel> _allEntities;
        private readonly List<IDisposable> _entitySubscriptions;
        private bool _hasUnsavedChanges;

        private ProjectViewModel(string projectFolder, SectionViewModel root, IEnumerable<Category> categories)
        {
            this.ProjectFolder = projectFolder;
            _root = root;
            
            this.DetachedEntities = new ObservableCollection<IEntityViewModel>();
            this.RootEntity = new ObservableCollection<IEntityViewModel>{_root};
            this.Categories = new ProjectCategories(categories);

            _allEntities = new Dictionary<Guid, IEntityViewModel>{{_root.Id, root}};
            _entitySubscriptions = new List<IDisposable>();
        }

        public string ProjectFolder { get; }

        public SectionViewModel Root => _root;

        /// <summary>
        /// Gets whether or not the project has unsaved changes
        /// </summary>
        public bool HasUnsavedChanges
        {
            get => _hasUnsavedChanges;
            set => this.RaiseAndSetIfChanged(ref _hasUnsavedChanges, value);
        }

        public ObservableCollection<IEntityViewModel> DetachedEntities { get; }
        public ObservableCollection<IEntityViewModel> RootEntity { get; }

        public ProjectCategories Categories { get; }

        public IReadOnlyDictionary<Guid, IEntityViewModel> Entities => _allEntities;

        /// <summary>
        /// Create a new project in a specified folder
        /// </summary>
        /// <param name="projectFolder"></param>
        /// <returns></returns>
        public static ProjectViewModel New(string projectFolder)
        {
            var root = new Entity {Summary = "Project Root", Id = Guid.NewGuid()};
            var categories = new Category[]
            {
                new Category()
                {
                    ColorName = "White",
                    Id = 0,
                    Name = "Default"
                }
            };
            return new ProjectViewModel(projectFolder, new SectionViewModel(root), categories) {HasUnsavedChanges = true};
        }

        /// <summary>
        /// Create a project from a LoadResult
        /// </summary>
        /// <param name="loaded"></param>
        /// <returns></returns>
        public static ProjectViewModel FromLoad(LoadResult loaded)
        {
            if (loaded.ProjectFolder is null)
                throw new ArgumentException("ProjectViewModel must be created by a LoadResult that has a path");

            var project = new ProjectViewModel(loaded.ProjectFolder, loaded.Root, loaded.Info.Categories);
            project.Merge(loaded);
            project.HasUnsavedChanges = false;
            return project;
        }

        /// <summary>
        /// Merge a LoadResult into the project. The root node of the current project will be preserved, and
        /// any child nodes of the loaded root will be detached from it and reattached to the current project
        /// root. 
        /// </summary>
        /// <param name="loaded"></param>
        public void Merge(LoadResult loaded)
        {
            if (loaded.Root != this._root)
            {
                foreach (var entity in loaded.AllEntities.Values)
                {
                    this.RegisterEntity(entity);
                }
            }

            foreach (var child in loaded.Root.Entities)
            {
                this._root.Entities.Add(child);
            }

            foreach (var entity in loaded.Unattached)
            {
                this.DetachedEntities.Add(entity);
            }
        }

        private void RegisterEntity(IEntityViewModel viewModel)
        {
            if (viewModel is IndexCardViewModel cardViewModel)
            {
                // If this is an index card we will attach the project's categories to it
                cardViewModel.ProjectCategories = this.Categories;
            }

            _allEntities[viewModel.Id] = viewModel;
            _entitySubscriptions.Add(viewModel.WhenAnyValue(e => e.Summary)
                .Subscribe(_ => this.HasUnsavedChanges = true));
            _entitySubscriptions.Add(viewModel.WhenAnyValue(e => e.Position)
                .Subscribe(_ => this.HasUnsavedChanges = true));
        }

        public void Dispose()
        {
            foreach (var d in _entitySubscriptions)
            {
                d.Dispose();
            }
        }
    }
}