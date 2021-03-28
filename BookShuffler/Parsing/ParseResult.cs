using System;
using System.Collections.Generic;
using BookShuffler.Models;

namespace BookShuffler.Parsing
{
    public class ParseResult
    {
        public ParseResult()
        {
            Cards = new List<IndexCard>();
            Sections = new List<SectionEntity>();
            Chapters = new List<Guid>();
        }
        
        public List<Guid> Chapters { get; set; }
        
        public List<IndexCard> Cards { get; }
        
        public List<SectionEntity> Sections { get; }
    }
}