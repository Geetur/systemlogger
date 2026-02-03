# TrayPerformanceMonitor AI Model Download Script
# Downloads the selected AI model for spike analysis

param(
    [Parameter(Mandatory=$true)]
    [string]$DestinationPath,
    
    [Parameter(Mandatory=$false)]
    [ValidateSet("full", "lite")]
    [string]$ModelType = "full"
)

$ErrorActionPreference = "Stop"
$host.UI.RawUI.WindowTitle = "TrayPerformanceMonitor - AI Model Download"

# Model configurations
$Models = @{
    "full" = @{
        Name = "TinyLlama 1.1B (Q4_K_M)"
        Size = "~640 MB"
        Url = "https://huggingface.co/TheBloke/TinyLlama-1.1B-Chat-v1.0-GGUF/resolve/main/tinyllama-1.1b-chat-v1.0.Q4_K_M.gguf"
        MinSize = 500MB
    }
    "lite" = @{
        Name = "SmolLM 135M (Q8_0)"
        Size = "~150 MB"
        Url = "https://huggingface.co/bartowski/SmolLM-135M-Instruct-GGUF/resolve/main/SmolLM-135M-Instruct-Q8_0.gguf"
        MinSize = 100MB
    }
}

$SelectedModel = $Models[$ModelType]

Clear-Host
Write-Host ""
Write-Host "  ============================================" -ForegroundColor Cyan
Write-Host "   TrayPerformanceMonitor - AI Model Setup" -ForegroundColor Cyan
Write-Host "  ============================================" -ForegroundColor Cyan
Write-Host ""

# Ensure destination directory exists
$DestDir = Split-Path -Parent $DestinationPath
if (-not (Test-Path $DestDir)) {
    Write-Host "  Creating directory: $DestDir" -ForegroundColor Gray
    New-Item -ItemType Directory -Path $DestDir -Force | Out-Null
}

# Check if model already exists and is the correct type
if (Test-Path $DestinationPath) {
    $existingFile = Get-Item $DestinationPath
    $existingSize = $existingFile.Length
    
    # Determine if the existing model matches the requested type
    # Full model: 500MB - 800MB, Lite model: 100MB - 300MB
    $isFullModel = $existingSize -gt 400MB
    $isLiteModel = $existingSize -gt 80MB -and $existingSize -lt 400MB
    
    $correctModelExists = ($ModelType -eq "full" -and $isFullModel) -or ($ModelType -eq "lite" -and $isLiteModel)
    
    if ($correctModelExists) {
        Write-Host "  AI model already exists!" -ForegroundColor Green
        Write-Host "  Model: $($SelectedModel.Name)" -ForegroundColor White
        Write-Host "  Location: $DestinationPath"
        Write-Host "  Size: $([math]::Round($existingSize / 1MB, 2)) MB"
        Write-Host ""
        Write-Host "  Press any key to close..." -ForegroundColor Gray
        $null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")
        exit 0
    } else {
        if ($isFullModel -or $isLiteModel) {
            Write-Host "  A different model type is installed." -ForegroundColor Yellow
            Write-Host "  Current: $(if ($isFullModel) { 'Full Model' } else { 'Lite Model' })" -ForegroundColor Yellow
            Write-Host "  Requested: $(if ($ModelType -eq 'full') { 'Full Model' } else { 'Lite Model' })" -ForegroundColor Yellow
        } else {
            Write-Host "  Existing file appears incomplete." -ForegroundColor Yellow
        }
        Write-Host "  Replacing with requested model..." -ForegroundColor Yellow
        Write-Host ""
        Remove-Item $DestinationPath -Force
    }
}

Write-Host "  Model: $($SelectedModel.Name)" -ForegroundColor White
Write-Host "  Size:  $($SelectedModel.Size)" -ForegroundColor White
Write-Host "  From:  HuggingFace" -ForegroundColor White
Write-Host ""
Write-Host "  Downloading... Please wait." -ForegroundColor Yellow
Write-Host ""

try {
    $TempPath = "$DestinationPath.download"
    
    # Use Invoke-WebRequest with progress bar
    $ProgressPreference = 'Continue'
    
    # Download with built-in progress bar
    Invoke-WebRequest -Uri $SelectedModel.Url -OutFile $TempPath -UseBasicParsing
    
    # Verify download
    if (Test-Path $TempPath) {
        $downloadedFile = Get-Item $TempPath
        if ($downloadedFile.Length -gt $SelectedModel.MinSize) {
            # Move to final location
            Move-Item -Path $TempPath -Destination $DestinationPath -Force
            
            Write-Host ""
            Write-Host "  ============================================" -ForegroundColor Green
            Write-Host "   Download Complete!" -ForegroundColor Green
            Write-Host "  ============================================" -ForegroundColor Green
            Write-Host ""
            Write-Host "  Model: $($SelectedModel.Name)" -ForegroundColor White
            Write-Host "  Saved to:" -ForegroundColor White
            Write-Host "  $DestinationPath" -ForegroundColor Gray
            Write-Host ""
            Write-Host "  Size: $([math]::Round((Get-Item $DestinationPath).Length / 1MB, 2)) MB" -ForegroundColor White
            Write-Host ""
            Write-Host "  AI analysis is now enabled!" -ForegroundColor Green
            Write-Host ""
            Write-Host "  Press any key to close..." -ForegroundColor Gray
            $null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")
            exit 0
        } else {
            throw "Downloaded file appears incomplete"
        }
    } else {
        throw "Download failed - file not found"
    }
}
catch {
    # Clean up temp file if it exists
    if (Test-Path $TempPath) {
        Remove-Item $TempPath -Force -ErrorAction SilentlyContinue
    }
    
    Write-Host ""
    Write-Host "  ============================================" -ForegroundColor Red
    Write-Host "   Download Failed" -ForegroundColor Red
    Write-Host "  ============================================" -ForegroundColor Red
    Write-Host ""
    Write-Host "  Error: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host ""
    Write-Host "  You can download the model manually:" -ForegroundColor Yellow
    Write-Host ""
    Write-Host "  1. Visit: $($SelectedModel.Url)" -ForegroundColor White
    Write-Host ""
    Write-Host "  2. Save the file as:" -ForegroundColor White
    Write-Host "     $DestinationPath" -ForegroundColor Gray
    Write-Host ""
    Write-Host "  Press any key to close..." -ForegroundColor Gray
    $null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")
    exit 1
}
