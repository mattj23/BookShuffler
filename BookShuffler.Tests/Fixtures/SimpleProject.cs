using System.Security.Cryptography;
using BookShuffler.Tests.Mocks;
using BookShuffler.ViewModels;

namespace BookShuffler.Tests.Fixtures
{
    public class SimpleProject
    {
        public SimpleProject()
        {
            Project = ProjectViewModel.New("fake");
            Builder = Project.MockBuilder();
            C0 = Builder.AddCard(Project.Root, "card0", "card0");

            S0 = Builder.AddSection(Project.Root, "section0");
            C1 = Builder.AddCard(S0, "card1", "card1");
            C2 = Builder.AddCard(S0, "card2", "card2");
            S1 = Builder.AddSection(S0, "section1");
            C3 = Builder.AddCard(S1, "card3", "card3");

            S2 = Builder.AddSection(Project.Root, "section2");
            C4 = Builder.AddCard(S2, "card4", "card4");
        }

        public ProjectViewModel Project { get; }
        public ProjectBuildHelper Builder { get;  }
        public SectionViewModel S0 { get;  }
        public SectionViewModel S1 { get;  }
        public SectionViewModel S2 { get;  }
        public IndexCardViewModel C0 { get;  }
        public IndexCardViewModel C1 { get;  }
        public IndexCardViewModel C2 { get;  }
        public IndexCardViewModel C3 { get;  }
        public IndexCardViewModel C4 { get;  }

    }
}