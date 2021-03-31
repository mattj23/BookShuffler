using System;
using System.Collections.Specialized;
using System.Drawing;
using BookShuffler.Models;
using BookShuffler.ViewModels;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace BookShuffler.Tests.Mocks
{
    public static class Helpers
    {
        public static ProjectBuildHelper MockBuilder(this ProjectViewModel project)
        {
            return new ProjectBuildHelper(project);
        }

        
    }
}