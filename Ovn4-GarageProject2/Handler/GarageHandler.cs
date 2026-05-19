using Ovn4_GarageProject2.Domain;

namespace Ovn4_GarageProject2.Handler;

public class GarageHandler : IHandler
{
    private IGarage? _garage;

    public void SetGarage(IGarage garage) => _garage = garage;

    public Domain.GarageCell[,] GetGrid() => _garage?.GetGrid() ?? new Domain.GarageCell[0, 0];

    public bool ParkAtSpot(int spotId, Vehicle vehicle)
    {
        if (_garage is null) return false;
        var spot = _garage.GetGrid().Cast<Domain.GarageCell>()
            .OfType<Domain.ParkingSpot>()
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

}
