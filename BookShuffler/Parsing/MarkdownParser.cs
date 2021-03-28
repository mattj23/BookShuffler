using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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

            List<Section> activeSections = new List<Section>();
            Card? activeCard = null;

            // Closure to clean up an active card
            var addCard = new Action(() =>
            {
                if (activeCard is not null)
                {
                    // Create the new card
                    var newCard = new IndexCard
                    {
                        Id = Guid.NewGuid(),
                        Summary = activeCard.Value.Summary,
                        Content = string.Join(string.Empty, activeCard.Value.Lines),
                        Notes = string.Empty
                    };

                    // Add it to the deepest level active section
                    if (activeSections.Any())
                    {
                        var deepest = activeSections.OrderByDescending(s => s.Level).First();
                        deepest.Ids.Add(newCard.Id);
                    }

                    result.Cards.Add(newCard);
                }
            });

            var finalizeAtLevel = new Action<int>(level =>
            {
                while (activeSections.Any(s => s.Level >= level))
                {
                    var deepest = activeSections.OrderByDescending(s => s.Level).First();
                    activeSections.Remove(deepest);

                    var newSection = new SectionEntity
                    {
                        Id = Guid.NewGuid(),
                        Notes = string.Empty,
                        Summary = deepest.Summary,
                        Children = deepest.Ids,
                    };

                    if (activeSections.Any())
                    {
                        var deepestRemaining = activeSections.OrderByDescending(s => s.Level).First();
                        deepestRemaining.Ids.Add(newSection.Id);
                    }

                    result.Sections.Add(newSection);
                }
            });

            foreach (var line in text)
            {
                var chapterMatch = _chapterRegex.Match(line);
                if (chapterMatch.Success)
                {
                    // Clean up any active chapters
                    finalizeAtLevel(0);

                    // Create a new L0 section
                    activeSections.Add(new Section()
                    {
                        Level = 0,
                        Summary =
                            $"[{chapterMatch.Groups[1].Value.Trim()}] {chapterMatch.Groups[2].Value.Trim()}",
                        Ids = new List<Guid>()
                    });
                    continue;
                }

                var sectionMatch = _sectionRegex.Match(line);
                if (sectionMatch.Success)
                {
                    var level = int.Parse(sectionMatch.Groups[1].Value);
                    finalizeAtLevel(level);

                    activeSections.Add(new Section()
                    {
                        Level = level,
                        Summary = sectionMatch.Groups[2].Value.Trim(),
                        Ids = new List<Guid>()
                    });
                    continue;
                }

                var cardMatch = _cardRegex.Match(line);
                if (cardMatch.Success)
                {
                    // Starting a new card
                    addCard();
                    activeCard = new Card {Lines = new List<string>(), Summary = cardMatch.Groups[1].Value.Trim()};
                    continue;
                }

                activeCard?.Lines.Add(line);
            }

            addCard();
            finalizeAtLevel(0);

            return result;
        }

        private struct Section
        {
            /// <summary>
            /// Gets the level of the section.  A level of 0 is equivalent to a chapter.
            /// </summary>
            public int Level { get; set; }
            public string Summary { get; set; }
            public List<Guid> Ids { get; set; }
        }

        private struct Card
        {
            public string Summary { get; set; }
            public List<string> Lines { get; set; }
        }
    }
}