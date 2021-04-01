using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using BookShuffler.Tools.Storage;
using BookShuffler.ViewModels;
using BookShuffler.Views;

namespace BookShuffler
{
    public class App : Application
    {
        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public override void OnFrameworkInitializationCompleted()
        {
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                var storage = new FileSystemStorage();
                desktop.MainWindow = new MainWindow
                {
                    DataContext = new AppViewModel(storage),
                };
            }

            base.OnFrameworkInitializationCompleted();
        }
    }
}