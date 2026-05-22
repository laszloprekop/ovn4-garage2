using Ovn4_GarageProject2.Layouts;

namespace Ovn4_GarageProject2.UI;

using Domain;

/// <summary>
/// Converts a <see cref="GarageCell"/> grid into an array of Unicode text lines for terminal display.
/// </summary>
/// <remarks>
/// The rendering pipeline has four stages:
/// <list type="number">
///   <item><description><see cref="AllocateBuffer"/> — allocates the backing <c>char[,]</c> array, filled with spaces.</description></item>
///   <item><description><see cref="BufferToLines"/> — serialises rows to right-trimmed strings.</description></item>
/// </list>
/// <para>
/// This class is split across two files using the <c>partial</c> keyword:
/// <list type="bullet">
///   <item><description><c>GarageRenderer.cs</c> — rendering pipeline and glyph dispatch (this file).</description></item>
///   <item><description><c>GarageRenderer.Glyphs.cs</c> — static Unicode sprite tables.</description></item>
/// </list>
/// Separating data from logic keeps this file focused and avoids loading sprite tables into
/// the reader's context when working on rendering behaviour.
/// </para>
/// </remarks>
public static partial class GarageRenderer
{
    public record SpotHighlight(int CharRow, int CharCol, int CharHeight, int CharWidth);

    public static string[] Render(GarageLayout layout)
    {
        var buffer = AllocateBuffer(layout.LogicalGrid);
        RenderBaseCells(layout.LogicalGrid, buffer);
        PaintBayAnchors(layout, buffer);
        WriteBorderStrips(layout, buffer);
        return BufferToLines(buffer);
    }

    public static (string[] Lines, SpotHighlight? Highlight) RenderWithHighlight(
        GarageLayout layout, int highlightSpotId)
    {
        var buffer = AllocateBuffer(layout.LogicalGrid);
        RenderBaseCells(layout.LogicalGrid, buffer); // pass 1
        PaintBayAnchors(layout, buffer); // pass 2
        ReplaceStripePadding(layout, buffer);
        WriteBorderStrips(layout, buffer); // pass 3
        var lines = BufferToLines(buffer);

        var grid = layout.LogicalGrid;
        int logRows = grid.GetLength(0), logCols = grid.GetLength(1);
        for (int r = 0; r < logRows; r++)
        for (int c = 0; c < logCols; c++)
        {
            if (grid[r, c] is not ParkingSpot { Id: var id } || id != highlightSpotId) continue;
            var bay = layout.BayAnchors.FirstOrDefault(anchor => anchor.Row == r && anchor.Col == c);
            int spanRows = bay?.SpanRows ?? 1;
            int spanCols = bay?.SpanCols ?? 1;
            return (lines, new SpotHighlight(r * 6 + 1, c * 5 + 1, spanRows * 6, spanCols * 5));
        }

        return (lines, null);
    }



    // ── Buffer ────────────────────────────────────────────────────────────

    private static char[,] AllocateBuffer(GarageCell[,] grid)
    {
        // 1,1 - top left border row
        int bufferHeight = 1 + grid.GetLength(0) * 6, bufferWidth = 1 + grid.GetLength(1) * 5;

        var buffer = new char[bufferHeight, bufferWidth];
        for (int r = 0; r < bufferHeight; r++)
        for (int c = 0; c < bufferWidth; c++)
            buffer[r, c] = ' ';

        return buffer;
    }

    // painter
    private static void RenderBaseCells(GarageCell[,] grid, char[,] buffer)
    {
        int logicalRows = grid.GetLength(0);
        int logicalCols = grid.GetLength(1);
        for (int row = 0; row < logicalRows; row++)
        for (int col = 0; col < logicalCols; col++)
        {
            var glyph = GetGlyph(grid, row, col);
            if (glyph.Length == 0) continue; // when road or bus - no stamp
            StampGlyph(buffer, row * 6 + 1, col * 5 + 1, glyph);
        }
    }


    // ── Glyph selection ───────────────────────────────────────────────────

    private static string[] GetGlyph(GarageCell[,] grid, int r, int c) =>
        grid[r, c] switch
        {
            RoadCell => [],
            ParkingSpot { AllowedVehicleType: var t } when t == typeof(Bus) => [],
            ParkingSpot spot => GetSpotGlyph(spot, grid, r, c),
            _ => [],
        };


