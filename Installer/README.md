# TrayPerformanceMonitor Installer

This folder contains the installer build scripts for TrayPerformanceMonitor.

## Prerequisites

- [Inno Setup 6](https://jrsoftware.org/isdl.php) - Free installer builder for Windows
- .NET 8.0 SDK

## Building the Installer

### Quick Build

```powershell
.\Build.ps1
```

This will:
1. Publish the application to `../publish/`
2. Compile the Inno Setup installer
3. Output the installer to `Output/`

### Build Options

```powershell
# Skip publishing (use existing publish folder)
.\Build.ps1 -SkipPublish

# Skip installer build (only publish)
.\Build.ps1 -SkipInstaller

# Build with Debug configuration
.\Build.ps1 -Configuration Debug
```

## Installer Features

The installer provides these options to users:

- **Desktop shortcut** (optional)
- **Start with Windows** (optional, checked by default) - Adds registry entry for auto-start
- **Download AI model** (optional) - Downloads ~640MB TinyLlama model for AI analysis

## Files

| File | Description |
|------|-------------|
| `TrayPerformanceMonitor.iss` | Inno Setup script |
| `Build.ps1` | PowerShell build script |
| `Scripts/DownloadModel.ps1` | AI model download script |

## Output

After building, the installer will be in:
```
Output/TrayPerformanceMonitor_Setup_1.0.0.exe
```
