using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
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

        private double _canvasScale;
        private double _treeScale;
        private ProjectViewModel? _project;
        private List<IDisposable> _projectSubscriptions;

        private readonly IStorageProvider _storage;

        public AppViewModel() : this(new FileSystemStorage())
        {
        }

        public AppViewModel(IStorageProvider storage)
        {
            _storage = storage;

            _projectSubscriptions = new List<IDisposable>();

            // Commands 
            // ====================================================================================
            var hasActiveProject = this.WhenAnyValue(v => v.Project).Select(p => p is not null);
            var hasActiveSection = this.WhenAnyValue(v => v.ActiveSection).Select(s => s is not null);
            var hasSelection = this.WhenAnyValue(v => v.SelectedEntity).Select(e => e is not null);
            var hasDetachedSelection = this.WhenAnyValue(v => v.SelectedDetachedEntity).Select(e => e is not null);
            
            this.Commands = new MainWindowCommands
            {
                NewProject = ReactiveCommand.Create(async () =>
                {
                    var result = await NewProject.Handle(default);
                    if (!string.IsNullOrEmpty(result)) this.New(result);
                }),

                OpenProject = ReactiveCommand.Create(async () =>
                {
                    var result = await OpenProject.Handle(default);
                    if (!string.IsNullOrEmpty(result)) this.Open(result);
                }),

                SaveProject = ReactiveCommand.Create(this.SaveProject, hasActiveProject),

                ImportMarkdown = ReactiveCommand.Create(async () =>
                {
                    var result = await ImportMarkdown.Handle(default);
                    this.ImportTaggedMarkdown(result);
                }, hasActiveProject),

                AutoArrange = ReactiveCommand.Create(
                    () => this.ActiveSection?.AutoTile(this.GetCanvasBounds?.Invoke().Width ?? 1200),
                    hasActiveProject),

                DetachEntity = ReactiveCommand.Create(() =>
                {
                    this.Project?.DetachEntity(this.SelectedEntity);
                    this.SelectedEntity = null;
                }, hasSelection),

                AttachEntity = ReactiveCommand.Create(() =>
                {
                    var attached = this.Project?.AttachEntity(this.SelectedDetachedEntity, this.ActiveSection);
                    if (attached is IndexCardViewModel card)
                    {
                        this.SelectedEntity = card;
                        this.SelectedDetachedEntity = null;
                    }

                    if (attached is SectionViewModel sec)
                    {
                        var parent = this.Project.GetParent(sec);
                        this.SelectedEntity = parent;
                        this.SelectedDetachedEntity = null;
                    }
                }, hasDetachedSelection),

                EditCategories = ReactiveCommand.CreateFromTask(async () =>
                {
                    await EditCategories.Handle(this.Project.Categories);
                }),
                
                ExportSection = ReactiveCommand.Create(async () =>
                {
                    var result = await this.ExportMarkdown.Handle(default);
                    this.ExportTo(result, this.ActiveSection);
                }, hasActiveSection),

                ExportRoot = ReactiveCommand.Create(async () =>
                {
                    var result = await this.ExportMarkdown.Handle(default);
                    this.ExportTo(result, this.Project?.Root);
                }, hasActiveProject),

                CreateCard = ReactiveCommand.Create(this.AddCardToSelected, hasActiveSection),
                CreateSection = ReactiveCommand.Create(this.AddSectionToSelected, hasActiveSection),
                SetCanvasScale = ReactiveCommand.Create<string>(d => this.SetCanvasScaleValue(double.Parse(d))),
                SetTreeScale = ReactiveCommand.Create<string>(d => this.SetTreeScaleValue(double.Parse(d))),
                
                OpenMarkdown = ReactiveCommand.Create(async () =>
                {
                    if (this.Project?.HasUnsavedChanges != false)
                    {
                        await this.ShowWarningMessage.Handle(new WarningMessage
                        {
                            Title = "Must Save Changes",
                            Message =
                                "Any unsaved changes must be saved before a file can be opened in an external editor"
                        });
                        return;
                    }
                    
                    this.LaunchEditorOnSelected();
                }, hasSelection),
                
                ReloadMarkdown = ReactiveCommand.Create(this.LoadSelectedFromFile, hasSelection),
            };

            // Interactions
            // ====================================================================================
            this.EditCategories = new Interaction<ProjectCategories, Unit>();
            this.NewProject = new Interaction<Unit, string?>();
            this.OpenProject = new Interaction<Unit, string?>();
            this.ImportMarkdown = new Interaction<Unit, string[]?>();
            this.ExportMarkdown = new Interaction<Unit, string?>();
            this.ShowWarningMessage = new Interaction<WarningMessage, Unit>();

            // Settings
            // ====================================================================================
            this.LeftExpander = new ToggleExpanderViewModel {IsExpanded = true};
            this.RightExpander = new ToggleExpanderViewModel {IsExpanded = true};
            this.CanvasScale = 1;
            this.TreeScale = 1;

            this.LoadSettings();

            // Open Last Project
            // ====================================================================================
            if (!string.IsNullOrEmpty(this.Settings?.LastOpenedProject)
                && File.Exists(this.Settings.LastOpenedProject))
            {
                this.Open(this.Settings.LastOpenedProject);
            }
        }

        public MainWindowCommands Commands { get; }

        // Interactions
        // ========================================================================================
        public Interaction<ProjectCategories, Unit> EditCategories { get; }
        public Interaction<Unit, string?> NewProject { get; }
        public Interaction<Unit, string?> OpenProject { get; }
        public Interaction<Unit, string[]?> ImportMarkdown { get; }
        public Interaction<Unit, string?> ExportMarkdown { get; }
        public Interaction<WarningMessage, Unit> ShowWarningMessage { get; }
        

        public AppSettings Settings { get; private set; }

        public ToggleExpanderViewModel LeftExpander { get; }
        public ToggleExpanderViewModel RightExpander { get; }

        public ProjectViewModel? Project
        {
            get => _project;
            private set
            {
                if (_project == value) return;
                foreach (var d in _projectSubscriptions)
                    d.Dispose();
                _projectSubscriptions.Clear();

                _project = value;
                this.RaisePropertyChanged();
                this.RaisePropertyChanged(nameof(AppTitle));

                if (_project is null) return;
                _projectSubscriptions.Add(_project.WhenAnyValue(x => x.HasUnsavedChanges)
                    .Subscribe(_ => this.RaisePropertyChanged(nameof(AppTitle))));
                _projectSubscriptions.Add(_project.EntityDetached.Subscribe(e => this.SelectedDetachedEntity = e));
            }
        }

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
        /// Create a new project at a given location in the storage provider
        /// </summary>
        /// <param name="projectFolder"></param>
        public void New(string projectFolder)
        {
            this.Project = ProjectViewModel.New(projectFolder);
            this.SelectedEntity = this.Project.Root;
        }

        /// <summary>
        /// Opens a project from the storage provider
        /// </summary>
        public void Open(string projectFolder)
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
        public void ImportTaggedMarkdown(string[]? files)
        {
            if (this.Project is null) return;
            if (files is null) return;

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
            Console.WriteLine($"Loading settings from {filePath}");
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

            this.Project?.AddNewCard(this.ActiveSection, new IndexCardViewModel(card));
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

            this.Project?.AddNewSection(this.ActiveSection, new SectionViewModel(entity));
        }

        private void ExportTo(string? path, SectionViewModel section)
        {
            if (string.IsNullOrEmpty(path)) return;
            if (section is null) return;

            var cards = section.GetOrderedCards();
            var text = string.Join("\n\n", cards.Select(x => x.Content));
            _storage.Put(path, text);
        }

        private string SelectedEntityFile()
        {
            if (_selectedEntity is SectionViewModel)
            {
                return Path.Combine(this.Project.ProjectFolder, ProjectLoader.SectionFolderName, $"{_selectedEntity.Id}.yaml");
            }
        
            if (_selectedEntity is IndexCardViewModel)
            {
                return Path.Combine(this.Project.ProjectFolder, ProjectLoader.CardFolderName, $"{_selectedEntity.Id}.md");
            }
        
            throw new ArgumentException($"No file known for type {_selectedEntity?.GetType()}");
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
            var reader = new EntityReader(_storage);
            
            if (_selectedEntity is IndexCardViewModel card)
            {
                var loaded = reader.LoadIndexCard(this.SelectedEntityFile());
                if (loaded is not null) 
                    card.CopyValuesFrom(loaded);
            }
            
            if (_selectedEntity is SectionViewModel section)
            {
                var loaded = reader.LoadSection(this.SelectedEntityFile());
                if (loaded is not null) 
                    section.CopyValuesFrom(loaded.ToEntity());
            }
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