    /// <summary>
    /// Selects the correct empty or reserved sprite for <paramref name="spot"/> based on
    /// orientation, inferred entry facing, reserved flag, and EV-charger flag.
    /// </summary>
    /// <seealso cref="InferFacing"/>
    private static string[] GetSpotGlyph(ParkingSpot spot, GarageCell[,] grid, int r, int c)
    {
        if (spot.IsMotorcycleMode) return GetMotorcycleGlyph(spot);
        if (!spot.IsEmpty) return GetOccupiedGlyph(spot);
        var facing = InferFacing(spot, grid, r, c);
        return (spot.Orientation, facing, spot.IsReserved, spot.HasEvCharger) switch
        {
            // Vertical 4×5
            (Orientation.Vertical, Facing.Up, false, false) => VertEmptyUpNoEv,
            (Orientation.Vertical, Facing.Up, false, true) => VertEmptyUpEv,
            (Orientation.Vertical, Facing.Up, true, false) => VertResvUpNoEv,
            (Orientation.Vertical, Facing.Up, true, true) => VertResvUpEv,
            (Orientation.Vertical, Facing.Down, false, false) => VertEmptyDownNoEv,
            (Orientation.Vertical, Facing.Down, false, true) => VertEmptyDownEv,
            (Orientation.Vertical, Facing.Down, true, false) => VertResvDownNoEv,
            (Orientation.Vertical, Facing.Down, true, true) => VertResvDownEv,
            // Horizontal 9×3
            (Orientation.Horizontal, Facing.Right, false, false) => HorizEmptyRightNoEv,
            (Orientation.Horizontal, Facing.Right, false, true) => HorizEmptyRightEv,
            (Orientation.Horizontal, Facing.Right, true, false) => HorizResvRightNoEv,
            (Orientation.Horizontal, Facing.Right, true, true) => HorizResvRightEv,
            (Orientation.Horizontal, Facing.Left, false, false) => HorizEmptyLeftNoEv,
            (Orientation.Horizontal, Facing.Left, false, true) => HorizEmptyLeftEv,
            (Orientation.Horizontal, Facing.Left, true, false) => HorizResvLeftNoEv,
            (Orientation.Horizontal, Facing.Left, true, true) => HorizResvLeftEv,
            _ => ["?"],
        };
    }

    // Grid render Pass 1
    private static void StampGlyph(char[,] buffer, int startRow, int startCol, string[] glyph)
    {
        int glyphH = glyph.Length;
        int glyphW = glyph.Max(row => row.Length);

        // Glyph fits the grid perfectly.
        if (glyphH % 6 == 0 && glyphW % 5 == 0)
        {
            for (int gr = 0; gr < glyphH; gr++)
            for (int gc = 0; gc < glyphW; gc++)
            {
                buffer[startRow + gr, startCol + gc] = glyph[gr][gc];
            }

            return;
        }

        // Safety fitting: round to nearest cell side multiple, center content, add `·` padding
        int cellH = (int)Math.Ceiling(glyphH / 6.0) * 6;
        int cellW = (int)Math.Ceiling(glyphW / 5.0) * 5;
        // center ascii art (minus padding
        int offR = (cellH - 1 - glyphH) / 2;
        int offC = (cellW - 1 - glyphW) / 2;

        for (int gr = 0; gr < glyphH; gr++)
        for (int gc = 0; gc < glyph[gr].Length; gc++)
        {
            buffer[startRow + offR + gr, startCol + offC + gc] = glyph[gr][gc];
        }

        // write padding placeholder
        for (int gr = 0; gr < cellH; gr++) buffer[startRow + gr, startCol + cellW - 1] = '·';
        for (int gc = 0; gc < cellW; gc++) buffer[startRow + cellH - 1, startCol + gc] = '·';
    }


    // Grid render Pass 2
    private static void PaintBayAnchors(GarageLayout layout, char[,] buffer)
    {
        foreach (var anchor in layout.BayAnchors)
        {
            bool isEmpty = layout.LogicalGrid[anchor.Row, anchor.Col] is not ParkingSpot spot || spot.IsEmpty;
            var sprite = (anchor.SpanRows, anchor.SpanCols, isEmpty) switch
            {
                (3, 2, true) => BusTwoByThreeEmpty,
                (3, 2, false) => BusTwoByThree,
                _ => Array.Empty<string>(),
            };
            if (sprite.Length == 0) continue;
            StampGlyph(buffer, anchor.Row * 6 + 1, anchor.Col * 5 + 1, sprite);
        }
    }

