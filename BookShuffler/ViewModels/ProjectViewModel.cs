using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Xml.XPath;
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

        private ProjectViewModel(string projectFolder, SectionViewModel root, IEnumerable<CategoryViewModel> categories)
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
            var categories = new[]
            {
                new CategoryViewModel(new Category()
                {
                    ColorName = "White",
                    Id = 0,
                    Name = "Default"
                })
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
            var categories = loaded.Info.Categories.Select(c => new CategoryViewModel(c));
            var project = new ProjectViewModel(loaded.ProjectFolder, loaded.Root, categories);
            project.Merge(loaded);
            project.HasUnsavedChanges = false;
            return project;
        }

        public IndexCardViewModel AddNewCard(SectionViewModel parent, IndexCardViewModel child)
        {
            if (_allEntities.ContainsKey(child.Id)) 
                throw new ArgumentException("Child already exists in the project");

            parent.Entities.Add(child);
            this.RegisterEntity(child);

            return child;
        }

        public SectionViewModel AddNewSection(SectionViewModel parent, SectionViewModel child)
        {
            if (_allEntities.ContainsKey(child.Id)) 
                throw new ArgumentException("Child already exists in the project");

            parent.Entities.Add(child);
            this.RegisterEntity(child);

            return child;
        }

        /// <summary>
        /// Merge a LoadResult into the project. The root node of the current project will be preserved, and
        /// any child nodes of the loaded root will be detached from it and reattached to the current project
        /// root. 
        /// </summary>
        /// <param name="loaded"></param>
        public void Merge(LoadResult loaded)
        {
            if (loaded.Root != _root)
                foreach (var child in loaded.Root.Entities)
                    _root.Entities.Add(child);

            foreach (var entity in loaded.AllEntities.Values) RegisterEntity(entity);
            foreach (var entity in loaded.Unattached) DetachedEntities.Add(entity);
        }

        public IEntityViewModel DetachEntity(IEntityViewModel? entity)
        {
            if (entity is null) return null;
            if (entity == _root) return null;
            
            if (!_allEntities.ContainsKey(entity.Id)) throw new ArgumentException("Entity isn't part of this project");

            var parent = this.BruteForceFindParent(entity.Id, this.Root);
            if (parent is null) throw new ArgumentException("Could not find parent of entity");

            parent.Entities.Remove(entity);
            this.DetachedEntities.Add(entity);
            return entity;
        }

        public IEntityViewModel? AttachEntity(IEntityViewModel? entity, SectionViewModel? newParent)
        {
            if (entity is null) return null;
            if (newParent is null) return null;
            
            // Find the entity in the detached
            var located = this.RemoveFromDetached(entity);
            if (located is null) return null;
            
            newParent.Entities.Add(entity);
            return entity;
        }

        /// <summary>
        /// Attempt to find the parent section of an entity in the attached project tree.
        /// </summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        public SectionViewModel? GetParent(IEntityViewModel entity)
        {
            return this.BruteForceFindParent(entity.Id, this.Root);
        }

        /// <summary>
        /// Registers an entity to the project which the project does not currently have. This subscribes to
        /// change notifications (for detecting unsaved changes) and attaches the project categories to any
        /// index cards.
        /// </summary>
        /// <param name="viewModel"></param>
        private void RegisterEntity(IEntityViewModel viewModel)
        {
            if (viewModel is IndexCardViewModel cardViewModel)
            {
                // If this is an index card we will attach the project's categories to it
                cardViewModel.ProjectCategories = this.Categories;
                _entitySubscriptions.Add(cardViewModel.WhenAnyValue(e => e.Category)
                    .Subscribe(_ => this.HasUnsavedChanges = true));
            }

            _allEntities[viewModel.Id] = viewModel;
            _entitySubscriptions.Add(viewModel.WhenAnyValue(e => e.Summary)
                .Subscribe(_ => this.HasUnsavedChanges = true));
            _entitySubscriptions.Add(viewModel.WhenAnyValue(e => e.Position)
                .Subscribe(_ => this.HasUnsavedChanges = true));
            _entitySubscriptions.Add(viewModel.DetachRequest.Subscribe(e => this.DetachEntity(e)));
        }

        /// <summary>
        /// Find the given entity in the detached collection and remove it. The entity may be a top level member of
        /// the collection or may be a child of one of the top level members.
        /// </summary>
        /// <param name="entity"></param>
        /// <returns>the removed entity, or null if none was found</returns>
        private IEntityViewModel? RemoveFromDetached(IEntityViewModel entity)
        {
            if (this.DetachedEntities.Contains(entity))
            {
                this.DetachedEntities.Remove(entity);
                return entity;
            }

            foreach (var possible in this.DetachedEntities)
            {
                var parent = this.BruteForceFindParent(entity.Id, possible);
                if (parent is null) continue;
                parent.Entities.Remove(entity);
                return entity;
            }

            return null;
        }

        /// <summary>
        /// Does what it says.  Not optimal, but will have to do for now.
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        private SectionViewModel? BruteForceFindParent(Guid id, IEntityViewModel possible)
        {
            if (possible is IndexCardViewModel) return null;

            if (possible is SectionViewModel sec)
            {
                foreach (var entity in sec.Entities)
                {
                    if (entity.Id == id)
                    {
                        return sec;
                    }

                    var parent = BruteForceFindParent(id, entity);
                    if (parent is not null)
                        return parent;
                }
            }

            return null;
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