using System;
using System.Collections.Generic;
using BookShuffler.ViewModels;

namespace BookShuffler.Models
{
    public class LoadResult
    {
        public LoadResult()
        {
            Unattached = new List<IEntityViewModel>();
            AllEntities = new Dictionary<Guid, IEntityViewModel>();
        }

        public string? ProjectFolder { get; set; }
        
        public SectionViewModel Root { get; set; }
        
        public List<IEntityViewModel> Unattached { get; set; }
        
        public Dictionary<Guid, IEntityViewModel> AllEntities { get; set; }

        public ProjectInfo Info { get; set; }
    }
}