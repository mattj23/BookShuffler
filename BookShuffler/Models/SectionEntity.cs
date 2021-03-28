using System;
using System.Collections.Generic;

namespace BookShuffler.Models
{
    public class SectionEntity : Entity
    {
        public List<Guid> Children { get; set; }
    }
}