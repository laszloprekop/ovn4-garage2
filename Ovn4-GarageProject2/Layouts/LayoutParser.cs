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
        var grid = new GarageCell[rows, cols];

        for (int r = 0; r < rows; r++)
        for (int c = 0; c < cols; c++)
        {
            char ch = c < blueprint[r].Length ? blueprint[r][c] : ' ';
            grid[r, c] = ch switch
            {
                '░' => new WallCell(),
                _ when RoadChars.Contains(ch) => new RoadCell(),
                _ => CreateSpot(ch, r, c, cols),
            };
        }

        return new Garage<T>(name, grid);
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
