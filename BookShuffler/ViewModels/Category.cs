using Avalonia.Media;
using ReactiveUI;
using YamlDotNet.Serialization;

namespace BookShuffler.ViewModels
{
    public class Category : ViewModelBase
    {
        private string _name;
        private Color _color;
        private string _colorName;

        public Category()
        {
            this.Name = string.Empty;
            this.Color = Colors.White;
        }
        
        public int Id { get; set; }

        public string Name
        {
            get => _name;
            set => this.RaiseAndSetIfChanged(ref _name, value);
        }

        public string ColorName
        {
            get => _colorName;
            set
            {
                if (_colorName == value) return;
                _colorName = value;
                this.Color = Color.Parse(_colorName);
                this.RaisePropertyChanged(nameof(Color));
                this.RaisePropertyChanged(nameof(ColorName));
            }
        }

        [YamlIgnore]
        public Color Color { get; private set; }
        
    }
}