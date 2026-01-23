@echo off
setlocal enabledelayedexpansion

REM Get version from AzTagger.App.csproj
for /f "tokens=2 delims=<>" %%a in ('findstr /i "<Version>" AzTagger.App\AzTagger.App.csproj') do (
    set FULL_VERSION=%%a
)
REM Extract major.minor.patch (remove the last .0)
for /f "tokens=1-3 delims=." %%a in ("!FULL_VERSION!") do (
    set VERSION=%%a.%%b.%%c
)

echo Building and packaging AzTagger v%VERSION% for Windows...

REM Clean publish directory
if exist publish\win-x64 rmdir /s /q publish\win-x64
if exist publish\win-arm64 rmdir /s /q publish\win-arm64

REM Build for Windows x64
echo.
echo Building for win-x64...
dotnet publish AzTagger.Wpf/AzTagger.Wpf.csproj -c Release -f net10.0-windows -r win-x64 --self-contained true -p:PublishSingleFile=true -p:PublishTrimmed=false -o ./publish/win-x64
if errorlevel 1 (
    echo ERROR: Failed to build for win-x64
    exit /b 1
)

REM Build for Windows ARM64
echo.
echo Building for win-arm64...
dotnet publish AzTagger.Wpf/AzTagger.Wpf.csproj -c Release -f net10.0-windows -r win-arm64 --self-contained true -p:PublishSingleFile=true -p:PublishTrimmed=false -o ./publish/win-arm64
if errorlevel 1 (
    echo ERROR: Failed to build for win-arm64
    exit /b 1
)

REM Create ZIP files
echo.
echo Creating ZIP files...

REM Remove old ZIP files if they exist
if exist "publish\AzTagger-v%VERSION%-win-x64.zip" del "publish\AzTagger-v%VERSION%-win-x64.zip"
if exist "publish\AzTagger-v%VERSION%-win-arm64.zip" del "publish\AzTagger-v%VERSION%-win-arm64.zip"

REM Create ZIP for win-x64
powershell -Command "Compress-Archive -Path 'publish\win-x64\*.exe','publish\win-x64\*.dll' -DestinationPath 'publish\AzTagger-v%VERSION%-win-x64.zip'"
if errorlevel 1 (
    echo ERROR: Failed to create ZIP for win-x64
    exit /b 1
)
echo Created: publish\AzTagger-v%VERSION%-win-x64.zip

REM Create ZIP for win-arm64
powershell -Command "Compress-Archive -Path 'publish\win-arm64\*.exe','publish\win-arm64\*.dll' -DestinationPath 'publish\AzTagger-v%VERSION%-win-arm64.zip'"
if errorlevel 1 (
    echo ERROR: Failed to create ZIP for win-arm64
    exit /b 1
)
echo Created: publish\AzTagger-v%VERSION%-win-arm64.zip

echo.
echo Done! Windows packages created successfully.
echo.
dir /b publish\*.zip 2>nul

endlocal
