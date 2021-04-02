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
        
        
    }
}