using System.Collections.ObjectModel;
using Ovn4_GarageProject2.Handler;
using Terminal.Gui.App;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;

namespace Ovn4_GarageProject2.UI;

public class ConsoleUi : IUi
{
    private readonly IHandler _handler;
    private readonly Manager _manager;

    public ConsoleUi(IHandler handler, Manager manager)
    {
        _handler = handler;
        _manager = manager;
    }

    public void Start()
    {
        // Request terminal resize before TG2 init so it sees the new size.
        // Works in Terminal.app / iTerm2; ignored in JetBrains embedded panel.
        Console.Write("\x1b[8;48;160t");
        Thread.Sleep(50);

        using IApplication app = Application.Create().Init();

        string BuildStats() =>
            _handler.GetVehicleTypeCounts().Any()
                ? "  " + string.Join("   ", _handler.GetVehicleTypeCounts()
                    .OrderBy(t => t.Type)
                    .Select(t => $"{t.Type}: {t.Count}"))
                : "  (garage empty)";

        var statsLine = new Label
        {
            X = 0, Y = 1,
            Width = Dim.Fill(),
            Height = 1,
            Text = BuildStats(),
        };

        var mapView = new GarageMapView(_handler.GetGrid())
        {
            X = 0, Y = 2,
            Width = Dim.Fill(),
            Height = Dim.Fill(),
            Visible = false,
        };

        var spriteView = new SpriteView(GarageRenderer.Render(_handler.GetGrid()))
        {
            X = 0, Y = 2,
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
                    new MenuItem("_Switch Garage", "", () =>
                    {
                        var names = _manager.Garages.Select((g, i) => $"{i + 1}. {g.Name} ({g.Capacity} spots")
                            .ToList();
                        var dialog = new Dialog { Title = "Select Garage", Width = 40, Height = 12 };
                        var list = new ListView { Width = Dim.Fill(), Height = Dim.Fill() - 2 };
                        list.SetSource(new ObservableCollection<string>(names));
                        var okButton = new Button { Text = "OK", X = Pos.Center(), Y = Pos.Bottom(list) };
                        okButton.Accepting += (_, _) =>
                        {
                            _manager.SwitchGarage(list.SelectedItem);
                            app.RequestStop(null);
                        };
                        dialog.Add(list, okButton);
                        app.Run(dialog);

                        // update the view to the new garage
                        spriteView.Rebuild(GarageRenderer.Render(_handler.GetGrid()));
                        mapView.Rebuild(_handler.GetGrid());
                        statsLine.Text = BuildStats();
                    }),
                    new MenuItem("_List Vehicles", "", () =>
                    {
                        var items = _handler.GetAllVehicles()
                            .Select(v =>
                                $"{v.RegNumber,-10} {v.GetType().Name,-12} {v.Colour,-10} {v.WheelCount} wheel(s)")
                            .ToList();

                        var dialog = new Dialog { Title = "Parked Vehicles", Width = 60, Height = 20 };
                        var list = new ListView { Width = Dim.Fill(), Height = Dim.Fill() - 2 };
                        list.SetSource(new ObservableCollection<string>(items));
                        var close = new Button { Text = "Close", X = Pos.Center(), Y = Pos.Bottom(list) };

                        close.Accepting += (_, _) => app.RequestStop(null);
                        dialog.Add(list, close);
                        app.Run(dialog);
                    }, null),
                    new MenuItem("Vehicle _Type Summary", "", () =>
                    {
                        var items = _handler.GetVehicleTypeCounts()
                            .Select(t => $"{t.Type,-15}: {t.Count}").ToList();
                        var dialog = new Dialog { Title = "Vehicle Type Summary", Width = 40, Height = 15 };
                        var list = new ListView { Width = Dim.Fill(), Height = Dim.Fill() - 2 };
                        list.SetSource(new ObservableCollection<string>(items));
                        var close = new Button { Text = "Close", X = Pos.Center(), Y = Pos.Bottom(list) };
                        close.Accepting += (_, _) => app.RequestStop(null);
                        dialog.Add(list, close);
                        app.Run(dialog);
                    }, null),
                    new MenuItem("_Park Vehicle", "", () =>
                    {
                        var types = new[] { "Car", "Motorcycle", "Bus", "Boat", "Airplane" };
                        var dialog = new Dialog { Title = "Park Vehicle", Width = 50, Height = 14 };

                        var typeLabel = new Label { Text = "Vehicle Type:", X = 1, Y = 1 };
                        var typeList = new ListView { X = 1, Y = 2, Width = 20, Height = 5 };
                        typeList.SetSource(new ObservableCollection<string>(types));
                        var regLabel = new Label { Text = "Registration number:", X = 1, Y = 8 };
                        var regField = new TextField { X = 14, Y = 8, Width = 15 };
                        var okButton = new Button { Text = "Park", X = 1, Y = 10 };
                        var cancelButton = new Button { Text = "Cancel", X = 10, Y = 10 };

                        int? parkedSpotId = null;

                        void DoPark()
                        {
                            if (string.IsNullOrWhiteSpace(regField.Text))
                            {
                                MessageBox.ErrorQuery(app, "Validation", "Registration number cannot be empty.", "OK");
                                return;
                            }

                            var vehicle = CreateVehicle(types[typeList.SelectedItem ?? 0], regField.Text.Trim());
                            parkedSpotId = _handler.Park(vehicle);
                            app.RequestStop(null);
                            MessageBox.Query(app, "Result",
                                parkedSpotId.HasValue ? "Vehicle parked." : "Could not park vehicle.", "OK");
                        }

                        okButton.Accepting += (_, _) => DoPark();
                        regField.OnEnter(DoPark);
                        cancelButton.Accepting += (_, _) => app.RequestStop(null);

                        dialog.Add(typeLabel, typeList, regLabel, regField, okButton, cancelButton);
                        app.Run(dialog);

                        if (parkedSpotId.HasValue)
                        {
                            var (lines, highlight) = GarageRenderer.RenderWithHighlight(
                                _handler.GetGrid(), parkedSpotId.Value);
                            spriteView.Rebuild(lines, highlight);
                            mapView.Rebuild(_handler.GetGrid());
                            statsLine.Text = BuildStats();
                        }
                    }, null),
                    new MenuItem("_Remove Vehicle", "", () =>
                    {
                        var dialog = new Dialog { Title = "Remove Vehicle", Width = 44, Height = 10 };
                        var regLabel = new Label { Text = "Registration number:", X = 1, Y = 1 };
                        var regField = new TextField { X = 1, Y = 3, Width = 30 };
                        var okButton = new Button { Text = "Remove", X = 1, Y = 6 };
                        var cancelButton = new Button { Text = "Cancel", X = 11, Y = 6 };

                        bool removed = false;

                        void DoRemove()
                        {
                            if (string.IsNullOrWhiteSpace(regField.Text))
                            {
                                MessageBox.ErrorQuery(app, "Validation", "Registration number cannot be empty.", "OK");
                                return;
                            }

                            removed = _handler.Remove(regField.Text.Trim());
                            app.RequestStop(null);
                            MessageBox.Query(app, "Result",
                                removed ? "Vehicle removed." : "No vehicle with that registration found.", "OK");
                        }

                        okButton.Accepting += (_, _) => DoRemove();
                        regField.OnEnter(DoRemove);
                        cancelButton.Accepting += (_, _) => app.RequestStop(null);

                        dialog.Add(regLabel, regField, okButton, cancelButton);
                        app.Run(dialog);

                        if (removed)
                        {
                            spriteView.Rebuild(GarageRenderer.Render(_handler.GetGrid()));
                            mapView.Rebuild(_handler.GetGrid());
                            statsLine.Text = BuildStats();
                        }
                    }, null),
                    new MenuItem("_Find by RegistrationNumber", "", () =>
                    {
                        var dialog = new Dialog { Title = "Find Vehicle", Width = 44, Height = 10 };
                        var label = new Label { Text = "Registration number:", X = 1, Y = 1 };
                        var regField = new TextField { X = 1, Y = 3, Width = 15 };
                        var okButton = new Button { Text = "Find", X = 1, Y = 6 };
                        var cancelButton = new Button { Text = "Cancel", X = 11, Y = 6 };

                        void DoFindRegNo()
                        {
                            if (string.IsNullOrWhiteSpace(regField.Text))
                            {
                                MessageBox.ErrorQuery(app, "Validation", "Registration number cannot be empty.", "OK");
                                return;
                            }

                            var results = _handler.FindByReg(regField.Text.Trim())
                                .Select(v =>
                                    $"{v.RegNumber,-10}  {v.GetType().Name,-12}  {v.Colour,-10} {v.WheelCount} wheel(s)")
                                .ToList();
                            app.RequestStop(null);

                            var resultDialog = new Dialog
                                { Title = $"Results ({results.Count})", Width = 60, Height = 20 };
                            var list = new ListView { Width = Dim.Fill(), Height = Dim.Fill() - 2 };
                            list.SetSource(
                                new ObservableCollection<string>(results.Count > 0 ? results : ["No results found."]));
                            var closeButton = new Button { Text = "Close", X = Pos.Center(), Y = Pos.Bottom(list) };
                            closeButton.Accepting += (_, _) => app.RequestStop(null);
                            resultDialog.Add(list, closeButton);
                            app.Run(resultDialog);
                        }

                        okButton.Accepting += (_, _) => DoFindRegNo();
                        regField.OnEnter(DoFindRegNo);
                        cancelButton.Accepting += (_, _) => app.RequestStop(null);
                        dialog.Add(label, regField, okButton, cancelButton);
                        app.Run(dialog);
                    }),
                    new MenuItem("_Search", "", () =>
                    {
                        var dialog = new Dialog { Title = "Search Vehicles", Width = 50, Height = 12 };
                        var colorLabel = new Label { Text = "Colour:", X = 1, Y = 1 };
                        var colorField = new TextField { X = 14, Y = 1, Width = 15 };
                        var wheelsLabel = new Label { Text = "Wheels:", X = 1, Y = 3 };
                        var wheelsField = new TextField { X = 14, Y = 3, Width = 15 };
                        var typeLabel = new Label { Text = "Vehicle Type:", X = 1, Y = 5 };
                        var typeField = new TextField { X = 14, Y = 5, Width = 15 };
                        var okButton = new Button { Text = "Search", X = 1, Y = 7 };
                        var cancelButton = new Button { Text = "Cancel", X = 10, Y = 7 };

                        void DoSearch()
                        {
                            string? wheels = int.TryParse(wheelsField.Text, out int w) ? w.ToString() : null;
                            Type? type = ResolveType(typeField.Text);
                            var results = _handler.Search(colorField.Text?.Trim(), wheels, type)
                                .Select(v => $"{v.RegNumber,-10} {v.GetType().Name,-12} {v.Colour}").ToList();
                            app.RequestStop(null);
                            ShowList(app, "Search Results", results.Count > 0 ? results : ["No results found."]);
                        }

                        okButton.Accepting += (_, _) => DoSearch();
                        colorField.OnEnter(DoSearch);
                        wheelsField.OnEnter(DoSearch);
                        typeField.OnEnter(DoSearch);

                        cancelButton.Accepting += (_, _) => app.RequestStop(null);
                        dialog.Add(colorLabel, colorField, wheelsLabel, wheelsField, typeLabel, typeField, okButton,
                            cancelButton);
                        app.Run(dialog);
                    }),
                    new MenuItem("Parking _History", "", () =>
                    {
                        var items = _handler.GetSessionHistory()
                            .OrderByDescending(session => session.Start)
                            .Select(session => session.End.HasValue
                                ? $"{session.RegNumber,-10} @ {session.SpotId,-4} in {session.Start:MM-dd HH:mm:ss} out {session.End.Value:MM-dd HH:mm:ss}"
                                : $"{session.RegNumber,-10} @ {session.SpotId,-4} in {session.Start:MM-dd HH:mm:ss} (parking)")
                            .ToList();
                        ShowList(app, "Parking History",
                            items.Count > 0 ? items : ["No parking sessions recorded yet."]);
                    }, null),
                    new MenuItem("_Toggle Renderer", "", () =>
                    {
                        showingSprite = !showingSprite;
                        spriteView.Visible = showingSprite;
                        mapView.Visible = !showingSprite;
                    }, null),
                    new MenuItem("_Quit", "", () => app.RequestStop(null), null)
                ])
            ]
        };

        var win = new Window { Title = "Garage 2.0" };
        win.Add(menu, statsLine, mapView, spriteView);
        app.Run(win);
        win.Dispose();
    }

    private static Domain.Vehicle CreateVehicle(string type, string regNo) => type switch
    {
        "Car" => new Domain.Car { RegNumber = regNo, Colour = "White", WheelCount = "4" },
        "Motorcycle" => new Domain.Motorcycle { RegNumber = regNo, Colour = "Black", WheelCount = "2" },
        "Bus" => new Domain.Bus { RegNumber = regNo, Colour = "Yellow", WheelCount = "6" },
        "Boat" => new Domain.Boat { RegNumber = regNo, Colour = "Blue", WheelCount = "10" },
        "Airplane" => new Domain.Airplane { RegNumber = regNo, Colour = "Silver", WheelCount = "18" },
        _ => throw new NotImplementedException()
    };

    private static void ShowList(IApplication app, string title, List<string> items)
    {
        var d = new Dialog { Title = title, Width = 60, Height = 20 };
        var lv = new ListView { Width = Dim.Fill(), Height = Dim.Fill() - 2 };
        lv.SetSource(new System.Collections.ObjectModel.ObservableCollection<string>(items));
        var btn = new Button { Text = "Close", X = Pos.Center(), Y = Pos.Bottom(lv) };
        btn.Accepting += (_, _) => app.RequestStop(null);
        d.Add(lv, btn);
        app.Run(d);
    }

    private static Type? ResolveType(string name) => name.Trim().ToLower() switch
    {
        "car" => typeof(Domain.Car),
        "motorcycle" => typeof(Domain.Motorcycle),
        "bus" => typeof(Domain.Bus),
        "boat" => typeof(Domain.Boat),
        "airplane" => typeof(Domain.Airplane),
        _ => null,
    };
}
