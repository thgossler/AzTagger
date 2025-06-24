// Copyright (c) Thomas Gossler. All rights reserved.
// Licensed under the MIT license.

#nullable enable

using System;
using System.Diagnostics;
using Eto.Drawing;
using Eto.Forms;

namespace AzTagger.App;

public class AboutDialog : Dialog
{
    public AboutDialog()
    {
        var version = GetType().Assembly.GetName().Version?.ToString() ?? "Unknown";
        
        Title = "About AzTagger";
        ClientSize = new Size(350, 180);
        Resizable = false;

        // Create GitHub link button
        var githubLink = new LinkButton { Text = "thgossler/AzTagger" };
        githubLink.Click += (_, _) => Process.Start(new ProcessStartInfo
        {
            FileName = "https://github.com/thgossler/AzTagger",
            UseShellExecute = true
        });

        // Create GitHub row with label and link
        var githubRow = new StackLayout
        {
            Orientation = Orientation.Horizontal,
            Spacing = 5,
            HorizontalContentAlignment = HorizontalAlignment.Center,
            Items =
            {
                new Label { Text = "GitHub:", TextAlignment = TextAlignment.Center },
                githubLink
            }
        };

        var content = new StackLayout
        {
            Orientation = Orientation.Vertical,
            Spacing = 10,
            Padding = 20,
            Items =
            {
                new Label { Text = "AzTagger", Font = new Font(FontFamilies.Sans, 18, FontStyle.Bold), TextAlignment = TextAlignment.Center },
                new Label { Text = $"Version {version}", TextAlignment = TextAlignment.Center },
                new Label { Text = "A tool for querying and managing Azure resources and tags.", TextAlignment = TextAlignment.Center, Wrap = WrapMode.Word },
                githubRow
            }
        };

        Content = content;
        
        // Handle ESC key to close the dialog
        KeyDown += (_, e) =>
        {
            if (e.Key == Keys.Escape)
            {
                Close();
                e.Handled = true;
            }
        };
    }
}
