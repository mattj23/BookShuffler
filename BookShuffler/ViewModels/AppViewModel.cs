using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Reactive.Subjects;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Input;
using Avalonia;
using Avalonia.Controls.Shapes;
using BookShuffler.Models;
using BookShuffler.Tools;
using BookShuffler.Tools.MarkdownImport;
using ReactiveUI;
using YamlDotNet.RepresentationModel;
using Path = System.IO.Path;

namespace BookShuffler.ViewModels
{
    public class AppViewModel : ViewModelBase
    {
        private string? _projectPath;
        private SectionViewModel _projectRoot;
        private SectionViewModel _activeSection;
        private IEntityViewModel? _selectedEntity;
        private double _canvasTop;
        private double _canvasLeft;
        private IEntityViewModel? _selectedDetachedEntity;

        private readonly BehaviorSubject<bool> _selectedIsSectionSubject;
        private double _canvasScale;
        private double _treeScale;
        private bool _hasUnsavedChanges;
        private readonly List<IEntityViewModel> _allEntities = new List<IEntityViewModel>();
        private readonly List<IDisposable> _entitySubscriptions = new List<IDisposable>();
        private ProjectViewModel? _project;

        public AppViewModel()
        {
            _selectedIsSectionSubject = new BehaviorSubject<bool>(false);

            this.RootItem = new ObservableCollection<IEntityViewModel>();
            this.Unattached = new ObservableCollection<IEntityViewModel>();

            // this.SaveProjectCommand = ReactiveCommand.Create(this.SaveProject);
            this.DetachSelectedCommand = ReactiveCommand.Create(this.DetachSelected);
            this.AttachSelectedCommand = ReactiveCommand.Create(this.AttachSelected);
            this.LaunchEditorCommand = ReactiveCommand.Create(this.LaunchEditorOnSelected);
            this.LoadFromFileCommand = ReactiveCommand.Create(this.LoadSelectedFromFile);
            this.DeleteEntityCommand = ReactiveCommand.Create(this.DeleteSelected);

            this.CreateCardCommand =
                ReactiveCommand.Create(this.AddCardToSelected, _selectedIsSectionSubject);
            this.CreateSectionCommand =
                ReactiveCommand.Create(this.AddSectionToSelected, _selectedIsSectionSubject);

            this.SetCanvasScale = ReactiveCommand.Create<string>(d => this.SetCanvasScaleValue(double.Parse(d)));
            this.SetTreeScale = ReactiveCommand.Create<string>(d => this.SetTreeScaleValue(double.Parse(d)));

            this.AutoTileActiveSectionCommand = ReactiveCommand.Create(this.AutoTileActiveSection);

            this.CanvasScale = 1;
            this.TreeScale = 1;

            this.LoadSettings();

            // if (File.Exists(this.Settings.LastOpenedProject))
            // {
            //     this.OpenProject(this.Settings.LastOpenedProject);
            // }
        }

        public AppSettings Settings { get; private set; }

        public ProjectViewModel? Project
        {
            get => _project;
            set => this.RaiseAndSetIfChanged(ref _project, value);
        }

        public ICommand SetCanvasScale { get; }
        public ICommand SetTreeScale { get; }

        public ICommand SaveProjectCommand { get; }

        public ICommand DetachSelectedCommand { get; }

        public ICommand DeleteEntityCommand { get; }
        public ICommand LaunchEditorCommand { get; }
        public ICommand LoadFromFileCommand { get; }
        public ICommand AttachSelectedCommand { get; }
        public ICommand CreateCardCommand { get; }
        public ICommand CreateSectionCommand { get; }

        public ICommand AutoTileActiveSectionCommand { get; }
        public double CanvasScale
        {
            get => _canvasScale;
            set => this.RaiseAndSetIfChanged(ref _canvasScale, value);
        }

        public double TreeScale
        {
            get => _treeScale;
            set => this.RaiseAndSetIfChanged(ref _treeScale, value);
        }

        public bool HasUnsavedChanges
        {
            get => _hasUnsavedChanges;
            set
            {
                if (value == _hasUnsavedChanges) return;
                _hasUnsavedChanges = value;
                this.RaisePropertyChanged(nameof(HasUnsavedChanges));
                this.RaisePropertyChanged(nameof(AppTitle));
            }
        }

