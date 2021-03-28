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
        
    }
}