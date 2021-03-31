using System;
using System.Linq;
using BookShuffler.Models;
using BookShuffler.Tests.Mocks;
using BookShuffler.Tools;
using BookShuffler.ViewModels;
using Xunit;

namespace BookShuffler.Tests
{
    public class ReadWriteTests
    {

        [Fact]
        public void Section_RoundTrip_Works0()
        {
            var backing = new MockFileSystem();
            var writer = new EntityWriter(backing);
            var reader = new EntityReader(backing);

            var id = Guid.NewGuid();
            
            var section = new SectionViewModel(new Entity { 
                Id = id,
                Label = WorkflowLabel.Done,
                Notes = "notes",
                Summary = "summary"
            });

            writer.Serialize(section, "test");
            var loaded = reader.LoadSection($"test/{ProjectLoader.SectionFolderName}/{id}.yaml");

            Assert.Equal(id, loaded.Id);
            Assert.Equal(WorkflowLabel.Done, loaded.Label);
            Assert.Equal("notes", loaded.Notes);
            Assert.Equal("summary", loaded.Summary);
        }

        [Fact]
        public void Section_RoundTrip_Works1()
        {
            var backing = new MockFileSystem();
            var writer = new EntityWriter(backing);
            var reader = new EntityReader(backing);

            var id = Guid.NewGuid();
            
            var section = new SectionViewModel(new Entity { 
                Id = id,
                Label = WorkflowLabel.Done,
                Notes = null,
                Summary = "summary"
            });

            writer.Serialize(section, "test");
            var loaded = reader.LoadSection($"test/{ProjectLoader.SectionFolderName}/{id}.yaml");

            Assert.Equal(id, loaded.Id);
            Assert.Equal(WorkflowLabel.Done, loaded.Label);
            Assert.Null(loaded.Notes);
            Assert.Equal("summary", loaded.Summary);
        }
        
        [Fact]
        public void Card_RoundTrip_Works()
        {
            var backing = new MockFileSystem();
            var writer = new EntityWriter(backing);
            var reader = new EntityReader(backing);

            var id = Guid.NewGuid();

            var categories = new ProjectCategories(new[]
            {
                new CategoryViewModel(new Category
                {
                    ColorName = "Black",
                    Id = 30,
                    Name = "Test Category"
                })
            });

            var card = new IndexCardViewModel(new IndexCard()
            {
                Id = id,
                Label = WorkflowLabel.InProgress,
                Notes = "notes",
                Summary = "summary",
                Content = "content"
            }) {ProjectCategories = categories, Category = categories.All.First()};


            writer.Serialize(card, "test");
            var loaded = reader.LoadIndexCard($"test/{ProjectLoader.CardFolderName}/{id}.md");

            Assert.Equal(id, loaded.Id);
            Assert.Equal(WorkflowLabel.InProgress, loaded.Label);
            Assert.Equal("notes", loaded.Notes);
            Assert.Equal("notes", loaded.Notes);
            Assert.Equal("summary", loaded.Summary);
            Assert.Equal("content", loaded.Content);
            Assert.Equal(30, loaded.CategoryId);
        }

    }
}