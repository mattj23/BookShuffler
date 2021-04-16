using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reactive;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.ReactiveUI;
using BookShuffler.Models;
using BookShuffler.ViewModels;
using MessageBox.Avalonia;
using MessageBox.Avalonia.Enums;
using ReactiveUI;

namespace BookShuffler.Views
{
    public class MainWindow : ReactiveWindow<AppViewModel>
    {
        private IEntityViewModel? _selected;
        private Point _clickPoint;
        private Point _dragStart;
        private bool _isPanning;
        private ItemsControl _layoutContainer;
        private EntityView? _selectedEntityView;
        private bool _forceClose;
        
        public MainWindow()
        {
            InitializeComponent();
            
#if DEBUG
            this.AttachDevTools();
#endif

            // Register interactions to the various handlers
            this.WhenActivated(d => d(ViewModel.EditCategories.RegisterHandler(DoEditCategoriesAsync)));
            this.WhenActivated(d => d(ViewModel.NewProject.RegisterHandler(DoSelectNewFolder)));
            this.WhenActivated(d => d(ViewModel.OpenProject.RegisterHandler(DoOpenProjectAsync)));
            this.WhenActivated(d => d(ViewModel.ImportMarkdown.RegisterHandler(DoImportMarkdownAsync)));
            this.WhenActivated(d => d(ViewModel.ExportMarkdown.RegisterHandler(DoSelectExportFile)));
            this.WhenActivated(d => d(ViewModel.ShowWarningMessage.RegisterHandler(DoShowWarningMessage)));
        }

        private AppViewModel? ViewModel => DataContext as AppViewModel;

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
            _layoutContainer = this.FindControl<ItemsControl>("LayoutContainer");
        }
        
        // Interactions 
        // ======================================================================================================
        private async Task DoShowWarningMessage(InteractionContext<WarningMessage, Unit> interaction)
        {
            var dialog = MessageBoxManager.GetMessageBoxStandardWindow(interaction.Input.Title,
                interaction.Input.Message, ButtonEnum.Ok);
            await dialog.ShowDialog(this);
        }

        private async Task DoEditCategoriesAsync(InteractionContext<ProjectCategories, Unit> interaction)
        {
            var dialog = new CategoryEditorView();
            dialog.DataContext = interaction.Input;
            await dialog.ShowDialog(this);
            interaction.SetOutput(default);
        }

        private async Task DoSelectExportFile(InteractionContext<Unit, string?> interaction)
        {
            var dialog = new SaveFileDialog
            {
                Title = "Select Location for New Project",
                DefaultExtension = "md",
                Directory = this.ViewModel.Project.ProjectFolder,
                InitialFileName = "export.md",
                Filters = new List<FileDialogFilter>{new FileDialogFilter
                {
                    Extensions = {"md", "MD"},
                    Name = "Markdown File"
                }}
            };

            var result = await dialog.ShowAsync(this);
            interaction.SetOutput(result);
        }
        
        private async Task DoSelectNewFolder(InteractionContext<Unit, string?> interaction)
        {
            var dialog = new OpenFolderDialog
            {
                Title = "Select Location for New Project"
            };

            var result = await dialog.ShowAsync(this);
            interaction.SetOutput(result);
        }

        private async Task DoOpenProjectAsync(InteractionContext<Unit, string?> interaction)
        {
            var dialog = new OpenFileDialog
            {
                Directory = ViewModel?.Project?.ProjectFolder,
                Title = "Select project.yaml File",
                Filters = new List<FileDialogFilter>{new()
                {
                    Extensions = {"yml", "yaml"},
                    Name = "Project YAML File"
                }}
            };
            
            var result = await dialog.ShowAsync(this);
            interaction.SetOutput(result?.FirstOrDefault());
        }
        
        private async Task DoImportMarkdownAsync(InteractionContext<Unit, string[]?> interaction)
        {
            var dialog = new OpenFileDialog
            {
                AllowMultiple = true,
                Directory = ViewModel.Project.ProjectFolder,
                Title = "Select Markdown File(s)",
                Filters = new List<FileDialogFilter>{new FileDialogFilter
                {
                    Extensions = {"md", "MD"},
                    Name = "Markdown File"
                }}
            };

            var result = await dialog.ShowAsync(this);
            interaction.SetOutput(result);
        }


        private void LayoutContainer_OnPointerPressed(object? sender, PointerPressedEventArgs e)
        {
            var pointer = e.GetCurrentPoint(this);
            if (pointer.Properties.IsLeftButtonPressed)
            {
                _dragStart = pointer.Position;
                _isPanning = true;
            }
        }
        
