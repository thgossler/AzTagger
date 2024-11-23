// Copyright (c) Thomas Gossler. All rights reserved.
// Licensed under the MIT license.

using System.Windows.Forms;

namespace AzTagger;
public class InputDialog : Form
{
    private TextBox _txtInput;
    private Button _btnOk;
    private Button _btnCancel;

    public string InputText => _txtInput.Text;

    public InputDialog(string prompt)
    {
        Text = "Save Query as...";
        Width = 400;
        Height = 150;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        StartPosition = FormStartPosition.CenterParent;
        MaximizeBox = false;
        MinimizeBox = false;
        ControlBox = false;

        var lblPrompt = new Label() { Left = 10, Top = 10, Width = 360, Text = prompt };
        _txtInput = new TextBox() { Left = 10, Top = 30, Width = 360 };
        _btnOk = new Button() { Text = "OK", Left = 210, Width = 75, Top = 60, DialogResult = DialogResult.OK };
        _btnCancel = new Button() { Text = "Cancel", Left = 295, Width = 75, Top = 60, DialogResult = DialogResult.Cancel };

        _btnOk.Click += (sender, e) => { Close(); };
        _btnCancel.Click += (sender, e) => { Close(); };

        Controls.Add(lblPrompt);
        Controls.Add(_txtInput);
        Controls.Add(_btnOk);
        Controls.Add(_btnCancel);

        AcceptButton = _btnOk;
        CancelButton = _btnCancel;
    }
}
