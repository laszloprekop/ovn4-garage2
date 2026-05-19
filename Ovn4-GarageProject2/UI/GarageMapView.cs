namespace Ovn4_GarageProject2.UI;

using Terminal.Gui.Drawing;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;
using Ovn4_GarageProject2.Domain;

// Renders a GarageCell[,] as a 2×2 coloured block per logical cell.
// Top row col*2: sym (reservation/type/occupation). col*2+1: fill ('↯' or ' '). Bottom row: background fill.
public class GarageMapView : View
{
    public GarageMapView(GarageCell[,] grid) => Rebuild(grid);

    public void Rebuild(GarageCell[,] grid)
    {
        RemoveAll();
        int rows = grid.GetLength(0);
        int cols = grid.GetLength(1);

        for (int r = 0; r < rows; r++)
        for (int c = 0; c < cols; c++)
        {
            var (attr, sym, fill) = SymbolRenderer.CellRendering(grid[r, c]);
            var bottom = grid[r, c] switch
            {
                WallCell    => "░░",
                ParkingSpot => "··",
                _           => "  ",   // road / void
            };
            var scheme = new Scheme(attr);

            var topSymbol = new Label { Text = sym.ToString(),  X = c * 2,     Y = r * 2, Width = 1, Height = 1 };
            var topFill   = new Label { Text = fill.ToString(), X = c * 2 + 1, Y = r * 2, Width = 1, Height = 1 };
            var bottomRow = new Label { Text = bottom, X = c * 2, Y = r * 2 + 1, Width = 2, Height = 1 };
            topSymbol.SetScheme(scheme);
            topFill.SetScheme(scheme);
            bottomRow.SetScheme(scheme);
            Add(topSymbol, topFill, bottomRow);
        }

        SetNeedsDraw();
    }
}
