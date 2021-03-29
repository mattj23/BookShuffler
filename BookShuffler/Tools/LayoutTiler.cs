using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Controls.Shapes;
using BookShuffler.ViewModels;

namespace BookShuffler.Parsing
{
    /// <summary>
    /// Tool to perform the tiling layout of a section's contents
    /// </summary>
    public static class LayoutTiler
    {
        private const double TileWidth = 400;
        private const double TileHeight = 400;
        private const double Spacing = 20;

        public static void AutoTile(this SectionView section, double width)
        {
            // Assumes that the size of each tile is 400x400
            double x = Spacing;
            double y = Spacing;
            foreach (var entity in section.Entities)
            {
                if (x + TileWidth > width)
                {
                    x = Spacing;
                    y += TileHeight + Spacing;
                }

                entity.Position = new Point(x, y);
                x += TileWidth + Spacing;
            }
        }

        public static void ResortOrder(this SectionView section)
        {
            var working = new List<IEntityView>(section.Entities);
            section.Entities.Clear();

            while (working.Any())
            {
                // Find topmost
                var upperPosition = working.Min(e => e.Position.Y);
                var row = working.Where(e => Math.Abs(e.Position.Y - upperPosition) < TileHeight / 2.0)
                    .OrderBy(e => e.Position.X)
                    .ToArray();

                foreach (var e in row)
                {
                    section.Entities.Add(e);
                    working.Remove(e);
                }
            }
        }
    }
    
}