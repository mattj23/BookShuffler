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

        [Fact]
        public void NewProject_Loads()
        {
            var storage = new MockFileSystem();
            var writer = new ProjectWriter(storage);
            writer.Save(Project);

            var reader = new ProjectLoader(storage);
            var result = reader.Load("fake");

            var loaded = ProjectViewModel.FromLoad(result);

            Assert.Equal(3, loaded.Root.Entities.Count);
            Assert.Equal(C0.Id, loaded.Root.Entities[0].Id);
            Assert.Equal(S0.Id, loaded.Root.Entities[1].Id);
            Assert.Equal(S2.Id, loaded.Root.Entities[2].Id);

            var s0 = loaded.Root.Entities[1] as SectionViewModel;
            Assert.Equal(3, s0.Entities.Count);
            Assert.Equal(C1.Id, s0.Entities[0].Id);
            Assert.Equal(C2.Id, s0.Entities[1].Id);
            Assert.Equal(S1.Id, s0.Entities[2].Id);

            var s1 = s0.Entities[2] as SectionViewModel;
            Assert.Single(s1.Entities);
            Assert.Equal(C3.Id, s1.Entities[0].Id);

            var s2 = loaded.Root.Entities[2] as SectionViewModel;
            Assert.Single(s2.Entities);
            Assert.Equal(C4.Id, s2.Entities[0].Id);

            var c3 = s0.Entities[0] as IndexCardViewModel;
            Assert.Equal("Default", c3.Category.Name);

        }

        [Fact]
        public void DetachItems()
        {
            Project.DetachEntity(S0);

            Assert.DoesNotContain(S0, Project.Root.Entities);
            Assert.Contains(S0, Project.DetachedEntities);
        }
    }
}