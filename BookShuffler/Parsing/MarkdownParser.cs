using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using BookShuffler.Models;

namespace BookShuffler.Parsing
{
    public static class MarkdownParser
    {
        private static Regex _chapterRegex = new Regex(@"<!-- chapter (.+)::(.+)-->", RegexOptions.IgnoreCase);
        private static Regex _sectionRegex = new Regex(@"<!-- section l(\d+)::(.+)-->", RegexOptions.IgnoreCase);
        private static Regex _cardRegex = new Regex(@"<!-- card::(.+)-->", RegexOptions.IgnoreCase);
        
        public static ParseResult Parse(string path)
        {
            var result = new ParseResult();

            var text = File.ReadLines(path);

            Chapter? activeChapter = null;
            List<Section> activeSections = new List<Section>();
            Card? activeCard = null;

            var addCard = new Action(() =>
            {
                if (activeCard is not null)
                {
                    result.Cards.Add(new IndexCard
                    {
                        Summary = activeCard.Value.Summary,
                        Content = string.Join(string.Empty, activeCard.Value.Lines),
                        Notes = string.Empty
                    });
                }
            });

            foreach (var line in text)
            {
                var chapterMatch = _chapterRegex.Match(line);
                if (chapterMatch.Success)
                {
                    // Clean up any active chapter
                    continue;
                }

                var sectionMatch = _sectionRegex.Match(line);
                if (sectionMatch.Success)
                {
                    continue;
                }

                var cardMatch = _cardRegex.Match(line);
                if (cardMatch.Success)
                {
                    // Starting a new card
                    addCard();
                    activeCard = new Card {Lines = new List<string>(), Summary = cardMatch.Groups[1].ToString().Trim()};
                    continue;
                }

                activeCard?.Lines.Add(line);
            }

            addCard();


            return result;
        }


        private struct Chapter
        {
            public string Name { get; set; }
            public string Summary { get; set; }
        }

        private struct Section
        {
            public int Level { get; set; }
            public string Summary { get; set; }
        }

        private struct Card
        {
            public string Summary { get; set; }
            public List<string> Lines { get; set; }
        }
    }
}