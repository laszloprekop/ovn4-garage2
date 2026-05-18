using Ovn4_GarageProject2.UI;

namespace Ovn4_GarageProject2;

public class Manager
{
    private readonly IUi _ui;
    public Manager(IUi ui) => _ui = ui;
    public void Run() => _ui.Start();
}
