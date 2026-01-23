#!/bin/bash
# AzTagger GTK Launch Script for macOS
# This script sets the required environment variables for GTK+ on macOS
#
# Prerequisites: brew install gtk+3

set -e

# Detect Homebrew prefix (works for both Intel and Apple Silicon Macs)
if command -v brew &> /dev/null; then
    BREW_PREFIX=$(brew --prefix)
else
    echo "Error: Homebrew is not installed. Please install it from https://brew.sh"
    exit 1
fi

# Check if GTK+3 is installed
if ! brew list gtk+3 &> /dev/null; then
    echo "Error: GTK+3 is not installed. Please run: brew install gtk+3"
    exit 1
fi

# Get GTK+3 library path dynamically
GTK_LIB_PATH=$(brew --prefix gtk+3)/lib

# Set library paths for GTK+
export DYLD_LIBRARY_PATH="$BREW_PREFIX/lib:$GTK_LIB_PATH:$DYLD_LIBRARY_PATH"

# Additional GTK+ environment variables for better compatibility
export GTK_PATH="$BREW_PREFIX/lib/gtk-3.0"
export GDK_PIXBUF_MODULE_FILE="$BREW_PREFIX/lib/gdk-pixbuf-2.0/2.10.0/loaders.cache"

# Use native macOS backend for GTK (no X11 required)
export GDK_BACKEND=quartz

# Run the GTK application
SCRIPT_DIR="$(dirname "$0")"
DEBUG_EXE="$SCRIPT_DIR/bin/Debug/net10.0/AzTagger"
RELEASE_EXE="$SCRIPT_DIR/bin/Release/net10.0/AzTagger"

# Determine which executable to use (prefer newer one)
if [[ -f "$DEBUG_EXE" && -f "$RELEASE_EXE" ]]; then
    if [[ "$DEBUG_EXE" -nt "$RELEASE_EXE" ]]; then
        EXE="$DEBUG_EXE"
    else
        EXE="$RELEASE_EXE"
    fi
elif [[ -f "$RELEASE_EXE" ]]; then
    EXE="$RELEASE_EXE"
elif [[ -f "$DEBUG_EXE" ]]; then
    EXE="$DEBUG_EXE"
else
    echo "Error: No executable found. Please build the project first."
    echo "  Run: dotnet build AzTagger.Gtk/AzTagger.Gtk.csproj"
    exit 1
fi

"$EXE" "$@"
