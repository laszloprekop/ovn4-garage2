using System.Collections;
using Ovn4_GarageProject2.Layouts;

namespace Ovn4_GarageProject2.Domain;

public class Garage<T> : IGarage, IEnumerable<T> where T : Vehicle
{
    private readonly GarageCell[,] _grid;

    public string Name { get; }
    public int Capacity { get; }

    public Garage(string name, GarageCell[,] grid, GarageLayout layout)
    {
        Name = name;
        _grid = grid;
        Capacity = AllSpots().Count();
        Layout = layout;
    }

    public GarageLayout Layout { get; }

    public IEnumerable<Vehicle> GetAll() =>
        AllSpots().SelectMany(s => s.GetVehicles()).DistinctBy(v => v.RegNumber);

    public GarageCell[,] GetGrid() => _grid;

    private IEnumerable<ParkingSpot> AllSpots() => _grid.Cast<GarageCell>().OfType<ParkingSpot>();

    public IEnumerator<T> GetEnumerator() =>
        AllSpots().SelectMany(s => s.GetVehicles()).OfType<T>().GetEnumerator();


    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
