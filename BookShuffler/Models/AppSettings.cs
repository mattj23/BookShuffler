using Avalonia.Input;

namespace BookShuffler.Models
{
    public class AppSettings
    {
        public AppSettings()
        {
            this.KeyBindings = new KeyboardShortcuts();
        }
        
        public string? LastOpenedProject { get; set; }
        
        public double? ProjectTreeScale { get; set; }
        public double? CanvasScale { get; set; }

        public KeyboardShortcuts KeyBindings { get; set; }
    }
}