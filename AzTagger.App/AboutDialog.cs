// Copyright (c) Thomas Gossler. All rights reserved.
// Licensed under the MIT license.

#nullable enable

using System;
using System.Diagnostics;
using System.IO;
using Eto.Drawing;
using Eto.Forms;

namespace AzTagger.App;

public class AboutDialog : Dialog
{
    public AboutDialog()
    {
        var version = GetType().Assembly.GetName().Version?.ToString() ?? "Unknown";
        
        Title = "About AzTagger";
        ClientSize = new Size(400, 180);
        Resizable = false;

        // Load the application icon
        ImageView? iconView = null;
        try
        {
            Image? iconImage = null;
            
            // Try multiple resource loading approaches
            string[] resourceNames = {
                "AzTagger.App.Resources.icon.png",
                "Resources.icon.png",
                "icon.png"
            };
            
            foreach (var resourceName in resourceNames)
            {
                try
                {
                    iconImage = Bitmap.FromResource(resourceName);
                    System.Diagnostics.Debug.WriteLine($"Successfully loaded icon from resource: {resourceName}");
                    break;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Failed to load from resource '{resourceName}': {ex.Message}");
                }
            }
            
            // If resource loading failed, try file system
            if (iconImage == null)
            {
                var iconPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "icon.png");
                if (System.IO.File.Exists(iconPath))
                {
                    iconImage = new Bitmap(iconPath);
                    System.Diagnostics.Debug.WriteLine($"Successfully loaded icon from file: {iconPath}");
                }
                else
                {
                    // Final fallback: try to find the icon in the images directory
                    var baseDir = AppDomain.CurrentDomain.BaseDirectory;
                    var fallbackPath = System.IO.Path.GetFullPath(System.IO.Path.Combine(baseDir, "..", "..", "..", "..", "images", "icon.png"));
                    if (System.IO.File.Exists(fallbackPath))
                    {
                        iconImage = new Bitmap(fallbackPath);
                        System.Diagnostics.Debug.WriteLine($"Successfully loaded icon from fallback: {fallbackPath}");
                    }
                }
            }
            
            if (iconImage != null)
            {
                // Scale the icon to a reasonable size for the dialog
                var scaledIcon = new Bitmap(iconImage, 48, 48);
                iconView = new ImageView 
                { 
                    Image = scaledIcon,
                    Size = new Size(48, 48)
                };
                System.Diagnostics.Debug.WriteLine("Created ImageView with scaled icon");
            }
        }
        catch (Exception ex)
        {
            // Log error but don't fail the dialog
            System.Diagnostics.Debug.WriteLine($"Failed to load icon for About dialog: {ex.Message}");
        }

        // Debug: Check if icon was loaded
        if (iconView != null)
        {
            System.Diagnostics.Debug.WriteLine("Icon successfully loaded for About dialog");
        }
        else
        {
            System.Diagnostics.Debug.WriteLine("Icon could not be loaded for About dialog");
        }

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

        // Create the text content
        var textContent = new StackLayout
        {
            Orientation = Orientation.Vertical,
            Spacing = 10,
            Items =
            {
                new Label { Text = "AzTagger", Font = new Font(FontFamilies.Sans, 18, FontStyle.Bold), TextAlignment = TextAlignment.Center },
                new Label { Text = $"Version {version}", TextAlignment = TextAlignment.Center },
                new Label { Text = "A tool for querying and managing Azure resources and tags.", TextAlignment = TextAlignment.Center, Wrap = WrapMode.Word },
                githubRow
            }
        };

        // Create the main content layout
        if (iconView != null)
        {
            // Layout with icon on the left and text on the right
            var mainContent = new StackLayout
            {
                Orientation = Orientation.Horizontal,
                Spacing = 15,
                Padding = 20,
                Items =
                {
                    new StackLayoutItem(iconView, VerticalAlignment.Top),
                    new StackLayoutItem(textContent, true)
                }
            };
            Content = mainContent;
        }
        else
        {
            // Fallback to text-only layout if icon couldn't be loaded
            textContent.Padding = 20;
            Content = textContent;
        }
        
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
