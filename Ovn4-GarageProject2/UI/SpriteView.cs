namespace Ovn4_GarageProject2.UI;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;

// Renders a string[] as stacked Labels so content is visible without focus.
public class SpriteView : View
{
    public SpriteView(string[] lines) => Rebuild(lines);

    public void Rebuild(string[] lines)
    {
        RemoveAll();
        for (int i = 0; i < lines.Length; i++)
            Add(new Label { Text = lines[i], X = 0, Y = i, Width = Dim.Fill(), Height = 1 });
        SetNeedsDraw();
    }
}
