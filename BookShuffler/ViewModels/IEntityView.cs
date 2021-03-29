using System;
using Avalonia;
using BookShuffler.Models;

namespace BookShuffler.ViewModels
{
    public interface IEntityView
    {
        Guid Id { get; }
        Point Position { get; set; }
        string Summary { get; set; }
        string? Notes { get; set; }
        WorkflowLabel Label { get; set; }
        
        string Content { get; }
    }
}