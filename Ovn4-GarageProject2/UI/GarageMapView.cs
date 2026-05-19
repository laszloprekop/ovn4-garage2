namespace Ovn4_GarageProject2.UI;

using Terminal.Gui.Drawing;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;
using Ovn4_GarageProject2.Domain;

// Variable-size grid: ParkingSpot cells are 2-wide × 2-tall.
// Separator/wall columns and rows collapse to 1 terminal cell in that dimension.
public class GarageMapView : View
{
    public GarageMapView(GarageCell[,] grid) => Rebuild(grid);

    public void Rebuild(GarageCell[,] grid)
    {
        RemoveAll();
        int rows = grid.GetLength(0);
        int cols = grid.GetLength(1);

        // Width=2 for columns that contain at least one ParkingSpot; 1 otherwise.
        int[] colW = new int[cols];
        int[] colX = new int[cols];
        for (int c = 0; c < cols; c++)
        {
            for (int r = 0; r < rows; r++)
                if (grid[r, c] is ParkingSpot) { colW[c] = 2; break; }
            if (colW[c] == 0) colW[c] = 1;
            colX[c] = c == 0 ? 0 : colX[c - 1] + colW[c - 1];
        }

        // Height=2 for rows that contain at least one ParkingSpot; 1 otherwise.
        int[] rowH = new int[rows];
        int[] rowY = new int[rows];
        for (int r = 0; r < rows; r++)
        {
            for (int c = 0; c < cols; c++)
                if (grid[r, c] is ParkingSpot) { rowH[r] = 2; break; }
            if (rowH[r] == 0) rowH[r] = 1;
            rowY[r] = r == 0 ? 0 : rowY[r - 1] + rowH[r - 1];
        }

        for (int r = 0; r < rows; r++)
        for (int c = 0; c < cols; c++)
        {
            var (attr, sym, fill) = SymbolRenderer.CellRendering(grid[r, c]);
            var scheme = new Scheme(attr);
            int x = colX[c], y = rowY[r], w = colW[c], h = rowH[r];

            if (w == 1 && h == 1)
            {
                // Separator / wall in a thin row — single label.
                var lbl = new Label { Text = sym.ToString(), X = x, Y = y, Width = 1, Height = 1 };
                lbl.SetScheme(scheme);
                Add(lbl);
            }
            else if (w == 2 && h == 1)
            {
                // Wide column, thin row (e.g. wall across a parking column).
                var l = new Label { Text = sym.ToString(),  X = x,     Y = y, Width = 1, Height = 1 };
                var r2 = new Label { Text = fill.ToString(), X = x + 1, Y = y, Width = 1, Height = 1 };
                l.SetScheme(scheme); r2.SetScheme(scheme);
                Add(l, r2);
            }
            else if (w == 1 && h == 2)
            {
                // Narrow column, tall row (e.g. │ beside a parking bay).
                var top = new Label { Text = sym.ToString(), X = x, Y = y,     Width = 1, Height = 1 };
                var bot = new Label { Text = sym.ToString(), X = x, Y = y + 1, Width = 1, Height = 1 };
                top.SetScheme(scheme); bot.SetScheme(scheme);
                Add(top, bot);
            }
            else
            {
                // Full 2×2 parking cell.
                var bottom = grid[r, c] is ParkingSpot ? "··" : "  ";
                var topSymbol  = new Label { Text = sym.ToString(),  X = x,     Y = y,     Width = 1, Height = 1 };
                var topFill = new Label { Text = fill.ToString(), X = x + 1, Y = y,     Width = 1, Height = 1 };
                var bottomRow  = new Label { Text = bottom,          X = x,     Y = y + 1, Width = 2, Height = 1 };
                topSymbol.SetScheme(scheme); topFill.SetScheme(scheme); bottomRow.SetScheme(scheme);
                Add(topSymbol, topFill, bottomRow);
            }
        }

        SetNeedsDraw();
    }
}
