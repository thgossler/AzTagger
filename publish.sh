#!/bin/bash
set -e

# Get version from AzTagger.App.csproj
FULL_VERSION=$(grep -o '<Version>[^<]*</Version>' AzTagger.App/AzTagger.App.csproj | sed 's/<[^>]*>//g')
# Extract major.minor.patch (remove the last .0)
VERSION=$(echo "$FULL_VERSION" | cut -d'.' -f1-3)

echo "Building and packaging AzTagger v$VERSION for all platforms..."

# Clean publish directory
rm -rf publish/win-x64 publish/win-arm64 publish/mac-x64 publish/mac-arm64 publish/linux-x64 publish/linux-arm64

# Build for all platforms
echo ""
echo "Building for win-x64..."
dotnet publish AzTagger.Wpf/AzTagger.Wpf.csproj -c Release -f net10.0-windows -r win-x64 --self-contained true -p:PublishSingleFile=true -p:PublishTrimmed=false -o ./publish/win-x64

echo ""
echo "Building for win-arm64..."
dotnet publish AzTagger.Wpf/AzTagger.Wpf.csproj -c Release -f net10.0-windows -r win-arm64 --self-contained true -p:PublishSingleFile=true -p:PublishTrimmed=false -o ./publish/win-arm64

echo ""
echo "Building for mac-x64..."
dotnet publish AzTagger.Mac/AzTagger.Mac.csproj -c Release -f net10.0 -r osx-x64 --self-contained true -p:PublishSingleFile=true -p:PublishTrimmed=false -o ./publish/mac-x64

echo ""
echo "Building for mac-arm64..."
dotnet publish AzTagger.Mac/AzTagger.Mac.csproj -c Release -f net10.0 -r osx-arm64 --self-contained true -p:PublishSingleFile=true -p:PublishTrimmed=false -o ./publish/mac-arm64

echo ""
echo "Building for linux-x64..."
dotnet publish AzTagger.Gtk/AzTagger.Gtk.csproj -c Release -f net10.0 -r linux-x64 --self-contained true -p:PublishSingleFile=true -p:PublishTrimmed=false -o ./publish/linux-x64

echo ""
echo "Building for linux-arm64..."
dotnet publish AzTagger.Gtk/AzTagger.Gtk.csproj -c Release -f net10.0 -r linux-arm64 --self-contained true -p:PublishSingleFile=true -p:PublishTrimmed=false -o ./publish/linux-arm64

# Create ZIP files
echo ""
echo "Creating ZIP files..."

# Remove old ZIP files if they exist
rm -f "publish/AzTagger-v$VERSION-win-x64.zip"
rm -f "publish/AzTagger-v$VERSION-win-arm64.zip"
rm -f "publish/AzTagger-v$VERSION-mac-x64.zip"
rm -f "publish/AzTagger-v$VERSION-mac-arm64.zip"
rm -f "publish/AzTagger-v$VERSION-linux-gtk-x64.zip"
rm -f "publish/AzTagger-v$VERSION-linux-gtk-arm64.zip"

# Create ZIPs
cd publish

zip -j "AzTagger-v$VERSION-win-x64.zip" win-x64/*.exe win-x64/*.dll
echo "Created: publish/AzTagger-v$VERSION-win-x64.zip"

zip -j "AzTagger-v$VERSION-win-arm64.zip" win-arm64/*.exe win-arm64/*.dll
echo "Created: publish/AzTagger-v$VERSION-win-arm64.zip"

zip -r "AzTagger-v$VERSION-mac-x64.zip" mac-x64/AzTagger.Mac.app
echo "Created: publish/AzTagger-v$VERSION-mac-x64.zip"

zip -r "AzTagger-v$VERSION-mac-arm64.zip" mac-arm64/AzTagger.Mac.app
echo "Created: publish/AzTagger-v$VERSION-mac-arm64.zip"

zip -j "AzTagger-v$VERSION-linux-gtk-x64.zip" linux-x64/AzTagger linux-x64/*.png
echo "Created: publish/AzTagger-v$VERSION-linux-gtk-x64.zip"

zip -j "AzTagger-v$VERSION-linux-gtk-arm64.zip" linux-arm64/AzTagger linux-arm64/*.png
echo "Created: publish/AzTagger-v$VERSION-linux-gtk-arm64.zip"

cd ..

echo ""
echo "Done! All packages created successfully."
echo ""
ls -la publish/*.zip 2>/dev/null || echo "No ZIP files found"
