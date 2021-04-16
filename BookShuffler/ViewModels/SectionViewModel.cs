using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Windows.Input;
using Avalonia;
using BookShuffler.Models;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using ReactiveUI;

namespace BookShuffler.ViewModels
{
    public class SectionViewModel : ViewModelBase, IEntityViewModel
    {
        private Point _position;            // The logical position of this entity
        private Point _viewOffset;          // The offset of the this entity's *parent* section
        private Point _childrenOffset;      // The offset of this section's children
        private readonly Entity _model;
        private int _z;
        private bool _isExpanded;
        private readonly Subject<IEntityViewModel> _detachSubject;

        public Point ViewPosition => _position + _viewOffset;
        
        public SectionViewModel(Entity model)
        {
            _model = model;
            this.Entities = new ObservableCollection<IEntityViewModel>();
            _childrenOffset = new Point(0, 0);
            _detachSubject = new Subject<IEntityViewModel>();
            this.Detach = ReactiveCommand.Create(() => _detachSubject.OnNext(this));
        }
        
        public ICommand Detach { get; }

        public IObservable<IEntityViewModel> DetachRequest => _detachSubject.AsObservable();

        public SectionViewModel(string summary)
        {
            _model = new Entity {Id = Guid.NewGuid(), Summary = summary};
            this.Entities = new ObservableCollection<IEntityViewModel>();
            _childrenOffset = new Point(0, 0);
        }

        public bool IsExpanded
        {
            get => _isExpanded;
            set => this.RaiseAndSetIfChanged(ref _isExpanded, value);
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
            set
            {
                if (_position == value) return;
                _position = value;
                this.RaisePropertyChanged();
                this.RaisePropertyChanged(nameof(ViewPosition));
            }
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

        public void SetChildrenOffset(Point absolute)
        {
            this._childrenOffset = absolute;
            foreach (var entity in this.Entities)
            {
                entity.SetViewOffset(absolute);
            }
        }

        public void IncrementChildrenOffset(Point relative)
        {
            this.SetChildrenOffset(this._childrenOffset + relative);
        }

        public void SetViewOffset(Point offset)
        {
            this._viewOffset = offset;
            this.RaisePropertyChanged(nameof(ViewPosition));
        }

        public void CopyValuesFrom(Entity model)
        {
            this.Summary = model.Summary;
            this.Notes = model.Notes;
            this.Label = model.Label;
        }

        public bool Equals(IEntityViewModel? other)
        {
            if (other is null) return false;
            return other.Id == this.Id;
        }

        public IndexCardViewModel[] GetOrderedCards()
        {
            var cards = new List<IndexCardViewModel>();
            var sections = new List<SectionViewModel>();
            
            this.RecursiveCardSearch(this, cards, sections);

            return cards.ToArray();
        }

        private void RecursiveCardSearch(IEntityViewModel vm, List<IndexCardViewModel> cards,
            List<SectionViewModel> sections)
        {
            if (vm is IndexCardViewModel card)
            {
                cards.Add(card);
                return;
            }

            if (vm is SectionViewModel sec)
            {
                foreach (var childVm in sec.Entities)
                {
                    this.RecursiveCardSearch(childVm, cards, sections);
                }
            }
        }
        
    }
}