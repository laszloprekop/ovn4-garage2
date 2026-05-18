using Ovn4_GarageProject2.Domain;

namespace Ovn4_GarageProject2.UI;

public static class SymbolRenderer
{
    public static string[] Render(GarageCell[,] grid)
    {
        int rows = grid.GetLength(0);
        int cols = grid.GetLength(1);
        var lines = new string[rows];

        for (int r = 0; r < rows; r++)
        {
            var sb = new System.Text.StringBuilder(cols);
            for (int c = 0; c < cols; c++)
                sb.Append(CellSymbol(grid[r, c]));
            lines[r] = sb.ToString();
        }

        return lines;
    }

    private static char CellSymbol(GarageCell? cell) => cell switch
    {
        null => ' ',
        WallCell => '█',
        RoadCell => '·',
        ParkingSpot { AllowedVehicleType: var t } s when t == typeof(Bus) => s.IsEmpty ? 'B' : '■',
        ParkingSpot { AllowedVehicleType: var t } s when t == typeof(Airplane) => s.IsEmpty ? 'A' : '■',
        ParkingSpot { IsEmpty: false, HasEvCharger: true } => '◆',
        ParkingSpot { IsEmpty: false } => '■',
        ParkingSpot { IsReserved: true, HasEvCharger: true } => '◈',
        ParkingSpot { IsReserved: true } => '▣',
        ParkingSpot { HasEvCharger: true } => '◇',
        ParkingSpot => '□',
        _ => '?',
    };
}
