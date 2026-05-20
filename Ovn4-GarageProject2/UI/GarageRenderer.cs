namespace Ovn4_GarageProject2.UI;

using Domain;

/// <summary>
/// Converts a <see cref="GarageCell"/> grid into an array of Unicode text lines for terminal display.
/// </summary>
/// <remarks>
/// The rendering pipeline has four stages:
/// <list type="number">
///   <item><description><see cref="BuildRenderPlan"/> — assigns a character-space bounding rectangle to every logical cell.</description></item>
///   <item><description><see cref="AllocateBuffer"/> — allocates the backing <c>char[,]</c> array, filled with spaces.</description></item>
///   <item><description><see cref="WriteToBuffer"/> — stamps each cell's glyph into the buffer.</description></item>
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
    /// <summary>The character-space bounding rectangle of a single parking spot, used for highlight overlays.</summary>
    public record SpotHighlight(int CharRow, int CharCol, int CharHeight, int CharWidth);

    /// <summary>
    /// Renders <paramref name="grid"/> into an array of text lines ready for terminal display.
    /// </summary>
    /// <param name="grid">The logical grid of garage cells produced by <see cref="LayoutParser"/>.</param>
    /// <returns>One string per character row, right-trimmed of trailing spaces.</returns>
    public static string[] Render(GarageCell[,] grid)
    {
        var plan = BuildRenderPlan(grid);
        var buffer = AllocateBuffer(plan);
        WriteToBuffer(grid, plan, buffer);
        return BufferToLines(buffer);
    }

    /// <summary>
    /// Renders <paramref name="grid"/> and also returns the character-space rectangle of the spot
    /// with <paramref name="highlightSpotId"/>, so the caller can apply a colour overlay to that region.
    /// </summary>
    /// <returns>The rendered lines and the highlight rectangle, or <see langword="null"/> if the spot is not found.</returns>
    public static (string[] Lines, SpotHighlight? Highlight) RenderWithHighlight(
        GarageCell[,] grid, int highlightSpotId)
    {
        var plan = BuildRenderPlan(grid);
        var buffer = AllocateBuffer(plan);
        WriteToBuffer(grid, plan, buffer);
        var lines = BufferToLines(buffer);

        int logRows = grid.GetLength(0), logCols = grid.GetLength(1);
        for (int r = 0; r < logRows; r++)
        for (int c = 0; c < logCols; c++)
        {
            if (grid[r, c] is ParkingSpot { Id: var id } && id == highlightSpotId)
            {
                var rect = plan[(r, c)];
                // Bus anchor: extend highlight to the bottom of the full zone.
                if (grid[r, c] is ParkingSpot { AllowedVehicleType: var t } && t == typeof(Bus))
                {
                    int lastRow = r;
                    while (lastRow + 1 < logRows
                           && grid[lastRow + 1, c] is ParkingSpot { AllowedVehicleType: var nt }
                           && nt == typeof(Bus))
                        lastRow++;
                    var last = plan[(lastRow, c)];
                    return (lines, new SpotHighlight(rect.CharRow, rect.CharCol,
                        last.CharRow + last.CharHeight - rect.CharRow, rect.CharWidth));
                }

                return (lines, new SpotHighlight(rect.CharRow, rect.CharCol, rect.CharHeight, rect.CharWidth));
            }
        }

        return (lines, null);
    }

    // ── Render plan ───────────────────────────────────────────────────────

    /// <summary>Character-space bounding rectangle for one logical cell.</summary>
    private record CellRect(int CharRow, int CharCol, int CharHeight, int CharWidth);

    /// <summary>
    /// Computes a character-space bounding rectangle for every logical cell in <paramref name="grid"/>.
    /// </summary>
    /// <remarks>
    /// Three passes refine the initial 1×1 defaults:
    /// <list type="number">
    ///   <item><description>Parking-spot dimensions set spot columns to 4 or 9 wide and spot rows to 5 or 3 tall.</description></item>
    ///   <item><description>Lane-column pass: columns that contain a plain road cell (<c>' '</c>) in a spot row
    ///     are widened to <c>laneW</c> (5 for vertical garages, 10 for horizontal).
    ///     Separator columns are implicitly excluded because they hold <c>│</c>, not <c>' '</c>, in spot rows.</description></item>
    ///   <item><description>Lane-row pass: rows that contain a plain road cell in a spot column are heightened to
    ///     <c>laneH</c> (5 for vertical garages, 3 for horizontal).
    ///     A snapshot of column widths taken before pass 2 (<c>spotColW</c>) prevents expanded lane
    ///     columns from incorrectly qualifying wall rows as lane rows.</description></item>
    /// </list>
    /// </remarks>
    /// <param name="grid">The logical grid to analyse.</param>
    /// <returns>A dictionary mapping each <c>(row, col)</c> index to its <see cref="CellRect"/>.</returns>
    private static Dictionary<(int r, int c), CellRect> BuildRenderPlan(GarageCell[,] grid)
    {
        int logRows = grid.GetLength(0);
        int logCols = grid.GetLength(1);

        var busSatellites = ComputeBusSatellites(grid);

        var rowH = new int[logRows];
        for (int r = 0; r < logRows; r++)
        for (int c = 0; c < logCols; c++)
            rowH[r] = Math.Max(rowH[r], CharHeight(grid[r, c]));

        var colW = new int[logCols];
        for (int c = 0; c < logCols; c++)
        for (int r = 0; r < logRows; r++)
            colW[c] = Math.Max(colW[c], CharWidth(grid[r, c], busSatellites));

        // Detect parking orientations for lane sizing.
        bool hasVert = false, hasHoriz = false;
        for (int r = 0; r < logRows; r++)
        for (int c = 0; c < logCols; c++)
        {
            if (grid[r, c] is ParkingSpot { Orientation: Orientation.Vertical }) hasVert = true;
            if (grid[r, c] is ParkingSpot { Orientation: Orientation.Horizontal }) hasHoriz = true;
        }

        int laneW = hasHoriz ? 10 : hasVert ? 5 : 1;
        int laneH = hasVert ? 5 : hasHoriz ? 3 : 1;

        // Snapshot initial colW so the row-pass can identify spot columns.
        var spotColW = (int[])colW.Clone();

        // Expand lane columns: a lane column has a plain-road cell in a spot row (rowH > 1).
        // Separator columns (│ etc.) have only their glyph char in spot rows, never ' ',
        // so the Glyph: ' ' pattern already excludes them — no extra wall check needed.
        for (int c = 0; c < logCols; c++)
        {
            if (colW[c] > 1) continue;
            bool hasParking = false, isLaneCol = false;
            for (int r = 0; r < logRows; r++)
            {
                if (grid[r, c] is ParkingSpot) hasParking = true;
                if (rowH[r] > 1 && grid[r, c] is RoadCell { Glyph: ' ' }) isLaneCol = true;
            }

            if (!hasParking && isLaneCol) colW[c] = laneW;
        }

        // Expand lane rows: a lane row has a plain-road cell in a spot column (spotColW > 1).
        // Bus spots (CharHeight == 1) do NOT count as hasParking — they live in lane rows.
        // Separator rows (containing ─ ┼ etc.) are excluded.
        for (int r = 0; r < logRows; r++)
        {
            if (rowH[r] > 1) continue;
            bool hasParking = false, hasSep = false, isLaneRow = false;
            for (int c = 0; c < logCols; c++)
            {
                if (CharHeight(grid[r, c]) > 1) hasParking = true;
                if (grid[r, c] is RoadCell rc && IsSeparatorGlyph(rc.Glyph)) hasSep = true;
                if (spotColW[c] > 1 && grid[r, c] is RoadCell { Glyph: ' ' }) isLaneRow = true;
            }

            if (!hasParking && !hasSep && isLaneRow) rowH[r] = laneH;
        }

        int[] rowY = CumulativeSum(rowH);
        int[] colX = CumulativeSum(colW);

        var plan = new Dictionary<(int, int), CellRect>();
        for (int r = 0; r < logRows; r++)
        for (int c = 0; c < logCols; c++)
            plan[(r, c)] = new CellRect(rowY[r], colX[c], rowH[r], colW[c]);

        return plan;
    }

    /// <summary>Converts an array of slot sizes into an array of cumulative start offsets.</summary>
    private static int[] CumulativeSum(int[] sizes)
    {
        var offsets = new int[sizes.Length];
        for (int i = 1; i < sizes.Length; i++)
            offsets[i] = offsets[i - 1] + sizes[i - 1];
        return offsets;
    }

    /// <summary>
    /// Returns <see langword="true"/> if <paramref name="g"/> is a box-drawing separator character
    /// (<c>│ ─ ┼ ├ ┤ ┬ ┴</c>).
    /// </summary>
    private static bool IsSeparatorGlyph(char g) =>
        g is '│' or '─' or '┼' or '├' or '┤' or '┬' or '┴';

    // ── Character dimensions ──────────────────────────────────────────────

    /// <summary>
    /// Returns the character height of <paramref name="cell"/>'s sprite.
    /// </summary>
    /// <remarks>
    /// Bus spots are matched before the general vertical case because <see cref="LayoutParser"/>
    /// assigns <see cref="Orientation.Vertical"/> as the default orientation, which would
    /// otherwise give bus spots a height of 5 instead of 1.
    /// </remarks>
    private static int CharHeight(GarageCell? cell) => cell switch
    {
        ParkingSpot { AllowedVehicleType: var t } when t == typeof(Bus) => 1,
        ParkingSpot { Orientation: Orientation.Vertical } => 5,
        ParkingSpot { Orientation: Orientation.Horizontal } => 3,
        _ => 1,
    };

    /// <summary>Returns the character width of <paramref name="cell"/>'s sprite.</summary>
    private static int CharWidth(GarageCell? cell, HashSet<int> busSatellites) => cell switch
    {
        ParkingSpot { AllowedVehicleType: var t } s when t == typeof(Bus) && busSatellites.Contains(s.Id) => 0,
        ParkingSpot { AllowedVehicleType: var t } when t == typeof(Bus) => 9,
        ParkingSpot { Orientation: Orientation.Vertical } => 4,
        ParkingSpot { Orientation: Orientation.Horizontal } => 9,
        _ => 1,
    };

    // Satellite = bus spot that has another bus spot immediately to its left.
    private static HashSet<int> ComputeBusSatellites(GarageCell[,] grid)
    {
        int rows = grid.GetLength(0), cols = grid.GetLength(1);
        var satellites = new HashSet<int>();
        for (int r = 0; r < rows; r++)
        for (int c = 1; c < cols; c++)
        {
            if (grid[r, c] is ParkingSpot
                {
                    AllowedVehicleType: var t
                } spot && t == typeof(Bus)
                       && grid[r, c - 1] is ParkingSpot
                       {
                           AllowedVehicleType: var lt
                       } && lt == typeof(Bus))
                satellites.Add(spot.Id);
        }

        return satellites;
    }

    // ── Buffer ────────────────────────────────────────────────────────────

    /// <summary>
    /// Allocates a <c>char[,]</c> large enough to hold every cell described by <paramref name="plan"/>,
    /// initialised with spaces.
    /// </summary>
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

    /// <summary>Iterates every logical cell and writes its glyph into <paramref name="buffer"/>.</summary>
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

    /// <summary>
    /// Writes <paramref name="glyph"/> into <paramref name="buffer"/> at the given position.
    /// </summary>
    /// <remarks>
    /// Single-character glyphs (walls, separators) are <em>tiled</em> to fill the entire
    /// <paramref name="slotH"/>×<paramref name="slotW"/> slot, so lane columns and lane rows
    /// always display a continuous surface rather than isolated symbols.
    /// Multi-character glyphs (parking-spot sprites) are written verbatim, top-left aligned.
    /// </remarks>
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

    /// <summary>
    /// Returns the sprite rows for the cell at <c>grid[<paramref name="r"/>, <paramref name="c"/>]</c>.
    /// </summary>
    private static string[] GetGlyph(GarageCell[,] grid, int r, int c) =>
        grid[r, c] switch
        {
            WallCell => ["░"],
            RoadCell { Glyph: var g } when g != ' ' => [$"{g}"],
            RoadCell => [" "],
            ParkingSpot { AllowedVehicleType: var t } spot when t == typeof(Bus) => GetBusGlyph(grid, r, c, spot),
            ParkingSpot spot => GetSpotGlyph(spot, grid, r, c),
            _ => [" "],
        };

    // Returns the correct 5-row slice for an anchor bus cell, or [] for a satellite.
    private static string[] GetBusGlyph(GarageCell[,] grid, int r, int c, ParkingSpot spot)
    {
        // Satellite: bus spot to the left → render nothing (anchor covers this area).
        if (c > 0 && grid[r, c - 1] is ParkingSpot { AllowedVehicleType: var lt } && lt == typeof(Bus))
            return [];

        // Anchor: count adjacent bus rows above to pick the correct slice (0=top, 1=mid, 2=bot).
        int slice = 0;
        int nr = r - 1;
        while (nr >= 0 && grid[nr, c] is ParkingSpot { AllowedVehicleType: var at } && at == typeof(Bus))
        {
            slice++;
            nr--;
        }

        // Occupancy is read from the zone's top anchor so all slices agree,
        // even when only one cell was seeded via ParkAtSpot.
        bool isEmpty = grid[r - slice, c] is ParkingSpot top ? top.IsEmpty : spot.IsEmpty;

        return (slice, isEmpty) switch
        {
            (0, true) => BusTwoByThreeEmptyTop,
            (1, true) => BusTwoByThreeEmptyMid,
            (2, true) => BusTwoByThreeEmptyBot,
            (0, false) => BusTwoByThreeTop,
            (1, false) => BusTwoByThreeMid,
            (2, false) => BusTwoByThreeBot,
            _ => [],
        };
    }

    /// <summary>
    /// Selects the correct empty or reserved sprite for <paramref name="spot"/> based on
    /// orientation, inferred entry facing, reserved flag, and EV-charger flag.
    /// </summary>
    /// <seealso cref="InferFacing"/>
    /// <seealso cref="GarageRenderer.Glyphs.cs"/>
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

    private static string[] GetMotorcycleGlyph(ParkingSpot spot)
    {
        int count = spot.GetVehicles().Count();
        return (count, spot.HasEvCharger) switch
        {
            (1, false) => VertMoto1NoEv,
            (2, false) => VertMoto2NoEv,
            (_, false) => VertMoto3NoEv,
            (1, true)  => VertMoto1Ev,
            (2, true)  => VertMoto2Ev,
            (_, true)  => VertMoto3Ev,
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
