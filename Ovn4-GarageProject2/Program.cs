using Ovn4_GarageProject2;
using Ovn4_GarageProject2.Handler;
using Ovn4_GarageProject2.UI;

var handler = new GarageHandler();
var ui = new ConsoleUi(handler);
var manager = new Manager(ui, handler);
handler.SetGarage(manager.ActiveGarage);
// seed garage with some vehicles
handler.ParkAtSpot(20, new Ovn4_GarageProject2.Domain.Car
    { RegNumber = "DEF345", Colour = "Blue", WheelCount = "4", FuelType = "EV" });
handler.ParkAtSpot(22, new Ovn4_GarageProject2.Domain.Motorcycle
    { RegNumber = "DEF456", Colour = "Black", WheelCount = "2", CylinderVolume = "650" });
handler.ParkAtSpot(84, new Ovn4_GarageProject2.Domain.Car
    { RegNumber = "ABC123", Colour = "Red", WheelCount = "4", FuelType = "Gasoline" });
handler.ParkAtSpot(28, new Ovn4_GarageProject2.Domain.Bus()
    { RegNumber = "ABC123", Colour = "Red", WheelCount = "4", NumberOfSeats = "12"});
manager.Run();
