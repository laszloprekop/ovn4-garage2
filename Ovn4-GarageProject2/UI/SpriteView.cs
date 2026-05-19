namespace Ovn4_GarageProject2.UI;
using System.Drawing;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;

// Renders a string[] as stacked Labels so content is visible without focus.
public class SpriteView : View
{
    public SpriteView(string[] lines)
    {
        ViewportSettings = ViewportSettingsFlags.HasScrollBars;
        Rebuild(lines);
    }

    public void Rebuild(string[] lines)
    {
        RemoveAll();
        int maxWidth = lines.Length > 0 ? lines.Max(l => l.Length) : 0;
        for (int i = 0; i < lines.Length; i++)
            Add(new Label { Text = lines[i], X = 0, Y = i, Width = maxWidth, Height = 1 });
        SetContentSize(new Size(maxWidth, lines.Length));
        SetNeedsDraw();
    }
}
