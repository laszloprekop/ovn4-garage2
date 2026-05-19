using Ovn4_GarageProject2.Domain;

namespace Ovn4_GarageProject2.Handler;

public interface IHandler
{
    IEnumerable<Vehicle> GetAllVehicles();
    GarageCell[,] GetGrid();
    IEnumerable<(string Type, int Count)> GetVehicleTypeCounts();

    /// <summary>
    /// Parks <paramref name="vehicle"/> in the first available spot.
    /// </summary>
    /// <returns>The anchor spot's ID on success, or <see langword="null"/> if parking failed.</returns>
    int? Park(Vehicle vehicle);
}
