namespace Ovn4_GarageProject2.Layouts;
using Domain;

public static class MixedGarageLayout
{
    private static readonly string[] Blueprint =
    [
        "░░░░░░░░░░░░░░░",
        "░░│C│C│p│p│  b░",
        "░            b░",
        "░ │C│c│c│p│  b░",
        "░ ├─┼─┼─┼─┤  ─░",
        "░ │C│c│c│P│  b░",
        "░            b░",
        "░░│C│C│p│P│  b░",
        "░░░░░░░░░░░  ░░",
    ];

    public static Garage<Vehicle> Create() =>
        LayoutParser.Parse<Vehicle>("Mixed Garage", Blueprint);
}
