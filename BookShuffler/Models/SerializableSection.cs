using System;
using System.Collections.Generic;

namespace BookShuffler.Models
{
    public class SerializableSection
    {
        public SerializableSection()
        {
            Children = new List<Child>();
        }

        public Guid Id { get; set; }
        public string? Summary { get; set; }
        public string? Notes { get; set; }
        public WorkflowLabel Label { get; set; }
        public List<Child> Children { get; set; }

        public struct Child
        {
            public Guid Id { get; set; }
            public double X { get; set; }
            public double Y { get; set; }
        }

        public Entity ToEntity()
        {
            return new Entity
            {
                Id = this.Id,
                Summary = this.Summary,
                Notes = this.Notes,
                Label = this.Label
            };
        }
    }
    
}