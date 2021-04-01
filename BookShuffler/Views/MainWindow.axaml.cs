using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using BookShuffler.Models;
using BookShuffler.ViewModels;
using MessageBox.Avalonia.Enums;
using ReactiveUI;

namespace BookShuffler.Views
{
    public class MainWindow : Window
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

        }

        private AppViewModel? ViewModel => this.DataContext as AppViewModel;

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
            _layoutContainer = this.FindControl<ItemsControl>("LayoutContainer");
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
            if (_isPanning & this.ViewModel != null)
            {
                var pointer = e.GetCurrentPoint(this);
                var shift = pointer.Position - _dragStart;
                _dragStart = pointer.Position;
                this.ViewModel?.ActiveSection?.IncrementChildrenOffset(shift);
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
                
                this.ViewModel?.ActiveSection?.BringChildToFront(_selected);

                if (viewModel is IndexCardViewModel card)
                {
                    this.ViewModel.SelectedEntity = card;
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
                this.ViewModel.ResortActiveSection();
            }
        }

        private async void NewProject_OnClick(object? sender, RoutedEventArgs e)
        {
            this.ViewModel.GetCanvasBounds = () => _layoutContainer.Bounds;
            
            var dialog = new OpenFolderDialog
            {
                Title = "Select Location for New Project"
            };

            var result = await dialog.ShowAsync(this);

            if (string.IsNullOrEmpty(result)) return;

            // this.ViewModel.NewProject(result);
        }

        private async void ImportTaggedMarkdown_OnClick(object? sender, RoutedEventArgs e)
        {
            
            var dialog = new OpenFileDialog
            {
                AllowMultiple = true,
                Directory = ViewModel.Project.ProjectFolder,
                Title = "Select Markdown File(s)",
                Filters = new List<FileDialogFilter>{new FileDialogFilter()
                {
                    Extensions = {"md", "MD"},
                    Name = "Markdown File"
                }}
            };

            var result = await dialog.ShowAsync(this);
            if (result?.Any() == true)
            {
                this.ViewModel.ImportTaggedMarkdown(result);
            }
        }

        private async void OpenProject_OnClick(object? sender, RoutedEventArgs e)
        {
            // this.ViewModel.GetCanvasBounds = () => _layoutContainer.Bounds;
            //
            // var dialog = new OpenFileDialog
            // {
            //     Directory = ViewModel.ProjectPath,
            //     Title = "Select project.yaml File",
            //     Filters = new List<FileDialogFilter>{new()
            //     {
            //         Extensions = {"yml", "yaml"},
            //         Name = "Project YAML File"
            //     }}
            // };
            //
            // var result = await dialog.ShowAsync(this);
            //
            // if (string.IsNullOrEmpty(result?.FirstOrDefault())) return;
            //
            // this.ViewModel.OpenProject(result.First());
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
            if (_forceClose || this.ViewModel?.Project?.HasUnsavedChanges != true) return;
            
            // This is arranged in this way because Avalonia does not wait for this to complete as an asynchronous
            // method and will not accept async Task as a method signature. Thus when reaching the await the window
            // will close anyway unless we interrupt it.
            e.Cancel = true;
                
            var dialog = MessageBox.Avalonia.MessageBoxManager
                .GetMessageBoxStandardWindow("Unsaved Changes",
                    "You have unsaved changes, are you sure you want to exit?", ButtonEnum.OkCancel);
            var result = await dialog.ShowDialog(this);

            if (result == ButtonResult.Ok)
            {
                _forceClose = true;
                this.Close();
            }
        }

        private void MainWindow_OnDataContextChanged(object? sender, EventArgs e)
        {
            if (this.ViewModel is not null)
            {
                this.RegisterKeyBindings(this.ViewModel.Settings.KeyBindings);
                this.ViewModel.GetCanvasBounds = () => _layoutContainer.Bounds;
            }
        }

        private void RegisterKeyBindings(KeyboardShortcuts shortcut)
        {
            this.KeyBindings.Clear();
            this.Register(this.ViewModel.LeftExpander.Toggle, shortcut.ExpandProject);
            this.Register(this.ViewModel.RightExpander.Toggle, shortcut.ExpandDetached);
            this.Register(this.ViewModel.SaveProjectCommand, shortcut.SaveProject);
            this.Register(this.ViewModel.AutoTileActiveSectionCommand, shortcut.AutoArrange);
            this.Register(this.ViewModel.AttachSelectedCommand, shortcut.AttachSelected);
            this.Register(this.ViewModel.DetachSelectedCommand, shortcut.DetachSelected);
        }

        private void Register(ICommand c, string s)
        {
            this.KeyBindings.Add(new KeyBinding {Command = c, Gesture = KeyGesture.Parse(s)});
        }
    }
}