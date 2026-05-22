using Ovn4_GarageProject2.Domain;

namespace Ovn4_GarageProject2.Layouts;

public record GarageLayout(
    GarageCell[,] LogicalGrid,
    char[] ToWall,
    char[] BottomWall,
    char[] LeftWall,
    char[] RightWall,
    BayAnchor[] BayAnchors
);
