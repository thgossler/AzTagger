#!/bin/bash
# AzTagger GTK Launch Script for macOS
# This script sets the required environment variables for GTK+ on macOS

# Set library paths for GTK+
export DYLD_LIBRARY_PATH="/opt/homebrew/lib:/opt/homebrew/Cellar/gtk+3/3.24.43/lib:$DYLD_LIBRARY_PATH"

# Additional GTK+ environment variables for better compatibility
export GTK_PATH="/opt/homebrew/lib/gtk-3.0"
export GDK_PIXBUF_MODULE_FILE="/opt/homebrew/lib/gdk-pixbuf-2.0/2.10.0/loaders.cache"

# Use native macOS backend for GTK (no X11 required)
export GDK_BACKEND=quartz

# Run the GTK application
cd "$(dirname "$0")"
./AzTagger.Gtk/bin/Debug/net10.0/AzTagger.Gtk "$@"
