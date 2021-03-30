using System.Collections.Generic;
using BookShuffler.ViewModels;

namespace BookShuffler.Models
{
    public class LoadResult
    {
        public LoadResult()
        {
            Unattached = new List<IEntityViewModel>();
        }
        
        public SectionViewModel Root { get; set; }
        
        public List<IEntityViewModel> Unattached { get; set; }
    }
}