namespace Ovn4_GarageProject2.Layouts;
using Domain;

public static class MixedGarageLayout
{
    private static readonly string[] Blueprint =
    [
        "░░░░░░░░░░░░░░",
        "░│C│C│c│c│  b░",
        "░           b░",
        "░│C│c│c│c│  b░",
        "░├─┼─┼─┼─┤  ─░",
        "░│c│c│c│c│  b░",
        "░           b░",
        "░│C│C│c│P│  b░",
        "░░░░░░░░░░░░░░",
    ];

    public static Garage<Vehicle> Create() =>
        LayoutParser.Parse<Vehicle>("Mixed Garage", Blueprint);
}
