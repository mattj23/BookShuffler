namespace BookShuffler.Models
{
    public class KeyboardShortcuts
    {
        public string? SaveProject { get; set; } = "Ctrl+S";
        public string? AutoArrange { get; set; } = "Ctrl+Shift+T";
        public string? ExpandProject { get; set; } = "Ctrl+W";
        public string? ExpandDetached { get; set; } = "Ctrl+Shift+W";
        public string? DetachSelected { get; set; } = "Ctrl+D";
        public string? AttachSelected { get; set; } = "Ctrl+A";
        public string? CreateSection { get; set; } = null;
        public string? CreateCard { get; set; } = null;

    }
}