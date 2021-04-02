using Avalonia.Media;
using BookShuffler.Models;
using ReactiveUI;
using YamlDotNet.Serialization;

namespace BookShuffler.ViewModels
{
    public class CategoryViewModel : ViewModelBase
    {
        private IBrush _color;

        public CategoryViewModel(Category model)
        {
            this.Model = model;
            this.Color = new SolidColorBrush(Avalonia.Media.Color.Parse(this.Model.ColorName));
        }

        public int Id => this.Model.Id;
        
        public Category Model { get; }

        public string Name
        {
            get => this.Model.Name;
            set
            {
                if (this.Model.Name == value) return;
                this.Model.Name = value;
                this.RaisePropertyChanged();
            }
        }

        public string ColorName
        {
            get => this.Model.ColorName;
            set
            {
                if (this.Model.ColorName == value) return;
                this.Model.ColorName = value;
                this.RaisePropertyChanged(nameof(ColorName));
                this.Color = new SolidColorBrush(Avalonia.Media.Color.Parse(this.Model.ColorName));
            }
        }

        [YamlIgnore]
        public IBrush Color
        {
            get => _color;
            set => this.RaiseAndSetIfChanged(ref _color, value);
        }
    }
}