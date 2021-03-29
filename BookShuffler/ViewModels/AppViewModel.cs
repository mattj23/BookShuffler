using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Input;
using Avalonia;
using Avalonia.Controls.Shapes;
using BookShuffler.Models;
using BookShuffler.Parsing;
using ReactiveUI;
using YamlDotNet.RepresentationModel;
using Path = System.IO.Path;

namespace BookShuffler.ViewModels
{
    public class AppViewModel : ViewModelBase
    {
        private string? _projectPath;
        private SectionView _projectRoot;
        private SectionView _activeSection;
        private IEntityView? _selectedEntity;
        private Rectangle _canvasBounds;
        private double _canvasTop;
        private double _canvasLeft;
        private IEntityView? _selectedDetachedEntity;

        public AppViewModel()
        {
            this.RootItem = new ObservableCollection<IEntityView>();
            this.Unattached = new ObservableCollection<IEntityView>();

            this.SaveProjectCommand = ReactiveCommand.Create(this.SaveProject);
            this.DetachSelectedCommand = ReactiveCommand.Create(this.DetachSelected);
            this.AttachSelectedCommand = ReactiveCommand.Create(this.AttachSelected);
            this.LoadSettings();

            if (File.Exists(this.Settings.LastOpenedProject))
            {
                this.OpenProject(this.Settings.LastOpenedProject);
            }
        }
        
        public AppSettings Settings { get; private set; }
        
        public ICommand SaveProjectCommand { get; }
        
        public ICommand DetachSelectedCommand { get; }
        
        public ICommand AttachSelectedCommand { get; }
        
        /// <summary>
        /// Gets a function which returns the canvas boundaries. This is necessary because binding OneWayToSource on
        /// a property in the XAML is currently broken in Avalonia.
        /// </summary>
        public Func<Rect> GetCanvasBounds { get; set; }

        /// <summary>
        /// Gets a collection that should contain only the single root element of the project.
        /// </summary>
        public ObservableCollection<IEntityView> RootItem { get; }
        
        /// <summary>
        /// Gets a collection of all of the unattached entities in the project
        /// </summary>
        public ObservableCollection<IEntityView> Unattached { get; }

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
        public SectionView? ActiveSection
        {
            get => _activeSection;
            private set => this.RaiseAndSetIfChanged(ref _activeSection, value);
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

        public IEntityView? SelectedDetachedEntity
        {
            get => _selectedDetachedEntity;
            set => this.RaiseAndSetIfChanged(ref _selectedDetachedEntity, value);
        }

        public void LoadSettings()
        {
            var folder = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "BookShuffler");
            var filePath = System.IO.Path.Combine(folder, "settings.yaml");
            if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);
            
            if (File.Exists(filePath))
            {
                var des = new YamlDotNet.Serialization.Deserializer();
                this.Settings = des.Deserialize<AppSettings>(File.ReadAllText(filePath));
            }
            else
            {
                this.Settings = new AppSettings();
            }
        }

        public void SaveSettings()
        {
            var filePath = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "BookShuffler", "settings.yaml");
            var ser = new YamlDotNet.Serialization.Serializer();
            File.WriteAllText(filePath, ser.Serialize(this.Settings));
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

            this.Settings.LastOpenedProject = path;
            this.SaveSettings();
        }

        public void OpenProject(string path)
        {
            this.RootItem.Clear();
            this.Unattached.Clear();
            
            
            var loader = new ProjectLoader();
            var result = loader.Load(path);

            _projectRoot = result.Root;
            this.RootItem.Add(_projectRoot);
            foreach (var unattached in result.Unattached)
            {
                this.Unattached.Add(unattached);
            }

            this.ProjectPath = new FileInfo(path).DirectoryName;
            this.Settings.LastOpenedProject = path;
            this.SaveSettings();
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

        public void ResortActiveSection()
        {
            this.ActiveSection?.ResortOrder();
        }

        private void MergeLoadResult(ParseResult loaded)
        {
            // Load chapters and their entire descendant chain 
           foreach (var chapterId in loaded.Chapters)
           {
               var section = loaded.Sections.FirstOrDefault(s => s.Id == chapterId);
               
               // Now load this section and all of its children
               if (section != null)
               {
                    var newSection = LoadRecursive(section, loaded);
                    newSection.AutoTile(this.GetCanvasBounds?.Invoke().Width ?? 1200);
                   _projectRoot.Entities.Add(newSection);
               }

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
                    var newSection = LoadRecursive(sectionChild, loaded);
                    newSection.AutoTile(this.GetCanvasBounds?.Invoke().Width ?? 1200);
                    viewModel.Entities.Add(newSection);
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

        private void SaveProject()
        {
            var projectPath = this.ProjectPath;
            if (projectPath != null)
            {
                _projectRoot.Serialize(projectPath);
                var outputFile = System.IO.Path.Combine(projectPath, "project.yaml");
                var serializer = new YamlDotNet.Serialization.Serializer();
                File.WriteAllText(outputFile, serializer.Serialize(new ProjectInfo
                {
                    RootId = _projectRoot.Id,
                }));
            }
        }

        private void DetachSelected()
        {
            if (this.SelectedEntity is null) return;
            if (this.SelectedEntity == _projectRoot) return;

            var parent = this.BruteForceFindParent(this.SelectedEntity.Id, _projectRoot);

            if (parent is not null)
            {
                parent.Entities.Remove(this.SelectedEntity);
                this.Unattached.Add(this.SelectedEntity);
                this.SelectedEntity = parent;
            }
        }

        /// <summary>
        /// Does what it says.  Not optimal, but will have to do for now.
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        private SectionView? BruteForceFindParent(Guid id, IEntityView possible)
        {
            if (possible is IndexCardView) return null;

            if (possible is SectionView sec)
            {
                foreach (var entity in sec.Entities)
                {
                    if (entity.Id == id) { return sec; }
                    
                    var parent = BruteForceFindParent(id, entity);
                    if (parent is not null)
                        return parent;
                }
            }

            return null;
        }

        private void AttachSelected()
        {
            if (this.SelectedDetachedEntity is null) return;
            if (this.ActiveSection is null) return;

            var working = this.SelectedDetachedEntity;
            if (this.Unattached.Contains(working))
            {
                this.Unattached.Remove(working);
            }
            else
            {
                foreach (var entity in this.Unattached)
                {
                    var parent = this.BruteForceFindParent(working.Id, entity);
                    if (parent is null) continue;
                    parent.Entities.Remove(working);
                    break;
                }
            }
            
            this.ActiveSection.Entities.Add(working);
            this.SelectedDetachedEntity = null;
        }
        
    }
}
