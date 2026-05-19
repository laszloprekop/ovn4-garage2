namespace Ovn4_GarageProject2.UI;

using System.Drawing;
using Terminal.Gui.Drawing;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;

// Renders a string[] as stacked Labels so content is visible without focus.
// When a SpotHighlight is provided, the characters inside that rectangle are
// rendered in bright green by splitting the affected rows into three Labels.
public class SpriteView : View
{
    private static readonly Scheme GreenScheme =
        new(new Attribute(ColorName16.BrightBlue, ColorName16.Black));

    public SpriteView(string[] lines)
    {
        ViewportSettings = ViewportSettingsFlags.HasScrollBars;
        Rebuild(lines);
    }

    public void Rebuild(string[] lines, GarageRenderer.SpotHighlight? highlight = null)
    {
        RemoveAll();
        int maxWidth = lines.Length > 0 ? lines.Max(l => l.Length) : 0;

        for (int i = 0; i < lines.Length; i++)
        {
            bool isHighlightedRow = highlight is not null
                                    && i >= highlight.CharRow
                                    && i < highlight.CharRow + highlight.CharHeight;

            if (!isHighlightedRow)
            {
                Add(new Label { Text = lines[i], X = 0, Y = i, Width = maxWidth, Height = 1 });
                continue;
            }

            // Split the row into up to three segments: before | highlighted | after.
            int hStart = highlight!.CharCol;
            int hEnd = highlight.CharCol + highlight.CharWidth;
            string line = lines[i];

            if (hStart > 0)
                Add(new Label { Text = line[..hStart], X = 0, Y = i, Width = hStart, Height = 1 });

            if (hStart < line.Length)
            {
                int segEnd = Math.Min(hEnd, line.Length);
                var hl = new Label
                {
                    Text = line[hStart..segEnd],
                    X = hStart,
                    Y = i,
                    Width = hEnd - hStart,
                    Height = 1,
                };
                hl.SetScheme(GreenScheme);
                Add(hl);
            }

            if (hEnd < line.Length)
                Add(new Label { Text = line[hEnd..], X = hEnd, Y = i, Width = line.Length - hEnd, Height = 1 });
        }

        SetContentSize(new Size(maxWidth, lines.Length));
        SetNeedsDraw();
    }
}
