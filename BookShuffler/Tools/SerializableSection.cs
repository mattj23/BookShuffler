using System;
using System.Collections.Generic;
using BookShuffler.Models;

namespace BookShuffler.Parsing
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
    }
    
}