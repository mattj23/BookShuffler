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
        
        [Fact]
        public void SimpleParse_GetsChapter()
        {
            var result = MarkdownParser.Parse(Path.Combine("Sample", "test0.md"));

            Assert.Single(result.Sections);
            Assert.Equal(2, result.Sections[0].Children.Count);
            Assert.Contains(result.Cards[0].Id, result.Sections[0].Children);
            Assert.Contains(result.Cards[1].Id, result.Sections[0].Children);

        }

        [Fact]
        public void ComplexParse_GetsSections()
        {
            var result = MarkdownParser.Parse(Path.Combine("Sample", "test1.md"));
            
            Assert.Equal(7, result.Sections.Count);
        }
        
    }
}