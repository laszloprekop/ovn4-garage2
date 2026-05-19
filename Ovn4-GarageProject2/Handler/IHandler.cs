using Ovn4_GarageProject2.Domain;

namespace Ovn4_GarageProject2.Handler;

public interface IHandler
{
    IEnumerable<Vehicle> GetAllVehicles();
    GarageCell[,] GetGrid();
    IEnumerable<(string Type, int Count)> GetVehicleTypeCounts();
}
