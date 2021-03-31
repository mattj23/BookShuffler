using System;
using System.Collections.Generic;
using BookShuffler.Parsing;
using BookShuffler.ViewModels;

namespace BookShuffler.Models
{
    public class ProjectInfo
    {
        public ProjectInfo()
        {
            this.Categories = new List<Category>();
        }
        
        public Guid RootId { get; set; }
        
        public List<Category> Categories { get; set; }
    }
}