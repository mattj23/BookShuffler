using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using BookShuffler.ViewModels;

namespace BookShuffler.Views
{
    public class MainWindow : Window
    {
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
        }

        private IEntityView? _selected;
        private Point _clickPoint;
        private Point _dragStart;

        private void InputElement_OnPointerPressed(object? sender, PointerPressedEventArgs e)
        {
            var pointer = e.GetCurrentPoint(this);
            if ((sender as Border)?.Tag is IEntityView box && pointer.Properties.IsLeftButtonPressed)
            {
                _selected = box;
                _clickPoint = pointer.Position;
                _dragStart = box.Position;
            }
            
        }

        private void InputElement_OnPointerMoved(object? sender, PointerEventArgs e)
        {
            var pointer = e.GetCurrentPoint(this);
            if (_selected is not null && pointer.Properties.IsLeftButtonPressed)
            {
                _selected.Position = e.GetPosition(this) - _clickPoint + _dragStart;
            }
        }

        private void InputElement_OnPointerReleased(object? sender, PointerReleasedEventArgs e)
        {
            _selected = null;
        }

    }
}