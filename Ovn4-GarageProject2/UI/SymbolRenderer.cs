namespace Ovn4_GarageProject2.UI;

using Terminal.Gui.Drawing; // Attribute, ColorName16
using Ovn4_GarageProject2.Domain;

public static class SymbolRenderer
{
    // Text-only fallback. Each logical cell → 2 chars wide, 2 rows tall.
    public static string[] Render(GarageCell[,] grid)
    {
        int rows = grid.GetLength(0);
        int cols = grid.GetLength(1);
        var lines = new string[rows * 2];

        for (int row = 0; row < rows; row++)
        {
            var top = new System.Text.StringBuilder(cols * 2);
            var bottom = new System.Text.StringBuilder(cols * 2);
            for (int col = 0; col < cols; col++)
            {
                var (_, sym, fill) = CellRendering(grid[row, col]);
                top.Append($"{sym}{fill}");
                bottom.Append(grid[row, col] switch
                {
                    WallCell    => "░░",
                    ParkingSpot => "··",
                    _           => "  ",
                });
            }

            lines[row * 2] = top.ToString();
            lines[row * 2 + 1] = bottom.ToString();
        }

        return lines;
    }

    // Returns sym (col*2) and fill (col*2+1), both Width=1 in GarageMapView.
    // sym  → reservation flag / vehicle-type hint / occupation state
    // fill → base type glyph, overridden by more specific conditions (earlier cases win)
    public static (Attribute Attr, char Sym, char Fill) CellRendering(GarageCell? cell)
    {
        char fill = cell switch
        {
            null or RoadCell                   => ' ',   // road / void
            WallCell                           => '░',   // solid wall
            ParkingSpot { HasEvCharger: true } => '↯',  // EV overrides plain bay
            ParkingSpot                        => '·',   // empty bay
            _                                  => ' ',
        };
        var (attr, sym) = cell switch
        {
            null => (Palette.Road, 'x'),
            WallCell => (Palette.Wall, '░'),
            RoadCell => (Palette.Road, ' '),
            // Reserved — check before occupation
            ParkingSpot { IsReserved: true } s when !s.IsEmpty && s.HasEvCharger => (Palette.ResvParkedEv, 'P'),
            ParkingSpot { IsReserved: true } s when !s.IsEmpty => (Palette.ResvParked, 'P'),
            ParkingSpot { IsReserved: true, HasEvCharger: true } => (Palette.ResvEmptyEv, 'P'),
            ParkingSpot { IsReserved: true } => (Palette.ResvEmpty, 'p'),
            // Occupied
            ParkingSpot { IsEmpty: false, HasEvCharger: true } => (Palette.ParkedEv, ' '),
            ParkingSpot { IsEmpty: false } => (Palette.Parked, ' '),
            // Empty — type hints for dedicated bays
            ParkingSpot { AllowedVehicleType: var t } when t == typeof(Bus) => (Palette.Empty, 'B'),
            ParkingSpot { AllowedVehicleType: var t } when t == typeof(Airplane) => (Palette.Empty, 'A'),
            ParkingSpot { HasEvCharger: true } => (Palette.EmptyEv, '·'),
            ParkingSpot => (Palette.Empty, '·'),
            _ => (Palette.Road, '?'),
        };
        return (attr, sym, fill);
    }

    private static class Palette
    {
        // fg                      bg
        public static readonly Attribute Road = new(ColorName16.DarkGray, ColorName16.Black);
        public static readonly Attribute Wall = new(ColorName16.DarkGray, ColorName16.Black);
        public static readonly Attribute Empty = new(ColorName16.DarkGray, ColorName16.Black);
        public static readonly Attribute EmptyEv = new(ColorName16.BrightBlue, ColorName16.Black);
        public static readonly Attribute Parked = new(ColorName16.DarkGray, ColorName16.Green);
        public static readonly Attribute ParkedEv = new(ColorName16.Blue, ColorName16.Green);
        public static readonly Attribute ResvEmpty = new(ColorName16.DarkGray, ColorName16.Black);
        public static readonly Attribute ResvEmptyEv = new(ColorName16.BrightBlue, ColorName16.Blue);
        public static readonly Attribute ResvParked = new(ColorName16.Black, ColorName16.Green);
        public static readonly Attribute ResvParkedEv = new(ColorName16.Blue, ColorName16.Green);
    }
}
