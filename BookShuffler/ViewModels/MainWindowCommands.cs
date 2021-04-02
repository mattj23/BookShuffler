using System.Windows.Input;

namespace BookShuffler.ViewModels
{
    public class MainWindowCommands
    {
        public ICommand SaveProject { get; set; }
        public ICommand NewProject { get; set; }
        public ICommand OpenProject { get; set; }
        public ICommand ImportMarkdown { get; set; }
        public ICommand AttachEntity { get; set; }
        public ICommand DetachEntity { get; set; }
        public ICommand AutoArrange { get; set; }
        public ICommand EditCategories { get; set; }

        public ICommand CreateCard { get; set; }

        public ICommand CreateSection { get; set; }

        public ICommand DeleteEntity { get; set; }

        public ICommand SetCanvasScale { get; set; }
        public ICommand SetTreeScale { get; set; }
        
        public ICommand ExportRoot { get; set; }
        public ICommand ExportSection { get; set; }
    }
}