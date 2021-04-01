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
using BookShuffler.Tools.Storage;
using ReactiveUI;
using YamlDotNet.RepresentationModel;
using Path = System.IO.Path;

namespace BookShuffler.ViewModels
{
    public class AppViewModel : ViewModelBase
    {
        private SectionViewModel _activeSection;
        private IEntityViewModel? _selectedEntity;
        private IEntityViewModel? _selectedDetachedEntity;

        private readonly BehaviorSubject<bool> _selectedIsSectionSubject;
        private double _canvasScale;
        private double _treeScale;
        private ProjectViewModel? _project;
        private IDisposable? _unsavedSubscription;

        private readonly IStorageProvider _storage;

        public AppViewModel() : this(new FileSystemStorage())
        {
        }

        public AppViewModel(IStorageProvider storage)
        {
            _storage = storage;

            _selectedIsSectionSubject = new BehaviorSubject<bool>(false);

            this.SaveProjectCommand = ReactiveCommand.Create(this.SaveProject);
            
            this.DetachSelectedCommand = ReactiveCommand.Create(() =>
            {
                var detached = this.Project?.DetachEntity(this.SelectedEntity);
                if (detached is not null) this.SelectedDetachedEntity = detached;
                this.SelectedEntity = null;
            });
            
            this.AttachSelectedCommand = ReactiveCommand.Create(() =>
            {
                var attached =this.Project?.AttachEntity(this.SelectedDetachedEntity, this.ActiveSection);
                if (attached is not null)
                {
                    this.SelectedEntity = attached;
                    this.SelectedDetachedEntity = null;
                }
            });

            // this.LaunchEditorCommand = ReactiveCommand.Create(this.LaunchEditorOnSelected);
            // this.LoadFromFileCommand = ReactiveCommand.Create(this.LoadSelectedFromFile);
            this.DeleteEntityCommand = ReactiveCommand.Create(this.DeleteSelected);

            this.CreateCardCommand =
                ReactiveCommand.Create(this.AddCardToSelected, _selectedIsSectionSubject);
            this.CreateSectionCommand =
                ReactiveCommand.Create(this.AddSectionToSelected, _selectedIsSectionSubject);

            this.SetCanvasScale = ReactiveCommand.Create<string>(d => this.SetCanvasScaleValue(double.Parse(d)));
            this.SetTreeScale = ReactiveCommand.Create<string>(d => this.SetTreeScaleValue(double.Parse(d)));

            this.AutoTileActiveSectionCommand = ReactiveCommand.Create(() =>
                this.ActiveSection?.AutoTile(this.GetCanvasBounds?.Invoke().Width ?? 1200));


            this.LeftExpander = new ToggleExpanderViewModel {IsExpanded = true};
            this.RightExpander = new ToggleExpanderViewModel {IsExpanded = true};
            this.CanvasScale = 1;
            this.TreeScale = 1;

            this.LoadSettings();

            if (!string.IsNullOrEmpty(this.Settings?.LastOpenedProject)
                && File.Exists(this.Settings.LastOpenedProject))
            {
                this.OpenProject(this.Settings.LastOpenedProject);
            }
        }

        public AppSettings Settings { get; private set; }

        public ToggleExpanderViewModel LeftExpander { get; }
        public ToggleExpanderViewModel RightExpander { get; }

        public ProjectViewModel? Project
        {
            get => _project;
            private set
            {
                if (_project == value) return;
                _unsavedSubscription?.Dispose();

                _project = value;
                _unsavedSubscription = _project.WhenAnyValue(x => x.HasUnsavedChanges)
                    .Subscribe(_ => this.RaisePropertyChanged(nameof(AppTitle)));
                this.RaisePropertyChanged();
                this.RaisePropertyChanged(nameof(AppTitle));
            }
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

        /// <summary>
        /// Gets a function which returns the canvas boundaries. This is necessary because binding OneWayToSource on
        /// a property in the XAML is currently broken in Avalonia.
        /// </summary>
        public Func<Rect> GetCanvasBounds { get; set; }

        /// <summary>
        /// Gets whether or not there is an actively loaded project based on the value of ProjectPath
        /// </summary>
        public bool HasActiveProject => this.Project is not null;

        /// <summary>
        /// Gets the title of the application window
        /// </summary>
        public string AppTitle
        {
            get
            {
                if (this.Project is null) return "Book Shuffler [no project]";
                return $"Book Shuffler [{this.Project.ProjectFolder}]" +
                       (this.Project.HasUnsavedChanges ? " (unsaved changes)" : string.Empty);
            }
        }

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
                    var parent = this.Project?.GetParent(_selectedEntity);
                    if (parent is not null) this.ActiveSection = parent;
                    this.ActiveSection?.BringChildToFront(_selectedEntity);
                }
            }
        }

        public IEntityViewModel? SelectedDetachedEntity
        {
            get => _selectedDetachedEntity;
            set => this.RaiseAndSetIfChanged(ref _selectedDetachedEntity, value);
        }

        public void SaveProject()
        {
            if (this.Project is null) return;
            var writer = new ProjectWriter(_storage);
            writer.Save(this.Project);
            this.Project.HasUnsavedChanges = false;
        }

        /// <summary>
        /// Opens a project from the storage provider
        /// </summary>
        public void OpenProject(string projectFolder)
        {
            if (string.IsNullOrEmpty(projectFolder))
                throw new ArgumentException("An empty project file/folder was provided");

            if (File.Exists(projectFolder) && projectFolder.EndsWith(".yaml"))
            {
                projectFolder = new FileInfo(projectFolder).DirectoryName!;
            }

            // TODO: Error catching
            var loader = new ProjectLoader(_storage);
            var result = loader.Load(projectFolder);

            this.Project = ProjectViewModel.FromLoad(result);
            this.SelectedEntity = this.Project.Root;
        }

        /// <summary>
        /// Import a tagged markdown file into the currently active project
        /// </summary>
        /// <param name="files"></param>
        public void ImportTaggedMarkdown(string[] files)
        {
            if (this.Project is null) return;
            
            foreach (var file in files)
            {
                var parseResult = MarkdownParser.Parse(file);
                this.Project.Merge(parseResult.ToLoadResult());
            }
        }

        public void ResortActiveSection()
        {
            this.ActiveSection?.ResortOrder();
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

        // private string SelectedEntityFile()
        // {
        //     if (_selectedEntity is SectionViewModel)
        //     {
        //         return Path.Combine(_projectPath, ProjectLoader.SectionFolderName, $"{_selectedEntity.Id}.yaml");
        //     }
        //
        //     if (_selectedEntity is IndexCardViewModel)
        //     {
        //         return Path.Combine(_projectPath, ProjectLoader.CardFolderName, $"{_selectedEntity.Id}.md");
        //     }
        //
        //     throw new ArgumentException($"No file known for type {_selectedEntity.GetType()}");
        // }
        //
        // private void LaunchEditorOnSelected()
        // {
        //     if (_selectedEntity is not null)
        //     {
        //         var file = this.SelectedEntityFile();
        //         if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        //         {
        //             var process = new Process {StartInfo = {FileName = "xdg-open", Arguments = file}};
        //             process.Start();
        //         }
        //         else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        //         {
        //             var process = new Process {StartInfo = {FileName = "open", Arguments = file}};
        //             process.Start();
        //         }
        //         else
        //         {
        //             Process.Start(file);
        //         }
        //     }
        // }

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
            // this.RemoveFromDetached(this.SelectedDetachedEntity);
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