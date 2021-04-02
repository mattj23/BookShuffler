using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace BookShuffler.Views
{
    public class CategoryEditorView : Window
    {
        public CategoryEditorView()
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
    }
}