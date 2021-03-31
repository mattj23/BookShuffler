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
            var storage = new MockFileSystem();
            var writer = new ProjectWriter(storage);
            
            writer.Save(Project);

            Assert.Contains($"fake/{ProjectLoader.SectionFolderName}/{Project.Root.Id}.yaml", storage.Values.Keys);
            Assert.Contains($"fake/{ProjectLoader.SectionFolderName}/{S0.Id}.yaml", storage.Values.Keys);
            Assert.Contains($"fake/{ProjectLoader.SectionFolderName}/{S1.Id}.yaml", storage.Values.Keys);
            Assert.Contains($"fake/{ProjectLoader.SectionFolderName}/{S2.Id}.yaml", storage.Values.Keys);

            Assert.Contains($"fake/{ProjectLoader.CardFolderName}/{C0.Id}.md", storage.Values.Keys);
            Assert.Contains($"fake/{ProjectLoader.CardFolderName}/{C1.Id}.md", storage.Values.Keys);
            Assert.Contains($"fake/{ProjectLoader.CardFolderName}/{C2.Id}.md", storage.Values.Keys);
            Assert.Contains($"fake/{ProjectLoader.CardFolderName}/{C3.Id}.md", storage.Values.Keys);
            Assert.Contains($"fake/{ProjectLoader.CardFolderName}/{C4.Id}.md", storage.Values.Keys);

            Assert.Contains($"fake/project.yaml", storage.Values.Keys);
        }
    }
}