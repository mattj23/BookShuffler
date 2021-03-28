using Avalonia;
using BookShuffler.Models;

namespace BookShuffler.ViewModels
{
    public interface IEntityView
    {
        Point Position { get; set; }
        string? Summary { get; set; }
        string? Notes { get; set; }
        WorkflowLabel Label { get; set; }
    }
}