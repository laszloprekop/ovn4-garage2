namespace Ovn4_GarageProject2.UI;
using Domain;

// High-res text renderer: each ParkingSpot → 4×5 (vertical) or 9×3 (horizontal) char block.
// Walls and road cells remain 1×1. Separators tile to fill their slot height/width.
// Bus bays render as a single 'B' cell (zone-spanning glyph is a future step).
public static class GarageRenderer
{
    public static string[] Render(GarageCell[,] grid)
    {
        var plan = BuildRenderPlan(grid);
        var buffer = AllocateBuffer(plan);
        WriteToBuffer(grid, plan, buffer);
        return BufferToLines(buffer);
    }

    // ── Render plan ───────────────────────────────────────────────────────

    private record CellRect(int CharRow, int CharCol, int CharHeight, int CharWidth);

    private static Dictionary<(int r, int c), CellRect> BuildRenderPlan(GarageCell[,] grid)
    {
        int logRows = grid.GetLength(0);
        int logCols = grid.GetLength(1);

        var rowH = new int[logRows];
        for (int r = 0; r < logRows; r++)
            for (int c = 0; c < logCols; c++)
                rowH[r] = Math.Max(rowH[r], CharHeight(grid[r, c]));

        var colW = new int[logCols];
        for (int c = 0; c < logCols; c++)
            for (int r = 0; r < logRows; r++)
                colW[c] = Math.Max(colW[c], CharWidth(grid[r, c]));

        int[] rowY = CumulativeSum(rowH);
        int[] colX = CumulativeSum(colW);

        var plan = new Dictionary<(int, int), CellRect>();
        for (int r = 0; r < logRows; r++)
        for (int c = 0; c < logCols; c++)
            plan[(r, c)] = new CellRect(rowY[r], colX[c], rowH[r], colW[c]);

        return plan;
    }

    private static int[] CumulativeSum(int[] sizes)
    {
        var offsets = new int[sizes.Length];
        for (int i = 1; i < sizes.Length; i++)
            offsets[i] = offsets[i - 1] + sizes[i - 1];
        return offsets;
    }

    // ── Character dimensions ──────────────────────────────────────────────

    // Bus check must precede Vertical — bus spots default to Orientation.Vertical.
    private static int CharHeight(GarageCell? cell) => cell switch
    {
        ParkingSpot { AllowedVehicleType: var t } when t == typeof(Bus) => 1,
        ParkingSpot { Orientation: Orientation.Vertical }               => 5,
        ParkingSpot { Orientation: Orientation.Horizontal }             => 3,
        _                                                               => 1,
    };

    private static int CharWidth(GarageCell? cell) => cell switch
    {
        ParkingSpot { AllowedVehicleType: var t } when t == typeof(Bus) => 1,
        ParkingSpot { Orientation: Orientation.Vertical }               => 4,
        ParkingSpot { Orientation: Orientation.Horizontal }             => 9,
        _                                                               => 1,
    };

    // ── Buffer ────────────────────────────────────────────────────────────

    private static char[,] AllocateBuffer(Dictionary<(int r, int c), CellRect> plan)
    {
        int totalRows = plan.Values.Max(rect => rect.CharRow + rect.CharHeight);
        int totalCols = plan.Values.Max(rect => rect.CharCol + rect.CharWidth);
        var buffer = new char[totalRows, totalCols];
        for (int r = 0; r < totalRows; r++)
        for (int c = 0; c < totalCols; c++)
            buffer[r, c] = ' ';
        return buffer;
    }

    private static void WriteToBuffer(GarageCell[,] grid, Dictionary<(int, int), CellRect> plan, char[,] buffer)
    {
        int logRows = grid.GetLength(0);
        int logCols = grid.GetLength(1);
        for (int r = 0; r < logRows; r++)
        for (int c = 0; c < logCols; c++)
        {
            var rect = plan[(r, c)];
            WriteGlyph(buffer, rect.CharRow, rect.CharCol, GetGlyph(grid, r, c), rect.CharHeight, rect.CharWidth);
        }
    }

    private static void WriteGlyph(char[,] buffer, int startRow, int startCol,
                                   string[] glyph, int slotH, int slotW)
    {
        if (glyph.Length == 1 && glyph[0].Length == 1)
        {
            // Single-char cell: tile to fill the entire slot (separators, walls).
            char ch = glyph[0][0];
            for (int gr = 0; gr < slotH; gr++)
            for (int gc = 0; gc < slotW; gc++)
                buffer[startRow + gr, startCol + gc] = ch;
            return;
        }
        for (int gr = 0; gr < glyph.Length; gr++)
        for (int gc = 0; gc < glyph[gr].Length; gc++)
            buffer[startRow + gr, startCol + gc] = glyph[gr][gc];
    }

