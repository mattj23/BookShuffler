using System;
using System.Collections.ObjectModel;
using System.Linq;
using Avalonia;
using BookShuffler.Models;
using ReactiveUI;

namespace BookShuffler.ViewModels
{
    public class SectionViewModel : ViewModelBase, IEntityViewModel
    {
        private Point _position;
        private readonly Entity _model;
        private double _viewShiftX;
        private double _viewShiftY;
        private int _z;

        public SectionViewModel(Entity model)
        {
            _model = model;
            this.Entities = new ObservableCollection<IEntityViewModel>();
        }

        public double ViewShiftX
        {
            get => _viewShiftX;
            set => this.RaiseAndSetIfChanged(ref _viewShiftX, value);
        }

        public double ViewShiftY
        {
            get => _viewShiftY;
            set => this.RaiseAndSetIfChanged(ref _viewShiftY, value);
        }

        public int Z
        {
            get => _z;
            set => this.RaiseAndSetIfChanged(ref _z, value);
        }
        
        public Guid Id => _model.Id;
        
        public ObservableCollection<IEntityViewModel> Entities { get; }
        
        public Point Position
        {
            get => _position;
            set => this.RaiseAndSetIfChanged(ref _position, value);
        }

        public string Summary
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

        public string Content => string.Join("\n", this.Entities.Select(x => x.Summary));

        public void BringChildToFront(IEntityViewModel child)
        {
            var oldZ = child.Z;
            foreach (var entity in Entities)
            {
                if (entity.Z >= oldZ) entity.Z -= 1;
            }

            child.Z = Entities.Max(e => e.Z) + 1;
        }
    }
}