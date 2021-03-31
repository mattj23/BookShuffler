using BookShuffler.Tests.Fixtures;
using BookShuffler.Tests.Mocks;
using BookShuffler.Tools;
using BookShuffler.ViewModels;
using Xunit;

namespace BookShuffler.Tests
{
    public class ProjectTests : SimpleProject
    {
        /**
         * Things to test
         * - Loading
         * - Merging from import
         * - Saving restores what was written
         *  - Deleted files are removed
         *
         * - Detaching
         * - Reattaching
         *
         */

        [Fact]
        public void NewProject_Saves()
        {
            var project = ProjectViewModel.New("fake_folder");
            var builder = project.MockBuilder();
            var s0 = builder.AddSection(project.Root, new SectionViewModel("section0"));
            var c0 = builder.AddCard(s0, "card0", "card0");
            var c1 = builder.AddCard(s0, "card1", "card1");
            var c2 = builder.AddCard(project.Root, "card2", "card2");

            var storage = new MockFileSystem();
            var writer = new ProjectWriter(storage);
            
            writer.Save(project);

            Assert.Contains($"fake_folder/{ProjectLoader.SectionFolderName}/{s0.Id}.yaml", storage.Values.Keys);
            Assert.Contains($"fake_folder/{ProjectLoader.CardFolderName}/{c0.Id}.md", storage.Values.Keys);
            Assert.Contains($"fake_folder/{ProjectLoader.CardFolderName}/{c1.Id}.md", storage.Values.Keys);
            Assert.Contains($"fake_folder/{ProjectLoader.CardFolderName}/{c2.Id}.md", storage.Values.Keys);
            Assert.Contains($"fake_folder/{ProjectLoader.SectionFolderName}/{project.Root.Id}.yaml", storage.Values.Keys);
            Assert.Contains($"fake_folder/project.yaml", storage.Values.Keys);
        }
    }
}