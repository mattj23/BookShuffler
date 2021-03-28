using System;
using System.IO;
using BookShuffler.Models;
using BookShuffler.Parsing;
using Xunit;

namespace BookShuffler.Tests
{
    public class ParserTests
    {
        [Fact]
        public void SimpleParse_CardsCount()
        {
            var result = MarkdownParser.Parse(Path.Combine("Sample", "test0.md"));
            
            Assert.Equal(2, result.Cards.Count);
            Assert.Equal("This is a description of the card", result.Cards[0].Summary);
            Assert.Equal("Description of second card", result.Cards[1].Summary);
            Assert.Equal("This is the actual content of the card", result.Cards[0].Content.Trim());
            Assert.Equal("This is the content of the second card", result.Cards[1].Content.Trim());
        }
    }
}