        /// <summary>
        /// Gets a function which returns the canvas boundaries. This is necessary because binding OneWayToSource on
        /// a property in the XAML is currently broken in Avalonia.
        /// </summary>
        public Func<Rect> GetCanvasBounds { get; set; }

        /// <summary>
        /// Gets a collection that should contain only the single root element of the project.
        /// </summary>
        public ObservableCollection<IEntityViewModel> RootItem { get; }

        /// <summary>
        /// Gets a collection of all of the unattached entities in the project
        /// </summary>
        public ObservableCollection<IEntityViewModel> Unattached { get; }

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
        public string AppTitle =>
            (!this.HasActiveProject ? "Book Shuffler [no project]" : $"Book Shuffler [{ProjectPath}]") +
            (this.HasUnsavedChanges ? " (unsaved changes)" : string.Empty);

        /// <summary>
        /// The actively selected *SectionView* element, which is the last item in the project tree which was clicked
        /// on that was also a SectionView and not an IndexCardView
        /// </summary>
        public SectionViewModel? ActiveSection
        {
            get => _activeSection;
            private set => this.RaiseAndSetIfChanged(ref _activeSection, value);
        }

        /// <summary>
        /// The IEntityView selected in the project tree view
        /// </summary>
        public IEntityViewModel? SelectedEntity
        {
            get => _selectedEntity;
            set
            {
                this.RaiseAndSetIfChanged(ref _selectedEntity, value);
                _selectedIsSectionSubject.OnNext(_selectedEntity is SectionViewModel);
                if (_selectedEntity is null) return;

                if (_selectedEntity is SectionViewModel view)
                {
                    // If this is a section, we also set it as the active section so that the 2D editing pane
                    // now shows the children of this section
                    this.ActiveSection = view;
                }
                else
                {
                    // Otherwise this is an index card
                    // var parent = this.BruteForceFindParent(_selectedEntity.Id, _projectRoot);
                    // if (parent is not null) this.ActiveSection = parent;
                    this.ActiveSection?.BringChildToFront(_selectedEntity);
                }
            }
        }

        public IEntityViewModel? SelectedDetachedEntity
        {
            get => _selectedDetachedEntity;
            set => this.RaiseAndSetIfChanged(ref _selectedDetachedEntity, value);
        }

        /// <summary>
        /// Import a tagged markdown file into the currently active project
        /// </summary>
        /// <param name="files"></param>
        public void ImportTaggedMarkdown(string[] files)
        {
            // foreach (var file in files)
            // {
            //     var parseResult = MarkdownParser.Parse(file);
            //     this.MergeLoadResult(parseResult);
            // }
        }

        public void ResortActiveSection()
        {
            this.ActiveSection?.ResortOrder();
        }

        private void AutoTileActiveSection()
        {
            this.ActiveSection?.AutoTile(this.GetCanvasBounds?.Invoke().Width ?? 1200);
        }

        private void LoadSettings()
        {
            var folder = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "BookShuffler");
            var filePath = System.IO.Path.Combine(folder, "settings.yaml");
            if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);