    // Grid render Pass 3
    private static void ReplaceStripePadding(GarageLayout layout, char[,] buffer)
    {
        var grid = layout.LogicalGrid;
        int logicalRows = grid.GetLength(0), logicalCols = grid.GetLength(1);

        for (int row = 0; row < logicalRows; row++)
        for (int col = 0; col < logicalCols; col++)
        {
            // is inside a multi-cell?
            var bay = layout.BayAnchors.FirstOrDefault(anchor =>
                row >= anchor.Row && row < anchor.Row + anchor.SpanRows &&
                col >= anchor.Col && col < anchor.Col + anchor.SpanCols);
            bool isLastBayCol = bay is null || col == bay.Col + bay.SpanCols - 1;
            bool isLastBayRow = bay is null || row == bay.Row + bay.SpanRows - 1;

            int bufferRow = row * 6 + 1, bufferCol = col * 5 + 1;

            // When to skip interior bay columns
            if (isLastBayCol)
            {
                char ch = RightEdgeChar(grid, layout.RightWall, row, col, logicalCols);
                for (int dr = 0; dr < 5; dr++) buffer[bufferRow + dr, bufferCol + 4] = ch;
            }

            // when to skip interior bay rows
            if (isLastBayRow)
            {
                char ch = BottomEdgeChar(grid, layout.BottomWall, row, col, logicalRows);
                for (int dc = 0; dc < 4; dc++) buffer[bufferRow + 5, bufferCol + dc] = ch;
            }

            // corner at outer bottom-right?
            if (isLastBayCol && isLastBayRow)
                buffer[bufferRow + 5, bufferCol + 4] = CornerChar(grid, layout, row, col, logicalRows, logicalCols);
        }
    }

    private static char RightEdgeChar(GarageCell[,] grid, char[] rightWall, int r, int c, int logicalCols)
    {
        bool atEdge = c + 1 >= logicalCols;
        bool wallRight = atEdge ? rightWall[r] == '░' : grid[r, c + 1] is WallCell;
        if (wallRight) return '░';
        bool spotHere = grid[r, c] is ParkingSpot;
        bool spotRight = !atEdge && grid[r, c + 1] is ParkingSpot;
        return spotHere || spotRight ? '│' : ' ';
    }

    private static char BottomEdgeChar(GarageCell[,] grid, char[] bottomWall, int r, int c, int logicalRows)
    {
        bool atEdge = r + 1 >= logicalRows;
        bool wallBelow = atEdge ? bottomWall[c] == '░' : grid[r + 1, c] is WallCell;
        if (wallBelow) return '░';
        return grid[r, c] is ParkingSpot ? '─' : ' ';
    }

    private static char CornerChar(GarageCell[,] grid, GarageLayout layout, int r, int c, int logRows, int logCols)
    {
        bool rightOutOfBounds = c + 1 >= logCols;
        bool bottomOutOfBounds = r + 1 >= logRows;

        bool wallRight = rightOutOfBounds ? layout.RightWall[r] == '░' : grid[r, c + 1] is WallCell;
        bool wallBottom = bottomOutOfBounds ? layout.BottomWall[c] == '░' : grid[r + 1, c] is WallCell;

        if (wallRight || wallBottom) return '░';

        bool tl = grid[r, c] is ParkingSpot;
        bool tr = !rightOutOfBounds && grid[r, c + 1] is ParkingSpot;
        bool bl = !bottomOutOfBounds && grid[r + 1, c] is ParkingSpot;
        bool br = !rightOutOfBounds && !bottomOutOfBounds && grid[r + 1, c + 1] is ParkingSpot;

        bool up = tl || tr;
        bool down = bl || br;

        if (!up || !down) return ' ';

        return (tl, tr) switch // left-right connectors
        {
            (false, true) => '├',
            (true, false) => '┤',
            (true, true) => '┼',
            _ => ' ',
        };
    }

    private static void WriteBorderStrips(GarageLayout layout, char[,] buffer)
    {
        int logicalRows = layout.LogicalGrid.GetLength(0);
        int logicalCols = layout.LogicalGrid.GetLength(1);

        buffer[0, 0] = '░'; // top-left origin

        // Top border, stretched x5
        for (int c = 0; c < logicalCols; c++)
        {
            char ch = layout.TopWall[c];
            for (int dc = 0; dc < 5; dc++)
                buffer[0, c * 5 + 1 + dc] = ch;
        }

        // Left border stretched x6
        for (int r = 0; r < logicalRows; r++)
        {
            char ch = layout.LeftWall[r];
            for (int dr = 0; dr < 6; dr++)
                buffer[r * 6 + 1 + dr, 0] = ch;
        }
        // right and bottom walls are dealt with populating placeholder `·`s
    }

    private static string[] GetMotorcycleGlyph(ParkingSpot spot)
    {
        int count = spot.GetVehicles().Count();
        return (count, spot.HasEvCharger) switch
        {
            (1, false) => VertMoto1NoEv,
            (2, false) => VertMoto2NoEv,
            (_, false) => VertMoto3NoEv,
            (1, true) => VertMoto1Ev,
            (2, true) => VertMoto2Ev,
            (_, true) => VertMoto3Ev,
        };
    }

