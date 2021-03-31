using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using BookShuffler.Models;
using BookShuffler.Parsing;
using Microsoft.CodeAnalysis.CSharp.Syntax;
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
        private readonly List<IEntityViewModel> _allEntities;
        private readonly List<IDisposable> _entitySubscriptions;
        private bool _hasUnsavedChanges;

        private ProjectViewModel(string projectFolder, SectionViewModel root)
        {
            this.ProjectFolder = projectFolder;
            _root = root;
            
            this.DetachedEntities = new ObservableCollection<IEntityViewModel>();
            this.RootEntity = new ObservableCollection<IEntityViewModel>{_root};
            this.Categories = new ObservableCollection<Category>();

            _allEntities = new List<IEntityViewModel>();
            _entitySubscriptions = new List<IDisposable>();
        }

        public string ProjectFolder { get; }

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
        public ObservableCollection<Category> Categories { get; }

        /// <summary>
        /// Create a new project in a specified folder
        /// </summary>
        /// <param name="projectFolder"></param>
        /// <returns></returns>
        public static ProjectViewModel New(string projectFolder)
        {
            var root = new Entity {Summary = "Project Root", Id = Guid.NewGuid()};
            return new ProjectViewModel(projectFolder, new SectionViewModel(root)) {HasUnsavedChanges = true};
        }

        /// <summary>
        /// Open a project view model from a specified project file
        /// </summary>
        /// <param name="projectFile"></param>
        /// <returns></returns>
        public static ProjectViewModel Open(string projectFile)
        {
            var loader = new ProjectLoader();
            var result = loader.Load(projectFile);
            var folder = new FileInfo(projectFile).DirectoryName;

            var project = new ProjectViewModel(folder, result.Root);

            foreach (var entity in result.AllEntities)
            {
                project.RegisterEntity(entity);
            }

            foreach (var entity in result.Unattached)
            {
                project.DetachedEntities.Add(entity);
            }

            foreach (var category in result.Info.Categories)
            {
                project.Categories.Add(category);
            }

            return project;
        }

        private void RegisterEntity(IEntityViewModel viewModel)
        {
            _allEntities.Add(viewModel);
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