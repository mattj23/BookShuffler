using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace BookShuffler.Views
{
    public class EntityView : UserControl
    {
        public EntityView()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}