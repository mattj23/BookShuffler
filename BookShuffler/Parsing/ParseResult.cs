using System.Collections.Generic;
using BookShuffler.Models;

namespace BookShuffler.Parsing
{
    public class ParseResult
    {
        public ParseResult()
        {
            Cards = new List<IndexCard>();
        }
        
        public List<IndexCard> Cards { get; }
    }
}