    // ── Glyph selection ───────────────────────────────────────────────────

    private static string[] GetGlyph(GarageCell[,] grid, int r, int c) =>
        grid[r, c] switch
        {
            WallCell                                                     => ["░"],
            RoadCell { Glyph: var g } when g != ' '                     => [$"{g}"],
            RoadCell                                                     => [" "],
            ParkingSpot { AllowedVehicleType: var t } when t == typeof(Bus) => ["B"],
            ParkingSpot spot                                             => GetSpotGlyph(spot, grid, r, c),
            _                                                            => [" "],
        };

    private static string[] GetSpotGlyph(ParkingSpot spot, GarageCell[,] grid, int r, int c)
    {
        if (!spot.IsEmpty) return GetOccupiedGlyph(spot);
        var facing = InferFacing(spot, grid, r, c);
        return (spot.Orientation, facing, spot.IsReserved, spot.HasEvCharger) switch
        {
            // Vertical 4×5
            (Orientation.Vertical, Facing.Up,   false, false) => VertEmpty_Up_NoEV,
            (Orientation.Vertical, Facing.Up,   false, true)  => VertEmpty_Up_EV,
            (Orientation.Vertical, Facing.Up,   true,  false) => VertResv_Up_NoEV,
            (Orientation.Vertical, Facing.Up,   true,  true)  => VertResv_Up_EV,
            (Orientation.Vertical, Facing.Down, false, false) => VertEmpty_Down_NoEV,
            (Orientation.Vertical, Facing.Down, false, true)  => VertEmpty_Down_EV,
            (Orientation.Vertical, Facing.Down, true,  false) => VertResv_Down_NoEV,
            (Orientation.Vertical, Facing.Down, true,  true)  => VertResv_Down_EV,
            // Horizontal 9×3
            (Orientation.Horizontal, Facing.Right, false, false) => HorizEmpty_Right_NoEV,
            (Orientation.Horizontal, Facing.Right, false, true)  => HorizEmpty_Right_EV,
            (Orientation.Horizontal, Facing.Right, true,  false) => HorizResv_Right_NoEV,
            (Orientation.Horizontal, Facing.Right, true,  true)  => HorizResv_Right_EV,
            (Orientation.Horizontal, Facing.Left,  false, false) => HorizEmpty_Left_NoEV,
            (Orientation.Horizontal, Facing.Left,  false, true)  => HorizEmpty_Left_EV,
            (Orientation.Horizontal, Facing.Left,  true,  false) => HorizResv_Left_NoEV,
            (Orientation.Horizontal, Facing.Left,  true,  true)  => HorizResv_Left_EV,
            _ => ["?"],
        };
    }

    private static string[] GetOccupiedGlyph(ParkingSpot spot) =>
        (spot.Orientation, spot.HasEvCharger) switch
        {
            (Orientation.Vertical,   false) => VertParked_NoEV,
            (Orientation.Vertical,   true)  => VertParked_EV,
            (Orientation.Horizontal, false) => HorizParked_NoEV,
            (Orientation.Horizontal, true)  => HorizParked_EV,
            _                               => ["?"],
        };

    // ── Facing inference ──────────────────────────────────────────────────

