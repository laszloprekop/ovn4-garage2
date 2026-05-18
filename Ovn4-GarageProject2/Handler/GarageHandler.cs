using Ovn4_GarageProject2.Domain;

namespace Ovn4_GarageProject2.Handler;

public class GarageHandler : IHandler
{
    public IEnumerable<Vehicle> GetAllVehicles() => Enumerable.Empty<Vehicle>();
}
