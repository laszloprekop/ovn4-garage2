namespace Ovn4_GarageProject2.Layouts;

using Domain;

public static class LayoutParser
{
    private static readonly HashSet<char> RoadChars =
        [' ', '│', '─', '├', '┤', '┼', '┬', '┴'];

    public static Garage<T> Parse<T>(string name, string[] blueprint) where T : Vehicle
    {
        int rows = blueprint.Length;
        int cols = blueprint.Max(r => r.Length);

        var fullGrid = new GarageCell[rows, cols];
        for (int r = 0; r < rows; r++)
        for (int c = 0; c < cols; c++)
        {
            char ch = c < blueprint[r].Length ? blueprint[r][c] : ' ';
            fullGrid[r, c] = ch switch
            {
                '░' => new WallCell(),
                _ when RoadChars.Contains(ch) => new RoadCell { Glyph = ch },
                _ => CreateSpot(ch, r, c, cols),
            };
        }

        // strip the perimeter walls for logical grid
        int logicalRows = rows - 2, logicalCols = cols - 2;
        var logicalGrid = new GarageCell[logicalRows, logicalCols];

        for (int r = 1; r < rows - 1; r++)
        for (int c = 1; c < cols - 1; c++)
        {
            logicalGrid[r - 1, c - 1] = fullGrid[r, c];
        }

        // Extract wall char arrays
        char[] topWall = Enumerable.Range(1, logicalCols).Select(c => blueprint[0][c]).ToArray();
        char[] bottomWall = Enumerable.Range(1, logicalCols).Select(c => blueprint[rows - 1][c]).ToArray();
        char[] leftWall = Enumerable.Range(1, logicalRows).Select(r => blueprint[r][0]).ToArray();
        char[] rightWall = Enumerable.Range(1, logicalRows)
            .Select(r => r < blueprint.Length && cols - 1 < blueprint[r].Length
                ? blueprint[r][cols - 1]
                : '░').ToArray();

        var layout = new GarageLayout(logicalGrid, topWall, bottomWall, leftWall, rightWall,
            FindBayAnchors(logicalGrid));

        return new Garage<T>(name, logicalGrid, layout);
    }

    private static BayAnchor[] FindBayAnchors(GarageCell[,] grid)
    {
        int rows = grid.GetLength(0), cols = grid.GetLength(1);
        var seen = new HashSet<(int, int)>();
        var anchors = new List<BayAnchor>();
        for (int r = 0; r < rows; r++)
        for (int c = 0; c < cols; c++)
        {
            if (!seen.Add((r, c))) continue;
            if (grid[r, c] is not ParkingSpot { AllowedVehicleType: var t } || t != typeof(Bus)) continue;

            int spanCols = 1;
            while (c + spanCols < cols && grid[r, c + spanCols] is ParkingSpot { AllowedVehicleType: var tc } &&
                   tc == t) spanCols++;

            int spanRows = 1;
            while (r + spanRows < rows && grid[r + spanRows, c] is ParkingSpot { AllowedVehicleType: var tr } &&
                   tr == t) spanRows++;

            for (int dr = 0; dr < spanRows; dr++)
            for (int dc = 0; dc < spanCols; dc++)
            {
                seen.Add((r + dr, c + dc));
            }

            anchors.Add(new BayAnchor(r, c, spanRows, spanCols));
        }

        return [.. anchors];
    }

    private static ParkingSpot CreateSpot(char ch, int r, int c, int cols) =>
        new()
        {
            Id = r * cols + c,
            Row = r,
            Col = c,
            HasEvCharger = ch is 'C' or 'P' or '4' or '5' or '6',
            IsReserved = ch is 'p' or 'P',
            AllowedVehicleType = ch switch
            {
                'b' => typeof(Bus),
                'a' => typeof(Airplane),
                '1' or '2' or '3'
                    or '4' or '5' or '6' => typeof(Motorcycle),
                _ => null,
            },
        };
}
