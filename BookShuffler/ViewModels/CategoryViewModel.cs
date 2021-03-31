using Avalonia.Media;
using BookShuffler.Models;
using ReactiveUI;
using YamlDotNet.Serialization;

namespace BookShuffler.ViewModels
{
    public class CategoryViewModel : ViewModelBase
    {
        private Color _color;

        public CategoryViewModel(Category model)
        {
            this.Model = model;
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
                this.Color = Color.Parse(this.Model.ColorName);
                this.RaisePropertyChanged(nameof(Color));
                this.RaisePropertyChanged(nameof(ColorName));
            }
        }

        [YamlIgnore]
        public Color Color { get; private set; }
        
    }
}