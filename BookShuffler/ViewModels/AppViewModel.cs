using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using BookShuffler.Models;
using BookShuffler.Parsing;
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
            this.RootItem = new ObservableCollection<IEntityView>();
        }

        public ObservableCollection<IEntityView> RootItem { get; }

        /// <summary>
        /// Gets the path of the actively loaded project. The application also uses this to determine whether or not
        /// a project is loaded in the interface.
        /// </summary>
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

        /// <summary>
        /// Gets whether or not there is an actively loaded project based on the value of ProjectPath
        /// </summary>
        public bool HasActiveProject => !string.IsNullOrEmpty(this.ProjectPath);

        /// <summary>
        /// Gets the title of the application window
        /// </summary>
        public string AppTitle => !this.HasActiveProject
            ? "Book Shuffler [no project]"
            : $"Book Shuffler [{ProjectPath}]";

        /// <summary>
        /// The actively selected *SectionView* element, which is the last item in the project tree which was clicked
        /// on that was also a SectionView and not an IndexCardView
        /// </summary>
        public SectionView ActiveSection
        {
            get => _activeSection;
            set => this.RaiseAndSetIfChanged(ref _activeSection, value);
        }

        /// <summary>
        /// The IEntityView selected in the project tree view
        /// </summary>
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

        /// <summary>
        /// Create a new project at the given project path
        /// </summary>
        /// <param name="path"></param>
        public void NewProject(string path)
        {
            this.ProjectPath = path;
            var model = new Entity
            {
                Id = Guid.NewGuid(),
                Summary = "Project Root"
            };
            _projectRoot = new SectionView(model);
            this.RootItem.Add(_projectRoot);
        }

        /// <summary>
        /// Import a tagged markdown file into the currently active project
        /// </summary>
        /// <param name="files"></param>
        public void ImportTaggedMarkdown(string[] files)
        {
            foreach (var file in files)
            {
                var parseResult = MarkdownParser.Parse(file);
                this.MergeLoadResult(parseResult);
            }
            
        }

        private void MergeLoadResult(ParseResult loaded)
        {
            // Load chapters and their entire descendant chain 
           foreach (var chapterId in loaded.Chapters)
           {
               var section = loaded.Sections.FirstOrDefault(s => s.Id == chapterId);
               
               // Now load this section and all of its children
               if (section != null)
                   _projectRoot.Entities.Add(LoadRecursive(section, loaded));

           }
        }

        private SectionView LoadRecursive(SectionEntity raw, ParseResult loaded)
        {
            var viewModel = new SectionView(raw);
            foreach (var id in raw.Children)
            {
                var sectionChild = loaded.Sections.FirstOrDefault(s => s.Id == id);
                if (sectionChild != null)
                {
                    viewModel.Entities.Add(LoadRecursive(sectionChild, loaded));
                    continue;
                }

                var cardChild = loaded.Cards.FirstOrDefault(c => c.Id == id);
                if (cardChild != null)
                {
                    viewModel.Entities.Add(new IndexCardView(cardChild));
                }

            }

            return viewModel;
        }
    }
}
