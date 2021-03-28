using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.LogicalTree;
using Avalonia.Markup.Xaml;
using BookShuffler.ViewModels;

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

    }
}