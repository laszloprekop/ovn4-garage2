namespace Ovn4_GarageProject2.Domain;

public abstract class GarageCell {}
public class RoadCell : GarageCell { public char Glyph { get; init; } = ' '; }
public class WallCell: GarageCell{}
// public class ColumnCell: GarageCell{}  // support column location, not used in this iteration
