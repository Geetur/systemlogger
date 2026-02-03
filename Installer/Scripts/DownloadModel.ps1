# TrayPerformanceMonitor AI Model Download Script
# Downloads the TinyLlama model for AI-powered spike analysis

param(
    [Parameter(Mandatory=$true)]
    [string]$DestinationPath
)

$ErrorActionPreference = "Stop"
$host.UI.RawUI.WindowTitle = "TrayPerformanceMonitor - AI Model Download"

# Model URL - TinyLlama 1.1B Chat (Q4_K_M quantization - good balance of size/quality)
$ModelUrl = "https://huggingface.co/TheBloke/TinyLlama-1.1B-Chat-v1.0-GGUF/resolve/main/tinyllama-1.1b-chat-v1.0.Q4_K_M.gguf"

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

# Check if model already exists
if (Test-Path $DestinationPath) {
    $existingFile = Get-Item $DestinationPath
    if ($existingFile.Length -gt 100MB) {
        Write-Host "  AI model already exists!" -ForegroundColor Green
        Write-Host "  Location: $DestinationPath"
        Write-Host "  Size: $([math]::Round($existingFile.Length / 1MB, 2)) MB"
        Write-Host ""
        Write-Host "  Press any key to close..." -ForegroundColor Gray
        $null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")
        exit 0
    } else {
        Write-Host "  Existing file appears incomplete. Re-downloading..." -ForegroundColor Yellow
        Remove-Item $DestinationPath -Force
    }
}

Write-Host "  Model: TinyLlama 1.1B (Q4_K_M)" -ForegroundColor White
Write-Host "  Size:  ~640 MB" -ForegroundColor White
Write-Host "  From:  HuggingFace" -ForegroundColor White
Write-Host ""
Write-Host "  Downloading... Please wait." -ForegroundColor Yellow
Write-Host ""

try {
    $TempPath = "$DestinationPath.download"
    
    # Use Invoke-WebRequest with progress bar
    $ProgressPreference = 'Continue'
    
    # Download with built-in progress bar
    Invoke-WebRequest -Uri $ModelUrl -OutFile $TempPath -UseBasicParsing
    
    # Verify download
    if (Test-Path $TempPath) {
        $downloadedFile = Get-Item $TempPath
        if ($downloadedFile.Length -gt 100MB) {
            # Move to final location
            Move-Item -Path $TempPath -Destination $DestinationPath -Force
            
            Write-Host ""
            Write-Host "  ============================================" -ForegroundColor Green
            Write-Host "   Download Complete!" -ForegroundColor Green
            Write-Host "  ============================================" -ForegroundColor Green
            Write-Host ""
            Write-Host "  Model saved to:" -ForegroundColor White
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
    Write-Host ""
    Write-Host "  ============================================" -ForegroundColor Red
    Write-Host "   Download Failed" -ForegroundColor Red
    Write-Host "  ============================================" -ForegroundColor Red
    Write-Host ""
    Write-Host "  Error: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host ""
    Write-Host "  You can download the model manually:" -ForegroundColor Yellow
    Write-Host ""
    Write-Host "  1. Go to:" -ForegroundColor White
    Write-Host "     https://huggingface.co/TheBloke/TinyLlama-1.1B-Chat-v1.0-GGUF" -ForegroundColor Cyan
    Write-Host ""
    Write-Host "  2. Download: tinyllama-1.1b-chat-v1.0.Q4_K_M.gguf" -ForegroundColor White
    Write-Host ""
    Write-Host "  3. Rename to 'model.gguf' and place in:" -ForegroundColor White
    Write-Host "     $DestDir" -ForegroundColor Cyan
    Write-Host ""
    
    # Cleanup temp file if exists
    if (Test-Path "$DestinationPath.download") {
        Remove-Item "$DestinationPath.download" -Force -ErrorAction SilentlyContinue
    }
    
    Write-Host "  Press any key to close..." -ForegroundColor Gray
    $null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")
    exit 1
}
