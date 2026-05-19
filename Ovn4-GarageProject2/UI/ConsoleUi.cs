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

        var mapView = new GarageMapView(_handler.GetGrid())
        {
            X = 0, Y = 1,
            Width = Dim.Fill(),
            Height = Dim.Fill(),
            Visible = false,
        };

        var spriteView = new SpriteView(GarageRenderer.Render(_handler.GetGrid()))
        {
            X = 0, Y = 1,
            Width = Dim.Fill(),
            Height = Dim.Fill(),
        };

        bool showingSprite = true;

        var menu = new MenuBar
        {
            Menus =
            [
                new MenuBarItem("_Garage",
                [
                    new MenuItem("_List Vehicles", "",
                        () => MessageBox.Query(app, "Vehicles",
                            $"Parked: {_handler.GetAllVehicles().Count()}", "OK")),
                    new MenuItem("_Toggle Renderer", "",
                        () =>
                        {
                            showingSprite = !showingSprite;
                            spriteView.Visible = showingSprite;
                            mapView.Visible    = !showingSprite;
                        }),
                    new MenuItem("_Quit", "", () => app.RequestStop(null))
                ])
            ]
        };

        var win = new Window { Title = "Garage 2.0" };
        win.Add(menu, mapView, spriteView);
        app.Run(win);
        win.Dispose();
    }
}