    /// <summary>
    /// Selects the occupied sprite for <paramref name="spot"/> based on orientation and EV-charger flag.
    /// </summary>
    private static string[] GetOccupiedGlyph(ParkingSpot spot) =>
        (spot.Orientation, spot.HasEvCharger) switch
        {
            (Orientation.Vertical, false) => VertParkedNoEv,
            (Orientation.Vertical, true) => VertParkedEv,
            (Orientation.Horizontal, false) => HorizParkedNoEv,
            (Orientation.Horizontal, true) => HorizParkedEv,
            _ => ["?"],
        };

    // ── Facing inference ──────────────────────────────────────────────────

    /// <summary>The direction a driver faces when entering a parking spot from the road.</summary>
    private enum Facing
    {
        Up,
        Down,
        Left,
        Right
    }

    /// <summary>
    /// Infers the entry-facing direction for <paramref name="spot"/> by inspecting its neighbours.
    /// </summary>
    /// <remarks>
    /// For vertical spots the heuristic checks which adjacent cell is a road. For horizontal spots
    /// it checks for a wall on each side of the row. When the context is ambiguous (road or wall on
    /// both sides) the spot's ID parity is used as a tie-breaker, giving adjacent spots opposite
    /// facings — a reasonable default for back-to-back bay layouts.
    /// </remarks>
    private static Facing InferFacing(ParkingSpot spot, GarageCell[,] grid, int r, int c)
    {
        if (spot.Orientation == Orientation.Vertical)
        {
            bool roadAbove = IsRoad(grid, r - 1, c);
            bool roadBelow = IsRoad(grid, r + 1, c);
            if (roadAbove && !roadBelow) return Facing.Up;
            if (roadBelow && !roadAbove) return Facing.Down;
            return spot.Id % 2 == 0 ? Facing.Up : Facing.Down;
        }

        bool wallLeft = HasWallBefore(grid, r, c, dr: 0, dc: -1);
        bool wallRight = HasWallBefore(grid, r, c, dr: 0, dc: +1);
        if (wallLeft && !wallRight) return Facing.Left;
        if (wallRight && !wallLeft) return Facing.Right;
        bool roadLeft = IsRoad(grid, r, c - 1);
        bool roadRight = IsRoad(grid, r, c + 1);
        if (roadLeft && !roadRight) return Facing.Right;
        if (roadRight && !roadLeft) return Facing.Left;
        return spot.Id % 2 == 0 ? Facing.Right : Facing.Left;
    }

    /// <summary>
    /// Returns <see langword="true"/> if the cell at <c>(<paramref name="r"/>, <paramref name="c"/>)</c>
    /// is within bounds and is a <see cref="RoadCell"/>.
    /// </summary>
    private static bool IsRoad(GarageCell[,] grid, int r, int c)
    {
        if (r < 0 || r >= grid.GetLength(0) || c < 0 || c >= grid.GetLength(1)) return false;
        return grid[r, c] is RoadCell;
    }

    /// <summary>
    /// Scans from <c>(<paramref name="r"/>, <paramref name="c"/>)</c> in direction
    /// <c>(<paramref name="dr"/>, <paramref name="dc"/>)</c> and returns <see langword="true"/>
    /// if a <see cref="WallCell"/> is encountered before any <see cref="ParkingSpot"/> or the grid edge.
    /// </summary>
    private static bool HasWallBefore(GarageCell[,] grid, int r, int c, int dr, int dc)
    {
        int rows = grid.GetLength(0), cols = grid.GetLength(1);
        int nr = r + dr, nc = c + dc;
        while (nr >= 0 && nr < rows && nc >= 0 && nc < cols)
        {
            if (grid[nr, nc] is WallCell) return true;
            if (grid[nr, nc] is ParkingSpot) return false;
            nr += dr;
            nc += dc;
        }

        return true;
    }

    // ── Buffer → lines ────────────────────────────────────────────────────

    /// <summary>
    /// Converts the completed <paramref name="buffer"/> into an array of strings,
    /// one per row, right-trimmed of trailing spaces.
    /// </summary>
    private static string[] BufferToLines(char[,] buffer)
    {
        int rows = buffer.GetLength(0);
        int cols = buffer.GetLength(1);
        var lines = new string[rows];
        for (int r = 0; r < rows; r++)
        {
            var sb = new System.Text.StringBuilder(cols);
            for (int c = 0; c < cols; c++)
                sb.Append(buffer[r, c]);
            lines[r] = sb.ToString().TrimEnd();
        }

        return lines;
    }
}
