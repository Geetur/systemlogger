# Build script for TrayPerformanceMonitor Installer
# This script publishes the application and builds the installer

param(
    [string]$Configuration = "Release",
    [switch]$SkipPublish,
    [switch]$SkipInstaller
)

$ErrorActionPreference = "Stop"
$ProjectRoot = Split-Path -Parent $PSScriptRoot

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "TrayPerformanceMonitor Build Script" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Step 1: Publish the application
if (-not $SkipPublish) {
    Write-Host "Step 1: Publishing application..." -ForegroundColor Yellow
    
    $PublishPath = Join-Path $ProjectRoot "publish"
    
    # Clean publish directory
    if (Test-Path $PublishPath) {
        Remove-Item -Path $PublishPath -Recurse -Force
    }
    
    # Publish as framework-dependent (not single file due to LLamaSharp native libs)
    Push-Location $ProjectRoot
    try {
        dotnet publish "TrayPerformanceMonitor/TrayPerformanceMonitor.csproj" `
            --configuration $Configuration `
            --output $PublishPath `
            --self-contained false
        
        if ($LASTEXITCODE -ne 0) {
            throw "dotnet publish failed with exit code $LASTEXITCODE"
        }
        
        Write-Host "Application published to: $PublishPath" -ForegroundColor Green
    }
    finally {
        Pop-Location
    }
    
    # Copy the download script to publish folder
    $ScriptsDir = Join-Path $PublishPath "Scripts"
    if (-not (Test-Path $ScriptsDir)) {
        New-Item -ItemType Directory -Path $ScriptsDir -Force | Out-Null
    }
    Copy-Item -Path (Join-Path $PSScriptRoot "Scripts\DownloadModel.ps1") -Destination $ScriptsDir -Force
    
    Write-Host ""
} else {
    Write-Host "Step 1: Skipping publish (--SkipPublish specified)" -ForegroundColor Gray
}

# Step 2: Build the installer
if (-not $SkipInstaller) {
    Write-Host "Step 2: Building installer..." -ForegroundColor Yellow
    
    # Check for Inno Setup
    $InnoSetupPath = @(
        "${env:ProgramFiles(x86)}\Inno Setup 6\ISCC.exe",
        "${env:ProgramFiles}\Inno Setup 6\ISCC.exe",
        "C:\Program Files (x86)\Inno Setup 6\ISCC.exe",
        "C:\Program Files\Inno Setup 6\ISCC.exe"
    ) | Where-Object { Test-Path $_ } | Select-Object -First 1
    
    if (-not $InnoSetupPath) {
        Write-Host ""
        Write-Host "Inno Setup not found!" -ForegroundColor Red
        Write-Host ""
        Write-Host "Please install Inno Setup 6 from:" -ForegroundColor Yellow
        Write-Host "https://jrsoftware.org/isdl.php" -ForegroundColor Cyan
        Write-Host ""
        Write-Host "After installing, run this script again."
        exit 1
    }
    
    Write-Host "Found Inno Setup at: $InnoSetupPath"
    
    $IssFile = Join-Path $PSScriptRoot "TrayPerformanceMonitor.iss"
    
    & $InnoSetupPath $IssFile
    
    if ($LASTEXITCODE -ne 0) {
        throw "Inno Setup compilation failed with exit code $LASTEXITCODE"
    }
    
    $OutputDir = Join-Path $PSScriptRoot "Output"
    Write-Host ""
    Write-Host "========================================" -ForegroundColor Green
    Write-Host "Installer built successfully!" -ForegroundColor Green
    Write-Host "========================================" -ForegroundColor Green
    Write-Host "Output: $OutputDir" -ForegroundColor Cyan
    Write-Host ""
    
    # List output files
    Get-ChildItem -Path $OutputDir -Filter "*.exe" | ForEach-Object {
        Write-Host "  $($_.Name) ($([math]::Round($_.Length / 1MB, 2)) MB)"
    }
} else {
    Write-Host "Step 2: Skipping installer build (--SkipInstaller specified)" -ForegroundColor Gray
}

Write-Host ""
Write-Host "Build complete!" -ForegroundColor Green
