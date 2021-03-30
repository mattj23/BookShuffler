using System;
using Avalonia;
using BookShuffler.Models;
using ReactiveUI;

namespace BookShuffler.ViewModels
{
    public class IndexCardViewModel : ViewModelBase, IEntityViewModel
    {
        private Point _position;

        public Guid Id => Model.Id;

        public IndexCardViewModel(IndexCard model)
        {
            Model = model;
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