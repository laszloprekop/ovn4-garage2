using Ovn4_GarageProject2.Domain;

namespace Ovn4_GarageProject2.Handler;

public interface IHandler
{
    IEnumerable<Vehicle> GetAllVehicles();
}
