using Eto.Drawing;
using Eto.Forms;

namespace AzTagger.App;

public class InputDialog : Dialog<string>
{
    private readonly TextBox _textBox;

    public InputDialog(string title, string prompt)
    {
        Title = title;
        ClientSize = new Size(400, 120);
        Resizable = false;
        Padding = 10;

        var promptLabel = new Label { Text = prompt };
        _textBox = new TextBox { Width = 360 };

        var okButton = new Button { Text = "OK" };
        okButton.Click += (_, _) => Close(_textBox.Text);

        var cancelButton = new Button { Text = "Cancel" };
        cancelButton.Click += (_, _) => Close(null);

        DefaultButton = okButton;
        AbortButton = cancelButton;

        var layout = new DynamicLayout { DefaultSpacing = new Size(5, 5), Padding = new Padding(10) };
        layout.AddRow(promptLabel);
        layout.AddRow(_textBox);
        layout.AddRow(new StackLayout
        {
            Orientation = Orientation.Horizontal,
            Spacing = 5,
            Items = { null, okButton, cancelButton }
        });

        Content = layout;
    }
}
