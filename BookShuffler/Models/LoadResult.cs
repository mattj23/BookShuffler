using System.Collections.Generic;
using BookShuffler.ViewModels;

namespace BookShuffler.Models
{
    public class LoadResult
    {
        public LoadResult()
        {
            Unattached = new List<IEntityView>();
        }
        
        public SectionView Root { get; set; }
        
        public List<IEntityView> Unattached { get; set; }
    }
}