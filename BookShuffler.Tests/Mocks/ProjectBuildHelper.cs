using System;
using System.Collections.Generic;
using System.Runtime.InteropServices.ComTypes;
using BookShuffler.Models;
using BookShuffler.ViewModels;

namespace BookShuffler.Tests.Mocks
{
    public class ProjectBuildHelper
    {
        private readonly ProjectViewModel _project;

        public ProjectBuildHelper(ProjectViewModel project)
        {
            _project = project;
        }

        public SectionViewModel AddSection(SectionViewModel parent, SectionViewModel child)
        {
            return _project.AddNewSection(parent, child);
        }

        public SectionViewModel AddSection(SectionViewModel parent, string summary)
        {
            return this.AddSection(parent, new SectionViewModel(summary));
        }

        public IndexCardViewModel AddCard(SectionViewModel parent, string summary, string content = "")
        {
            var vm = new IndexCardViewModel(new IndexCard()
            {
                Content = content,
                Id = Guid.NewGuid(),
                Summary = summary
            });

            return _project.AddNewCard(parent, vm);
        }
    }
}