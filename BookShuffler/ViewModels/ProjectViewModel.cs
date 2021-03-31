using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using BookShuffler.Models;
using ReactiveUI;

namespace BookShuffler.ViewModels
{
    /// <summary>
    /// This view model represents an entire project, which consists of global project settings (root node, categories,
    /// etc), and the full list of all index cards and sections.
    /// </summary>
    public class ProjectViewModel : ViewModelBase
    {
        private ProjectInfo _info;
        private List<IEntityViewModel> _allEntities;
        private List<IDisposable> _entitySubscriptions;
        private bool _hasUnsavedChanges;

        public ProjectViewModel()
        {
            
        }

        /// <summary>
        /// Gets whether or not the project has unsaved changes
        /// </summary>
        public bool HasUnsavedChanges
        {
            get => _hasUnsavedChanges;
            set => this.RaiseAndSetIfChanged(ref _hasUnsavedChanges, value);
        }

        /// <summary>
        /// Gets the project information structure
        /// </summary>
        public ProjectInfo Info
        {
            get => _info;
            set => this.RaiseAndSetIfChanged(ref _info, value);
        }
        
        public ObservableCollection<IEntityViewModel> DetachedEntities { get; }
        public ObservableCollection<IEntityViewModel> RootEntity { get; }
        
        
    }
}