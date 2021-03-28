using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.LogicalTree;
using Avalonia.Markup.Xaml;
using BookShuffler.ViewModels;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using ReactiveUI;

namespace BookShuffler.Views
{
    public class MainWindow : Window
    {
        private IEntityView? _selected;
        private Point _clickPoint;
        private Point _dragStart;
        private ItemsControl _layoutContainer;
        private Border? _selectedBorder;
        
        public MainWindow()
        {
            InitializeComponent();
            
#if DEBUG
            this.AttachDevTools();
#endif
        }
        
        public ICommand CreateNewProjectCommand { get; }
        
        private AppViewModel? ViewModel => this.DataContext as AppViewModel;

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
            _layoutContainer = this.FindControl<ItemsControl>("LayoutContainer");
        }

        private void InputElement_OnPointerPressed(object? sender, PointerPressedEventArgs e)
        {
            var pointer = e.GetCurrentPoint(_layoutContainer);
            if ((sender as Border)?.Tag is IEntityView box && pointer.Properties.IsLeftButtonPressed)
            {
                _selected = box;
                _selectedBorder = sender as Border;
                _clickPoint = pointer.Position;
                _dragStart = box.Position;
            }
            
        }

        private void InputElement_OnPointerMoved(object? sender, PointerEventArgs e)
        {
            var pointer = e.GetCurrentPoint(_layoutContainer);
            if (_selected is not null && pointer.Properties.IsLeftButtonPressed)
            {
                var (x, y) = pointer.Position - _clickPoint + _dragStart;

                _selected.Position = new Point(Math.Min(Math.Max(0, x), _layoutContainer.Bounds.Width - _selectedBorder.Bounds.Width),
                    Math.Min(Math.Max(0, y), _layoutContainer.Bounds.Height - _selectedBorder.Bounds.Height));
            }
        }

        private void InputElement_OnPointerReleased(object? sender, PointerReleasedEventArgs e)
        {
            _selected = null;
        }

        private async void NewProject_OnClick(object? sender, RoutedEventArgs e)
        {
            var dialog = new OpenFolderDialog
            {
                Title = "Select Location for New Project"
            };

            var result = await dialog.ShowAsync(this);

            if (string.IsNullOrEmpty(result)) return;

            this.ViewModel.NewProject(result);
        }

        private async void ImportTaggedMarkdown_OnClick(object? sender, RoutedEventArgs e)
        {
            
            var dialog = new OpenFileDialog
            {
                AllowMultiple = true,
                Directory = ViewModel.ProjectPath,
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
    }
}