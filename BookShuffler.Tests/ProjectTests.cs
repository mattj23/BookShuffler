using BookShuffler.ViewModels;
using Xunit;

namespace BookShuffler.Tests
{
    public class ProjectTests
    {

        [Fact]
        public void NewProject_ImportNew()
        {
            var project = ProjectViewModel.New("fake_folder");
        }
    }
}