            if (File.Exists(filePath))
            {
                var des = new YamlDotNet.Serialization.Deserializer();
                this.Settings = des.Deserialize<AppSettings>(File.ReadAllText(filePath));
                
                // Apply the loaded settings
                if (this.Settings.CanvasScale is not null) this.CanvasScale = this.Settings.CanvasScale.Value;
                if (this.Settings.ProjectTreeScale is not null) this.CanvasScale = this.Settings.ProjectTreeScale.Value;
            }
            else
            {
                this.Settings = new AppSettings();
            }
        }

        private void SaveSettings()
        {
            var filePath = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "BookShuffler", "settings.yaml");
            var ser = new YamlDotNet.Serialization.Serializer();
            File.WriteAllText(filePath, ser.Serialize(this.Settings));
        }


        private void DetachSelected()
        {
            if (this.SelectedEntity is null) return;
            if (this.SelectedEntity == _projectRoot) return;

            // var parent = this.BruteForceFindParent(this.SelectedEntity.Id, _projectRoot);

            // if (parent is not null)
            // {
            //     parent.Entities.Remove(this.SelectedEntity);
            //     this.Unattached.Add(this.SelectedEntity);
            //     this.SelectedEntity = parent;
            // }
        }

        private void RemoveFromDetached(IEntityViewModel target)
        {
            // if (this.Unattached.Contains(target))
            // {
            //     this.Unattached.Remove(target);
            // }
            // else
            // {
            //     foreach (var entity in this.Unattached)
            //     {
            //         var parent = this.BruteForceFindParent(target.Id, entity);
            //         if (parent is null) continue;
            //         parent.Entities.Remove(target);
            //         break;
            //     }
            // }
        }

        private void AttachSelected()
        {
            if (this.SelectedDetachedEntity is null) return;
            if (this.ActiveSection is null) return;

            var working = this.SelectedDetachedEntity;
            this.RemoveFromDetached(working);

            working.Position = new Point(0, 0);
            this.ActiveSection.Entities.Add(working);
            this.ActiveSection.BringChildToFront(working);
            this.SelectedDetachedEntity = null;
        }

        private void AddCardToSelected()
        {
            var card = new IndexCard
            {
                Id = Guid.NewGuid(),
                Label = WorkflowLabel.ToDo,
                Notes = string.Empty,
                Summary = "Enter Card Summary",
                Content = string.Empty
            };

            this.ActiveSection?.Entities.Add(new IndexCardViewModel(card));
        }

        private void AddSectionToSelected()
        {
            var entity = new Entity()
            {
                Id = Guid.NewGuid(),
                Label = WorkflowLabel.ToDo,
                Notes = string.Empty,
                Summary = "Summary Description"
            };

            this.ActiveSection?.Entities.Add(new SectionViewModel(entity));
        }

        private string SelectedEntityFile()
        {
            if (_selectedEntity is SectionViewModel)
            {
                return Path.Combine(_projectPath, ProjectLoader.SectionFolderName, $"{_selectedEntity.Id}.yaml");
            }

            if (_selectedEntity is IndexCardViewModel)
            {
                return Path.Combine(_projectPath, ProjectLoader.CardFolderName, $"{_selectedEntity.Id}.md");
            }

            throw new ArgumentException($"No file known for type {_selectedEntity.GetType()}");
        }

        private void LaunchEditorOnSelected()
        {
            if (_selectedEntity is not null)
            {
                var file = this.SelectedEntityFile();
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                {
                    var process = new Process {StartInfo = {FileName = "xdg-open", Arguments = file}};
                    process.Start();
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                {
                    var process = new Process {StartInfo = {FileName = "open", Arguments = file}};
                    process.Start();
                }
                else
                {
                    Process.Start(file);
                }
            }
        }

        private void LoadSelectedFromFile()
        {
            // if (_selectedEntity is IndexCardViewModel card)
            // {
            //     var loaded = EntityWriter.LoadIndexCard(this.SelectedEntityFile());
            //     card.Label = loaded.Label;
            //     card.Summary = loaded.Summary;
            //     card.Notes = loaded.Notes;
            //     card.Content = loaded.Content;
            // }
            //
            // if (_selectedEntity is SectionViewModel section)
            // {
            //     var loaded = EntityWriter.LoadSection(this.SelectedEntityFile());
            //     section.Label = loaded.Label;
            //     section.Summary = loaded.Summary;
            //     section.Notes = loaded.Notes;
            // }
        }

        private void DeleteSelected()
        {
            if (this.SelectedDetachedEntity is null) return;
            this.RemoveFromDetached(this.SelectedDetachedEntity);
            this.SelectedDetachedEntity = null;
        }

        private void SetCanvasScaleValue(double d)
        {
            this.CanvasScale = d;
            this.Settings.CanvasScale = d;
            this.SaveSettings();
        }
        
        private void SetTreeScaleValue(double d)
        {
            this.TreeScale = d;
            this.Settings.ProjectTreeScale = d;
            this.SaveSettings();
        }
    }
}