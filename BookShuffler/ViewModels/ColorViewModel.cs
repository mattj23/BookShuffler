using System.Collections;
using System.Collections.Generic;
using Avalonia.Media;

namespace BookShuffler.ViewModels
{
    public class ColorViewModel : ViewModelBase
    {
        public string Name { get; set; }
        public Color Color { get; set; }

        public static ColorViewModel[] GetAllNamedColors()
        {
            var all = new List<ColorViewModel>();
            foreach (var info in typeof(Colors).GetProperties())
            {
                var value = info.GetValue(null);
                all.Add(new ColorViewModel {Name = info.Name, Color = value is Color ? (Color) value : default});
            }

            return all.ToArray();
        }
    }
}