using System.Windows.Input;
using ReactiveUI;

namespace BookShuffler.ViewModels
{
    public class ToggleExpanderViewModel : ViewModelBase
    {
        private bool _isExpanded;

        public ToggleExpanderViewModel()
        {
            this.Toggle = ReactiveCommand.Create(() => this.IsExpanded = !this.IsExpanded);
        }

        public bool IsExpanded
        {
            get => _isExpanded;
            set => this.RaiseAndSetIfChanged(ref _isExpanded, value);
        }
        
        public ICommand Toggle { get; }
    }
}