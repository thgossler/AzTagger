// Copyright (c) Thomas Gossler. All rights reserved.
// Licensed under the MIT license.

using System.Windows.Forms;

namespace AzTagger;
public class InputDialog : Form
{
    private TextBox _txtInput;
    private Label _lblPrompt;
    private Button _btnOk;
    private Button _btnCancel;

    private void InitializeComponent()
    {
        _lblPrompt = new Label();
        _txtInput = new TextBox();
        _btnOk = new Button();
        _btnCancel = new Button();
        SuspendLayout();
        // 
        // _lblPrompt
        // 
        _lblPrompt.Location = new System.Drawing.Point(27, 26);
        _lblPrompt.Name = "_lblPrompt";
        _lblPrompt.Size = new System.Drawing.Size(518, 30);
        _lblPrompt.TabIndex = 0;
        _lblPrompt.Text = "<Input prompt:>";
        // 
        // _txtInput
        // 
        _txtInput.Location = new System.Drawing.Point(27, 61);
        _txtInput.Name = "_txtInput";
        _txtInput.Size = new System.Drawing.Size(534, 31);
        _txtInput.TabIndex = 1;
        // 
        // _btnOk
        // 
        _btnOk.Location = new System.Drawing.Point(330, 117);
        _btnOk.Name = "_btnOk";
        _btnOk.Size = new System.Drawing.Size(105, 42);
        _btnOk.TabIndex = 2;
        _btnOk.Text = "Ok";
        // 
        // _btnCancel
        // 
        _btnCancel.Location = new System.Drawing.Point(441, 117);
        _btnCancel.Name = "_btnCancel";
        _btnCancel.Size = new System.Drawing.Size(120, 42);
        _btnCancel.TabIndex = 3;
        _btnCancel.Text = "Cancel";
        // 
        // InputDialog
        // 
        AcceptButton = _btnOk;
        CancelButton = _btnCancel;
        ClientSize = new System.Drawing.Size(584, 181);
        ControlBox = false;
        Controls.Add(_lblPrompt);
        Controls.Add(_txtInput);
        Controls.Add(_btnOk);
        Controls.Add(_btnCancel);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        Name = "InputDialog";
        StartPosition = FormStartPosition.CenterParent;
        Text = "<Dialog title>";
        ResumeLayout(false);
        PerformLayout();
    }

    public string InputText => _txtInput.Text;

    public InputDialog(Form owner, string title, string prompt)
    {
        Owner = owner;

        InitializeComponent();

        Text = title;
        _lblPrompt.Text = prompt;

        _btnOk.Click += (sender, e) => { Close(); };
        _btnCancel.Click += (sender, e) => { Close(); };
    }
}
