using System;

namespace BookShuffler.Models
{
    public class Entity
    {
        public Guid Id { get; set; }
        public string? Summary { get; set; }
        public string? Notes { get; set; }
        public WorkflowLabel Label { get; set; } 

    }
}