        private void LayoutContainer_OnPointerMoved(object? sender, PointerEventArgs e)
        {
            if (_isPanning & ViewModel != null)
            {
                var pointer = e.GetCurrentPoint(this);
                var shift = pointer.Position - _dragStart;
                _dragStart = pointer.Position;
                ViewModel?.ActiveSection?.IncrementChildrenOffset(shift);
            }
        }

        private void LayoutContainer_OnPointerReleased(object? sender, PointerReleasedEventArgs e)
        {
            _isPanning = false;
        }
        
        private void InputElement_OnPointerPressed(object? sender, PointerPressedEventArgs e)
        {
            var pointer = e.GetCurrentPoint(_layoutContainer);
            if ((sender as EntityView)?.DataContext is IEntityViewModel viewModel && pointer.Properties.IsLeftButtonPressed)
            {
                _selected = viewModel;
                _selectedEntityView = sender as EntityView;
                _clickPoint = pointer.Position;
                _dragStart = viewModel.Position;
                
                ViewModel?.ActiveSection?.BringChildToFront(_selected);

                if (viewModel is IndexCardViewModel card)
                {
                    ViewModel.SelectedEntity = card;
                }
            }

            e.Handled = true;
        }

        private void InputElement_OnPointerMoved(object? sender, PointerEventArgs e)
        {
            var pointer = e.GetCurrentPoint(_layoutContainer);
            if (_selected is not null && pointer.Properties.IsLeftButtonPressed)
            {
                var (x, y) = pointer.Position - _clickPoint + _dragStart;

                _selected.Position = new Point(Math.Min(Math.Max(0, x), _layoutContainer.Bounds.Width - _selectedEntityView.Bounds.Width),
                    Math.Min(Math.Max(0, y), _layoutContainer.Bounds.Height - _selectedEntityView.Bounds.Height));
            }

            e.Handled = true;
        }

        private void InputElement_OnPointerReleased(object? sender, PointerReleasedEventArgs e)
        {
            if (_selected is not null)
            {
                _selected = null;
                ViewModel.ResortActiveSection();
            }
        }
        
        private void Section_OnDoubleTapped(object? sender, RoutedEventArgs e)
        {
            if ((sender as Control)?.Parent is TreeViewItem parent)
            {
                parent.IsExpanded = !parent.IsExpanded;
            }
        }

        private async void Window_OnClosing(object? sender, CancelEventArgs e)
        {
            if (_forceClose || ViewModel?.Project?.HasUnsavedChanges != true) return;
            
            // This is arranged in this way because Avalonia does not wait for this to complete as an asynchronous
            // method and will not accept async Task as a method signature. Thus when reaching the await the window
            // will close anyway unless we interrupt it.
            e.Cancel = true;
                
            var dialog = MessageBoxManager
                .GetMessageBoxStandardWindow("Unsaved Changes",
                    "You have unsaved changes, are you sure you want to exit?", ButtonEnum.OkCancel);
            var result = await dialog.ShowDialog(this);

            if (result == ButtonResult.Ok)
            {
                _forceClose = true;
                Close();
            }
        }

        private void MainWindow_OnDataContextChanged(object? sender, EventArgs e)
        {
            if (ViewModel is not null)
            {
                RegisterKeyBindings(ViewModel.Settings.KeyBindings);
                ViewModel.GetCanvasBounds = () => _layoutContainer.Bounds;
            }
        }

        private void RegisterKeyBindings(KeyboardShortcuts shortcut)
        {
            KeyBindings.Clear();
            Register(ViewModel.LeftExpander.Toggle, shortcut.ExpandProject);
            Register(ViewModel.RightExpander.Toggle, shortcut.ExpandDetached);
            Register(ViewModel.Commands.SaveProject, shortcut.SaveProject);
            Register(ViewModel.Commands.AutoArrange, shortcut.AutoArrange);
            Register(ViewModel.Commands.AttachEntity, shortcut.AttachSelected);
            Register(ViewModel.Commands.DetachEntity, shortcut.DetachSelected);
            Register(ViewModel.Commands.CreateCard, shortcut.CreateCard);
            Register(ViewModel.Commands.CreateSection, shortcut.CreateSection);
            Register(ViewModel.Commands.ExportRoot, shortcut.ExportAll);
            Register(ViewModel.Commands.ExportSection, shortcut.ExportSelected);
        }

        private void Register(ICommand c, string s)
        {
            if (!string.IsNullOrEmpty(s))
                KeyBindings.Add(new KeyBinding {Command = c, Gesture = KeyGesture.Parse(s)});
        }
    }
}