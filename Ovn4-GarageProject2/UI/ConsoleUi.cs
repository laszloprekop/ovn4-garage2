using Ovn4_GarageProject2.Handler;
using Terminal.Gui.App;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;

namespace Ovn4_GarageProject2.UI;

public class ConsoleUi : IUi
{
    private readonly IHandler _handler;
    public ConsoleUi(IHandler handler) => _handler = handler;

    public void Start()
    {
        using IApplication app = Application.Create().Init();
        var menu = new MenuBar
        {
            Menus =
            [
                new MenuBarItem("_Garage",
                [
                    new MenuItem("_List Vehicles", "",
                        () => MessageBox.Query(app, "Vehicles",
                            $"Parked: {_handler.GetAllVehicles().Count()}", "OK")),
                    new MenuItem("_Quit", "", () => app.RequestStop(null))
                ])
            ]
        };

        var mapView = new GarageMapView(_handler.GetGrid())
        {
            X = 0, Y = 1,
            Width = Dim.Fill(),
            Height = Dim.Fill(),
        };
        var win = new Window { Title = "Garage 2.0" };
        win.Add(menu, mapView);
        app.Run(win);
        win.Dispose();
    }
}
