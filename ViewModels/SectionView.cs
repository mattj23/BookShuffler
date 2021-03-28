using System.Collections.ObjectModel;
using Avalonia;
using BookShuffler.Models;
using ReactiveUI;

namespace BookShuffler.ViewModels
{
    public class SectionView : ViewModelBase, IEntityView
    {
        private Point _position;
        private readonly Entity _model;

        public SectionView(Entity model)
        {
            _model = model;
            this.Entities = new ObservableCollection<IEntityView>();
        }

        public SectionView()
        {
            
        }

        public ObservableCollection<IEntityView> Entities { get; }
        
        public Point Position
        {
            get => _position;
            set => this.RaiseAndSetIfChanged(ref _position, value);
        }

        public string? Summary
        {
            get => _model.Summary;
            set
            {
                if (_model.Summary == value) return;
                _model.Summary = value;
                this.RaisePropertyChanged();
            }
        }

        public string? Notes
        {
            get => _model.Notes;
            set
            {
                if (_model.Notes == value) return;
                _model.Notes = value;
                this.RaisePropertyChanged();
            }
        }

        public WorkflowLabel Label
        {
            get => _model.Label;
            set
            {
                if (_model.Label == value) return;
                _model.Label = value;
                this.RaisePropertyChanged();
            }
        }
    }
}