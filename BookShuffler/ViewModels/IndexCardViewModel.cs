using System;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Windows.Input;
using Avalonia;
using BookShuffler.Models;
using ReactiveUI;

namespace BookShuffler.ViewModels
{
    public class IndexCardViewModel : ViewModelBase, IEntityViewModel
    {
        private IProjectCategories? _categories;
        private Point _position;
        private Point _viewOffset;
        private int _z;
        private readonly Subject<IEntityViewModel> _detachSubject;

        public Guid Id => Model.Id;

        public IndexCardViewModel() : this(new IndexCard())  {}
        
        public IndexCardViewModel(IndexCard model)
        {
            this.Model = model;
            _viewOffset = new Point(0, 0);
            _detachSubject = new Subject<IEntityViewModel>();
            this.Detach = ReactiveCommand.Create(() => _detachSubject.OnNext(this));
        }
        
        public ICommand Detach { get; }

        public IObservable<IEntityViewModel> DetachRequest => _detachSubject.AsObservable();
        
        public Point ViewPosition => _position + _viewOffset;

        /// <summary>
        /// Gets or sets (attaches) a set of categories defined by the project itself
        /// </summary>
        public IProjectCategories ProjectCategories
        {
            get => _categories;
            set
            {
                if (_categories == value) return;
                _categories = value;
                this.RaisePropertyChanged();
                this.RaisePropertyChanged(nameof(Category));
            }
        }

        public CategoryViewModel? Category
        {
            get => _categories?.ById.ContainsKey(this.Model.CategoryId) == true ? _categories.ById[this.Model.CategoryId] : null;
            set
            {
                if (this.Model.CategoryId == value?.Id) return;
                this.Model.CategoryId = value?.Id ?? -1;
                this.RaisePropertyChanged();
            }
        }

        public int Z
        {
            get => _z;
            set => this.RaiseAndSetIfChanged(ref _z, value);
        }

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
            get => Model.Summary;
            set
            {
                if (Model.Summary == value) return;
                Model.Summary = value;
                this.RaisePropertyChanged();
            }
        }
        
        public string? Notes
        {
            get => Model.Notes;
            set
            {
                if (Model.Notes == value) return;
                Model.Notes = value;
                this.RaisePropertyChanged();
            }
        }
        
        public string Content
        {
            get => Model.Content;
            set
            {
                if (Model.Content == value) return;
                Model.Content = value;
                this.RaisePropertyChanged();
            }
        }
        
        public WorkflowLabel Label
        {
            get => Model.Label;
            set
            {
                if (Model.Label == value) return;
                Model.Label = value;
                this.RaisePropertyChanged();
            }
        }

        public IndexCard Model { get; }

        public void SetViewOffset(Point offset)
        {
            this._viewOffset = offset;
            this.RaisePropertyChanged(nameof(ViewPosition));
        }
        
        public bool Equals(IEntityViewModel? other)
        {
            if (other is null) return false;
            return other.Id == this.Id;
        }
    }
}