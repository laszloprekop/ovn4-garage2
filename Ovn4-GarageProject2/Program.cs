using Ovn4_GarageProject2;
using Ovn4_GarageProject2.Handler;
using Ovn4_GarageProject2.UI;

var handler = new GarageHandler();
var ui = new ConsoleUi(handler);
var manager = new Manager(ui, handler);
handler.SetGarage(manager.ActiveGarage);
// seed garage with some vehicles
// IDs are r*16+c for the 16-column blueprint.
handler.ParkAtSpot(21, new Ovn4_GarageProject2.Domain.Car
    { RegNumber = "DEF345", Colour = "Blue", WheelCount = "4", FuelType = "EV" });
handler.ParkAtSpot(23, new Ovn4_GarageProject2.Domain.Motorcycle
    { RegNumber = "DEF456", Colour = "Black", WheelCount = "2", CylinderVolume = "650" });
handler.ParkAtSpot(89, new Ovn4_GarageProject2.Domain.Car
    { RegNumber = "ABC123", Colour = "Red", WheelCount = "4", FuelType = "Gasoline" });
handler.ParkAtSpot(29, new Ovn4_GarageProject2.Domain.Bus()
    { RegNumber = "BUS001", Colour = "Yellow", WheelCount = "6", NumberOfSeats = "12"});
manager.Run();
