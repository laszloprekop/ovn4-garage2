using Ovn4_GarageProject2.Domain;
using Ovn4_GarageProject2.Layouts;

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
    public GarageLayout GetLayout() => _garage?.Layout ?? new GarageLayout(new GarageCell[0, 0], [], [], [], [], []);

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
        if (found)
        {
            for (int i = 0; i < _sessions.Count; i++)
            {
                if (_sessions[i].RegNumber.Equals(regNumber, StringComparison.OrdinalIgnoreCase)
                    && _sessions[i].End is null)
                    _sessions[i] = _sessions[i] with { End = DateTime.Now };
            }
        }

        return found;
    }

    public int? Park(Vehicle vehicle)
    {
        if (_garage is null) return null;
        if (string.IsNullOrWhiteSpace(vehicle.RegNumber)) return null;
        if (GetAllVehicles().Any(v => v.RegNumber.Equals(vehicle.RegNumber, StringComparison.OrdinalIgnoreCase)))
            return null;

        // Park in the reserved spot linked to this reg number if it is still free.
        var reserved = _garage.GetGrid().Cast<GarageCell>()
            .OfType<ParkingSpot>()
            .FirstOrDefault(s => s.IsReserved && s.IsEmpty
                && s.ReservedForRegNumber?.Equals(vehicle.RegNumber, StringComparison.OrdinalIgnoreCase) == true);
        if (reserved is not null && reserved.TryPark(vehicle))
        {
            var rs = new ParkingSession(reserved.Id, vehicle.RegNumber, DateTime.Now);
            reserved.ActiveSession = rs;
            _sessions.Add(rs);
            return reserved.Id;
        }

        if (vehicle is Motorcycle)
        {
            var partial = _garage.GetGrid().Cast<GarageCell>()
                .OfType<ParkingSpot>()
                .FirstOrDefault(s => s.IsMotorcycleMode && s.GetVehicles().Count() < 3);
            // First motorcycle session is tracked on the GarageCell, the last two separately
            if (partial is not null && partial.TryPark(vehicle))
            {
                var ms = new ParkingSession(partial.Id, vehicle.RegNumber, DateTime.Now);
                _sessions.Add(ms);
                return partial.Id;
            }
        }
        var (width, height) = Footprints.GetValueOrDefault(vehicle.GetType(), (1, 1));
        Type? requiredZone = vehicle is Bus or Airplane ? vehicle.GetType() : null;
        var anchor = FindFreeSpot(width, height, requiredZone);
        if (anchor is null) return null;

        var grid = _garage.GetGrid();
        for (int r = anchor.Row; r < anchor.Row + height; r++)
        for (int c = anchor.Col; c < anchor.Col + width; c++)
            if (grid[r, c] is ParkingSpot spot)
                spot.TryPark(vehicle);

        var session = new ParkingSession(anchor.Id, vehicle.RegNumber, DateTime.Now);
        anchor.ActiveSession = session;
        _sessions.Add(session);
        return anchor.Id;
    }

    public IEnumerable<Vehicle> FindByReg(string partialRegNumber) =>
        _garage?.GetAll()
            .Where(v => v.RegNumber.Contains(
                partialRegNumber,
                StringComparison.OrdinalIgnoreCase))
        ?? Enumerable.Empty<Vehicle>();

    private ParkingSpot? FindFreeSpot(int width, int height, Type? requiredType = null)
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

    public IEnumerable<Vehicle> Search(string? colour, string? wheelCount, Type? vehicleType) =>
        _garage?.GetAll()
            .Where(v =>
                (colour is null || v.Colour.Contains(colour, StringComparison.OrdinalIgnoreCase)) &&
                (wheelCount is null || v.WheelCount.Contains(wheelCount, StringComparison.OrdinalIgnoreCase)) &&
                (vehicleType is null || v.GetType() == vehicleType))
        ?? Enumerable.Empty<Vehicle>();


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

    private bool PlaceVehicle(int spotId, Vehicle vehicle)
    {
        if (_garage is null) return false;
        var spot = _garage.GetGrid().Cast<GarageCell>()
            .OfType<ParkingSpot>()
            .FirstOrDefault(s => s.Id == spotId);
        if (spot is null) return false;
        return spot.TryPark(vehicle);
    }

    private static Vehicle CreateVehicleFromRecord(VehicleRecord r) => r.Type switch
    {
        "Car"        => new Car        { RegNumber = r.RegNumber, Colour = r.Colour, WheelCount = r.WheelCount, FuelType = r.Extra },
        "Motorcycle" => new Motorcycle { RegNumber = r.RegNumber, Colour = r.Colour, WheelCount = r.WheelCount, CylinderVolume = r.Extra },
        "Bus"        => new Bus        { RegNumber = r.RegNumber, Colour = r.Colour, WheelCount = r.WheelCount, NumberOfSeats = r.Extra },
        "Boat"       => new Boat       { RegNumber = r.RegNumber, Colour = r.Colour, WheelCount = r.WheelCount,
                            Length = double.TryParse(r.Extra, out double l) ? l : 10 },
        "Airplane"   => new Airplane   { RegNumber = r.RegNumber, Colour = r.Colour, WheelCount = r.WheelCount,
                            NumberOfEngines = int.TryParse(r.Extra, out int n) ? n : 1 },
        _ => throw new ArgumentException($"Unknown vehicle type: {r.Type}")
    };

    private static string ExtraOf(Vehicle v) => v switch
    {
        Car c        => c.FuelType,
        Motorcycle m => m.CylinderVolume,
        Bus b        => b.NumberOfSeats,
        Boat bo      => bo.Length.ToString(),
        Airplane a   => a.NumberOfEngines.ToString(),
        _            => string.Empty
    };

    public void LoadState(GarageState state)
    {
        if (_garage is null) return;
        var grid = _garage.GetGrid();

        foreach (var r in state.ReservedSpots)
        {
            var spot = grid.Cast<GarageCell>().OfType<ParkingSpot>().FirstOrDefault(s => s.Id == r.SpotId);
            if (spot is null) continue;
            spot.IsReserved = true;
            spot.ReservedForRegNumber = r.RegNumber;
        }

        foreach (var vr in state.Vehicles)
        {
            var vehicle = CreateVehicleFromRecord(vr);
            if (!PlaceVehicle(vr.SpotId, vehicle)) continue;
            var anchor = grid.Cast<GarageCell>().OfType<ParkingSpot>().FirstOrDefault(s => s.Id == vr.SpotId);
            var session = new ParkingSession(vr.SpotId, vr.RegNumber, DateTime.Now);
            if (anchor is not null) anchor.ActiveSession = session;
            _sessions.Add(session);
        }

        if (state.Sessions.Count > 0)
        {
            _sessions.Clear();
            _sessions.AddRange(state.Sessions);
        }
    }

    public GarageState SaveState()
    {
        var grid = _garage?.GetGrid() ?? new GarageCell[0, 0];

        var vehicles = grid.Cast<GarageCell>()
            .OfType<ParkingSpot>()
            .SelectMany(s => s.GetVehicles().Select(v => (Spot: s, Vehicle: v)))
            .DistinctBy(x => x.Vehicle.RegNumber)
            .Select(x => new VehicleRecord(
                x.Vehicle.GetType().Name,
                x.Spot.Id,
                x.Vehicle.RegNumber,
                x.Vehicle.Colour,
                x.Vehicle.WheelCount,
                ExtraOf(x.Vehicle)))
            .ToList();

        var reserved = grid.Cast<GarageCell>()
            .OfType<ParkingSpot>()
            .Where(s => s.IsReserved && s.ReservedForRegNumber is not null)
            .Select(s => new ReservedRecord(s.Id, s.ReservedForRegNumber!))
            .ToList();

        return new GarageState(vehicles, reserved, _sessions.ToList());
    }

    private readonly List<ParkingSession> _sessions = [];
    public IReadOnlyList<ParkingSession> GetSessionHistory() => _sessions.AsReadOnly();

    public IEnumerable<string> GetReservedRegNumbers() =>
        _garage?.GetGrid().Cast<GarageCell>()
            .OfType<ParkingSpot>()
            .Where(s => s.IsReserved && s.ReservedForRegNumber is not null)
            .Select(s => s.ReservedForRegNumber!)
            .Distinct()
        ?? Enumerable.Empty<string>();
}
