using System;
using Avalonia;
using BookShuffler.Models;
using ReactiveUI;

namespace BookShuffler.ViewModels
{
    public class IndexCardViewModel : ViewModelBase, IEntityViewModel
    {
        private IProjectCategories? _categories;
        private Point _position;
        private int _z;

        public Guid Id => Model.Id;

        public IndexCardViewModel(IndexCard model)
        {
            this.Model = model;
        }

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

        public Category? Category
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
            set => this.RaiseAndSetIfChanged(ref _position, value);
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
    }
}