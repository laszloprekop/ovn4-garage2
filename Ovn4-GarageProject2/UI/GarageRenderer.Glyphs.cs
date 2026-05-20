namespace Ovn4_GarageProject2.UI;

using Domain;

/// <summary>
/// Static Unicode sprite tables for <see cref="GarageRenderer"/>.
/// </summary>
/// <remarks>
/// This file is the data half of <c>GarageRenderer</c>; the rendering pipeline lives in
/// <c>GarageRenderer.cs</c>. The <c>partial</c> keyword joins both files at compile time,
/// keeping logic and data separately navigable without introducing a second type.
/// <para>
/// Each field is a fixed-size rectangular array of strings representing one rendered
/// parking-spot state. All strings within a table have the same character count.
/// Rows are indexed top-to-bottom.
/// </para>
/// <para>
/// Naming convention:
/// <c>Vert</c> / <c>Horiz</c> (orientation) ·
/// <c>Empty</c> / <c>Resv</c> / <c>Parked</c> (occupancy) ·
/// <c>Up</c> / <c>Down</c> / <c>Left</c> / <c>Right</c> (entry facing) ·
/// <c>NoEv</c> / <c>Ev</c> (EV charger absent or present).
/// </para>
/// </remarks>
public static partial class GarageRenderer
{
    // ── Glyph tables — vertical 4×5 ──────────────────────────────────────
    // ↯ replaces ⚡ throughout — ↯ is 1 terminal column, ⚡ is 2 (breaks text alignment).

    static readonly string[] VertEmptyUpNoEv =
    [
        "⌜  ⌝",
        " ╌╌ ",
        " ╌╌ ",
        " ╌╌ ",
        "⌞  ⌟"
    ];

    static readonly string[] VertEmptyUpEv =
    [
        "⌜ ↯⌝",
        " ╌╌ ",
        " ╌╌ ",
        " ╌╌ ",
        "⌞  ⌟"
    ];

    static readonly string[] VertResvUpNoEv =
    [
        "⌜  ⌝",
        " ┌╮ ",
        " ├╯ ",
        " ╵  ",
        "⌞  ⌟"
    ];

    static readonly string[] VertResvUpEv =
    [
        "⌜ ↯⌝",
        " ┌╮ ",
        " ├╯ ",
        " ╵  ",
        "⌞  ⌟"
    ];

    static readonly string[] VertEmptyDownNoEv =
    [
        "⌜  ⌝",
        " ╌╌ ",
        " ╌╌ ",
        " ╌╌ ",
        "⌞  ⌟"
    ];

    static readonly string[] VertEmptyDownEv =
    [
        "⌜  ⌝",
        " ╌╌ ",
        " ╌╌ ",
        " ╌╌ ",
        "⌞↯ ⌟"
    ];

    static readonly string[] VertResvDownNoEv =
    [
        "⌜  ⌝",
        " ┌╮ ",
        " ├╯ ",
        " ╵  ",
        "⌞  ⌟"
    ];

    static readonly string[] VertResvDownEv =
    [
        "⌜  ⌝",
        " ┌╮ ",
        " ├╯ ",
        " ╵  ",
        "⌞↯ ⌟"
    ];

    // Vertical 4×5 — occupied (car outline)
    static readonly string[] VertParkedNoEv =
    [
        "╭──╮",
        "╿⌜⌝╿",
        "│██│",
        "╽  ╽",
        "╰╶╴╯"
    ];

    static readonly string[] VertParkedEv =
    [
        "╭──╮",
        "╿⌜⌝╿",
        "│██│",
        "╽↯ ╽",
        "╰╶╴╯"
    ];

    // ── Glyph tables — horizontal 9×3 ────────────────────────────────────

    static readonly string[] HorizEmptyRightNoEv =
    [
        "⌜ ╌╌    ⌝",
        "  ╌╌     ",
        "⌞ ╌╌    ⌟"
    ];

    static readonly string[] HorizEmptyRightEv =
    [
        "⌜ ╌╌    ⌝",
        "  ╌╌  ↯  ",
        "⌞ ╌╌    ⌟"
    ];

    static readonly string[] HorizResvRightNoEv =
    [
        "⌜ ┌╮    ⌝",
        "  ├╯     ",
        "⌞ ╵     ⌟"
    ];

    static readonly string[] HorizResvRightEv =
    [
        "⌜ ┌╮    ⌝",
        "  ├╯   ↯ ",
        "⌞ ╵     ⌟"
    ];

    static readonly string[] HorizEmptyLeftNoEv =
    [
        "⌜    ╌╌ ⌝",
        "     ╌╌  ",
        "⌞    ╌╌ ⌟"
    ];

    static readonly string[] HorizEmptyLeftEv =
    [
        "⌜    ╌╌ ⌝",
        " ↯   ╌╌  ",
        "⌞    ╌╌ ⌟"
    ];

    static readonly string[] HorizResvLeftNoEv =
    [
        "⌜    ┌╮ ⌝",
        "     ├╯  ",
        "⌞    ╵  ⌟"
    ];

    static readonly string[] HorizResvLeftEv =
    [
        "⌜    ┌╮ ⌝",
        " ↯   ├╯  ",
        "⌞    ╵  ⌟"
    ];

    // Horizontal 9×3 — occupied
    static readonly string[] HorizParkedNoEv =
    [
        "╭━─────━╮",
        "│[ ███  │",
        "╰━─────━╯"
    ];

    static readonly string[] HorizParkedEv =
    [
        "╭━─────━╮",
        "│[ ███ ↯│",
        "╰━─────━╯"
    ];

    // ── Bus — 9×15 (two spots tall + separator row) ───────────────────────
    // Not yet wired into GetGlyph — bus bays currently render as a single 'B'.
    // Zone-spanning render requires detecting the full bay extent at plan time.
    private static readonly string[] BusTwoByThreeEmpty =
    [
        "⌜       ⌝",
        "         ",
        "         ",
        "         ",
        "         ",
        " ┌╮╷╷╭╮  ",
        " │││││   ",
        " ├┤││╰╮  ",
        " ││││ │  ",
        " └╯╰╯╰╯  ",
        "         ",
        "         ",
        "         ",
        "         ",
        "⌞       ⌟"
    ];

    private static readonly string[] BusTwoByThree =
    [
        "╭╭━───━╮╮",
        "▌└─────┘▐",
        "│╮     ╭│",
        "│╯     ╰│",
        "│╮┌───┐╭│",
        "│╯│   │╰│",
        "│╮└───┘╭│",
        "│╯     ╰│",
        "│╮     ╭│",
        "│╯     ╰│",
        "│╮     ╭│",
        "│╯█████╰│",
        "▌╮█████╭▐",
        "▌╯█████╰▐",
        "╰━─────━╯",
    ];

    // 5-row slices used by GetBusGlyph so each logical slot renders one third of the sprite.
    private static readonly string[] BusTwoByThreeEmptyTop = BusTwoByThreeEmpty[..5];
    private static readonly string[] BusTwoByThreeEmptyMid = BusTwoByThreeEmpty[5..10];
    private static readonly string[] BusTwoByThreeEmptyBot = BusTwoByThreeEmpty[10..];
    private static readonly string[] BusTwoByThreeTop = BusTwoByThree[..5];
    private static readonly string[] BusTwoByThreeMid = BusTwoByThree[5..10];
    private static readonly string[] BusTwoByThreeBot = BusTwoByThree[10..];
}
