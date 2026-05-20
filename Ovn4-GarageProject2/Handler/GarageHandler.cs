using Ovn4_GarageProject2.Domain;

namespace Ovn4_GarageProject2.Handler;

public class GarageHandler : IHandler
{
    private IGarage? _garage;

    private static readonly Dictionary<Type, (int W, int H)> Footprints = new()
    {
        [typeof(Car)] = (1, 1),
        [typeof(Motorcycle)] = (1, 1),
        [typeof(Boat)] = (1, 2),
        [typeof(Bus)] = (2, 3),
        [typeof(Airplane)] = (4, 2),
    };

    public void SetGarage(IGarage garage) => _garage = garage;

    public GarageCell[,] GetGrid() => _garage?.GetGrid() ?? new GarageCell[0, 0];

    public bool ParkAtSpot(int spotId, Vehicle vehicle)
    {
        if (_garage is null) return false;
        var spot = _garage.GetGrid().Cast<GarageCell>()
            .OfType<ParkingSpot>()
            .FirstOrDefault(s => s.Id == spotId);
        return spot?.TryPark(vehicle) ?? false;
    }

    public IEnumerable<Vehicle> GetAllVehicles() =>
        _garage?.GetAll() ?? Enumerable.Empty<Vehicle>();

    public IEnumerable<(string Type, int Count)> GetVehicleTypeCounts() =>
        _garage?.GetAll()
            .GroupBy(vehicle => vehicle.GetType().Name)
            .Select(g => (Type: g.Key, Count: g.Count()))
        ?? Enumerable.Empty<(string, int)>();

    public bool Remove(string regNumber)
    {
        if (_garage is null) return false;
        bool found = false;
        foreach (var spot in _garage.GetGrid().Cast<GarageCell>().OfType<ParkingSpot>())
            if (spot.TryUnpark(regNumber))
                found = true;
        return found;
    }

    public int? Park(Vehicle vehicle)
    {
        if (_garage is null) return null;
        if (GetAllVehicles().Any(v => v.RegNumber.Equals(vehicle.RegNumber, StringComparison.OrdinalIgnoreCase)))
            return null;

        var (width, height) = Footprints.GetValueOrDefault(vehicle.GetType(), (1, 1));
        Type? requiredZone = vehicle is Bus or Airplane ? vehicle.GetType() : null;
        var anchor = FindFreeSpot(width, height, requiredZone);
        if (anchor is null) return null;

        var grid = _garage.GetGrid();
        for (int r = anchor.Row; r < anchor.Row + height; r++)
        for (int c = anchor.Col; c < anchor.Col + width; c++)
            if (grid[r, c] is ParkingSpot spot)
                spot.TryPark(vehicle);

        return anchor.Id;
    }

    public ParkingSpot? FindFreeSpot(int width, int height, Type? requiredType = null)
    {
        if (_garage is null) return null;
        var grid = _garage.GetGrid();
        int rows = grid.GetLength(0);
        int cols = grid.GetLength(1);

        for (int r = 0; r <= rows - height; r++)
        {
            for (int c = 0; c <= cols - width; c++)
            {
                if (RectangleFree(grid, r, c, width, height, requiredType))
                    return (ParkingSpot)grid[r, c];
            }
        }

        return null;
    }

    private static bool RectangleFree(GarageCell[,] grid, int startRow, int startCol, int width, int height,
        Type? requiredType)
    {
        for (int r = startRow; r < startRow + height; r++)
        {
            for (int c = startCol; c < startCol + width; c++)
            {
                if (grid[r, c] is not ParkingSpot spot) return false;
                if (!spot.CanMerge) return false;
                if (requiredType is not null && spot.AllowedVehicleType != requiredType) return false;
            }
        }

        return true;
    }
}
