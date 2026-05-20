using Ovn4_GarageProject2.Domain;
using Ovn4_GarageProject2.Handler;
using Ovn4_GarageProject2.Layouts;
using Ovn4_GarageProject2.UI;

namespace Ovn4_GarageProject2;

public class Manager(IUi ui, IHandler handler)
{
    private readonly IHandler _handler = handler;

    private readonly List<IGarage> _garages = [MixedGarageLayout.Create(), HangarLayout.Create()];
    private readonly int _activeGarageIndex = 0;


    public void Seed()
    {
        // placeholder for seeding code
    }

    public IGarage ActiveGarage => _garages[_activeGarageIndex];

    public void Run() => ui.Start();
}