    private enum Facing { Up, Down, Left, Right }

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
        bool wallLeft  = HasWallBefore(grid, r, c, dr: 0, dc: -1);
        bool wallRight = HasWallBefore(grid, r, c, dr: 0, dc: +1);
        if (wallLeft  && !wallRight) return Facing.Left;
        if (wallRight && !wallLeft)  return Facing.Right;
        bool roadLeft  = IsRoad(grid, r, c - 1);
        bool roadRight = IsRoad(grid, r, c + 1);
        if (roadLeft  && !roadRight) return Facing.Right;
        if (roadRight && !roadLeft)  return Facing.Left;
        return spot.Id % 2 == 0 ? Facing.Right : Facing.Left;
    }

    private static bool IsRoad(GarageCell[,] grid, int r, int c)
    {
        if (r < 0 || r >= grid.GetLength(0) || c < 0 || c >= grid.GetLength(1)) return false;
        return grid[r, c] is RoadCell;
    }

    private static bool HasWallBefore(GarageCell[,] grid, int r, int c, int dr, int dc)
    {
        int rows = grid.GetLength(0), cols = grid.GetLength(1);
        int nr = r + dr, nc = c + dc;
        while (nr >= 0 && nr < rows && nc >= 0 && nc < cols)
        {
            if (grid[nr, nc] is WallCell)    return true;
            if (grid[nr, nc] is ParkingSpot) return false;
            nr += dr; nc += dc;
        }
        return true;
    }

    // ── Glyph tables — vertical 4×5 ──────────────────────────────────────
    // ↯ replaces ⚡ throughout — ↯ is 1 terminal column, ⚡ is 2 (breaks text alignment).

    static readonly string[] VertEmpty_Up_NoEV    = ["⌜  ⌝", " ╌╌ ", " ╌╌ ", " ╌╌ ", "⌞  ⌟"];
    static readonly string[] VertEmpty_Up_EV      = ["⌜ ↯⌝", " ╌╌ ", " ╌╌ ", " ╌╌ ", "⌞  ⌟"];
    static readonly string[] VertResv_Up_NoEV     = ["⌜  ⌝", " ┌╮ ", " ├╯ ", " ╵  ", "⌞  ⌟"];
    static readonly string[] VertResv_Up_EV       = ["⌜ ↯⌝", " ┌╮ ", " ├╯ ", " ╵  ", "⌞  ⌟"];
    static readonly string[] VertEmpty_Down_NoEV  = ["⌜  ⌝", " ╌╌ ", " ╌╌ ", " ╌╌ ", "⌞  ⌟"];
    static readonly string[] VertEmpty_Down_EV    = ["⌜  ⌝", " ╌╌ ", " ╌╌ ", " ╌╌ ", "⌞↯ ⌟"];
    static readonly string[] VertResv_Down_NoEV   = ["⌜  ⌝", " ┌╮ ", " ├╯ ", " ╵  ", "⌞  ⌟"];
    static readonly string[] VertResv_Down_EV     = ["⌜  ⌝", " ┌╮ ", " ├╯ ", " ╵  ", "⌞↯ ⌟"];

    // Vertical 4×5 — occupied (car outline)
    static readonly string[] VertParked_NoEV      = ["⌜  ⌝", " ┌─ ", " │  ", " └─ ", "⌞  ⌟"];
    static readonly string[] VertParked_EV        = ["⌜ ↯⌝", " ┌─ ", " │  ", " └─ ", "⌞  ⌟"];

    // ── Glyph tables — horizontal 9×3 ────────────────────────────────────

    static readonly string[] HorizEmpty_Right_NoEV  = ["⌜ ╌╌    ⌝", "  ╌╌     ", "⌞ ╌╌    ⌟"];
    static readonly string[] HorizEmpty_Right_EV    = ["⌜ ╌╌    ⌝", "  ╌╌  ↯  ", "⌞ ╌╌    ⌟"];
    static readonly string[] HorizResv_Right_NoEV   = ["⌜ ┌╮    ⌝", "  ├╯     ", "⌞ ╵     ⌟"];
    static readonly string[] HorizResv_Right_EV     = ["⌜ ┌╮    ⌝", "  ├╯   ↯ ", "⌞ ╵     ⌟"];
    static readonly string[] HorizEmpty_Left_NoEV   = ["⌜    ╌╌ ⌝", "     ╌╌  ", "⌞    ╌╌ ⌟"];
    static readonly string[] HorizEmpty_Left_EV     = ["⌜    ╌╌ ⌝", " ↯   ╌╌  ", "⌞    ╌╌ ⌟"];
    static readonly string[] HorizResv_Left_NoEV    = ["⌜    ┌╮ ⌝", "     ├╯  ", "⌞    ╵  ⌟"];
    static readonly string[] HorizResv_Left_EV      = ["⌜    ┌╮ ⌝", " ↯   ├╯  ", "⌞    ╵  ⌟"];

    // Horizontal 9×3 — occupied
    static readonly string[] HorizParked_NoEV       = ["⌜       ⌝", " ┌─────  ", "⌞ └─────⌟"];
    static readonly string[] HorizParked_EV         = ["⌜       ⌝", " ┌────↯  ", "⌞ └─────⌟"];

    // ── Buffer → lines ────────────────────────────────────────────────────

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
