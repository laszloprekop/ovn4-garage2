namespace Ovn4_GarageProject2.Domain;

public class ParkingSpot : GarageCell
{
    public int Id { get; init; }
    public int Row { get; init; }
    public int Col { get; init; }
    public bool HasEvCharger { get; init; }
    public string? ReservedForRegNumber { get; set; }
    public Type? AllowedVehicleType { get; init;}
    public Orientation Orientation { get; init; } = Orientation.Vertical;
    public int ZoneId { get; set; } // 0 = not in zone, parsed from layout
    public ParkingSession? ActiveSession { get; set; }

    private readonly Vehicle?[] _subslots = new Vehicle?[3];

    public bool IsEmpty => _subslots.All(s => s is null);
    public bool IsMotorcycleMode => _subslots[0] is Motorcycle;
    public bool CanMerge => IsEmpty && !IsMotorcycleMode;
    public bool IsReserved { get; set; }

    public bool TryPark(Vehicle vehicle)
    {
        if (vehicle is Motorcycle moto)
        {
            int free = Array.IndexOf(_subslots, null);
            if (free < 0) return false;
            _subslots[free] = moto;
            return true;
        }
        if (!IsEmpty) return false;
        _subslots[0] = vehicle;
        return true;
    }

    public bool TryUnpark(string regNumber)
    {
        for (int i = 0; i < _subslots.Length; i++)
        {
            if (_subslots[i]?.RegNumber.Equals(regNumber, StringComparison.OrdinalIgnoreCase) == true)
            {
                _subslots[i] = null;
                return true;
            }
        }
        return false;
    }
    public IEnumerable<Vehicle> GetVehicles() => _subslots.OfType<Vehicle>();
}
