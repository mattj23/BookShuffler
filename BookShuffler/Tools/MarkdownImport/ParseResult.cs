using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.XPath;
using BookShuffler.Models;
using BookShuffler.ViewModels;

namespace BookShuffler.Tools.MarkdownImport
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

        /// <summary>
        /// Convert the results of the parse to a LoadResult object, which nominally represents the data structure
        /// of a new project and can be used either to create one or to merge into an existing project
        /// </summary>
        /// <returns></returns>
        public LoadResult ToLoadResult()
        {
            var result = new LoadResult();

            var root = new Entity{Id = Guid.NewGuid(), Summary = "Project Root"};
            result.Root = new SectionViewModel(root);

            // To construct the various tree structures, we'll start by creating view models for every entity
            // and putting them all into the single unified dictionary. 
            foreach (var indexCard in this.Cards)
            {
                result.AllEntities[indexCard.Id] = new IndexCardViewModel(indexCard);
            }

            foreach (var section in this.Sections)
            {
                result.AllEntities[section.Id] = new SectionViewModel(section);
            }

            // Now we can attach child entities to each section. This does not check to enforce that the graph is
            // acyclic.  We'll copy the dictionary keys so that we can remove entities as we attach them
            // to their parents. When we're finished the entities left in the set will all be top level
            // entities
            var working = result.AllEntities.Keys.ToHashSet();

            foreach (var section in this.Sections)
            {
                var vm = (SectionViewModel) result.AllEntities[section.Id];
                foreach (var childId in section.Children)
                {
                    if (working.Contains(childId))
                    {
                        working.Remove(childId);
                    }
                    else
                    {
                        // Error
                    }

                    vm.Entities.Add(result.AllEntities[childId]);
                }
            }

            // Attach all of the root node's children
            foreach (var gid in this.Chapters)
            {
                working.Remove(gid);
                result.Root.Entities.Add(result.AllEntities[gid]);
            }

            result.Unattached.AddRange(working.Select(gid => result.AllEntities[gid]));

            return result;
        }
    }
}