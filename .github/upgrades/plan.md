# .NET 10.0 Upgrade Plan

## Execution Steps

Execute steps below sequentially one by one in the order they are listed.

1. Validate that a .NET 10.0 SDK required for this upgrade is installed on the machine and if not, help to get it installed.
2. Ensure that the SDK version specified in global.json files is compatible with the .NET 10.0 upgrade.
3. Upgrade AzTagger.Core/AzTagger.Core.csproj
4. Upgrade AzTagger.App/AzTagger.App.csproj
5. Upgrade AzTagger.Mac/AzTagger.Mac.csproj
6. Upgrade AzTagger.Gtk/AzTagger.Gtk.csproj
7. Upgrade AzTagger.Wpf/AzTagger.Wpf.csproj

## Settings

This section contains settings and data used by execution steps.

### Excluded projects

No projects are excluded from this upgrade.

### Aggregate NuGet packages modifications across all projects

NuGet packages used across all selected projects or their dependencies that need version update in projects that reference them.

| Package Name                        | Current Version | New Version | Description                                         |
|:------------------------------------|:---------------:|:-----------:|:----------------------------------------------------|
| Azure.Identity                      | 1.14.0          | 1.15.0      | Deprecated, update to latest version                |
| Eto.Platform.Wpf                    | 2.9.0           | 2.8.2       | Incompatible with .NET 10.0, downgrade required     |
| System.Text.Json                    | 9.0.6           | 10.0.2      | Recommended for .NET 10.0                           |

### Project upgrade details

This section contains details about each project upgrade and modifications that need to be done in the project.

#### AzTagger.Core/AzTagger.Core.csproj modifications

Project properties changes:
  - Target framework should be changed from `net9.0` to `net10.0`

NuGet packages changes:
  - Azure.Identity should be updated from `1.14.0` to `1.15.0` (*deprecated package*)
  - System.Text.Json should be updated from `9.0.6` to `10.0.2` (*recommended for .NET 10.0*)

API changes:
  - 20 API issues identified (source incompatible and behavioral changes)
  - TimeSpan.FromMilliseconds, TimeSpan.FromSeconds signature changes
  - Path.Combine overload changes
  - Uri behavioral changes
  - BinaryData.ToObjectFromJson changes

#### AzTagger.App/AzTagger.App.csproj modifications

Project properties changes:
  - Target framework should be changed from `net9.0` to `net10.0`

API changes:
  - 12 API issues identified (source incompatible)
  - Uri behavioral changes
  - TimeSpan method signature changes

#### AzTagger.Mac/AzTagger.Mac.csproj modifications

Project properties changes:
  - Target framework should be changed from `net9.0` to `net10.0`

#### AzTagger.Gtk/AzTagger.Gtk.csproj modifications

Project properties changes:
  - Target framework should be changed from `net9.0` to `net10.0`

API changes:
  - 1 API issue identified (source incompatible)
  - AppDomain.ProcessExit behavioral change

#### AzTagger.Wpf/AzTagger.Wpf.csproj modifications

Project properties changes:
  - Target framework should be changed from `net9.0-windows` to `net10.0-windows`

NuGet packages changes:
  - Eto.Platform.Wpf package compatibility issue - may need to use version `2.8.2` or wait for .NET 10.0 compatible version
