# TrayPerformanceMonitor AI Model Download Script
# Downloads the TinyLlama model for AI-powered spike analysis

param(
    [Parameter(Mandatory=$true)]
    [string]$DestinationPath
)

$ErrorActionPreference = "Stop"

# Model URL - TinyLlama 1.1B Chat (Q4_K_M quantization - good balance of size/quality)
$ModelUrl = "https://huggingface.co/TheBloke/TinyLlama-1.1B-Chat-v1.0-GGUF/resolve/main/tinyllama-1.1b-chat-v1.0.Q4_K_M.gguf"

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "TrayPerformanceMonitor AI Model Setup" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Ensure destination directory exists
$DestDir = Split-Path -Parent $DestinationPath
if (-not (Test-Path $DestDir)) {
    Write-Host "Creating directory: $DestDir"
    New-Item -ItemType Directory -Path $DestDir -Force | Out-Null
}

# Check if model already exists
if (Test-Path $DestinationPath) {
    $existingFile = Get-Item $DestinationPath
    if ($existingFile.Length -gt 100MB) {
        Write-Host "AI model already exists at: $DestinationPath" -ForegroundColor Green
        Write-Host "Size: $([math]::Round($existingFile.Length / 1MB, 2)) MB"
        exit 0
    } else {
        Write-Host "Existing file appears incomplete. Re-downloading..." -ForegroundColor Yellow
        Remove-Item $DestinationPath -Force
    }
}

Write-Host "Downloading AI model from HuggingFace..." -ForegroundColor Yellow
Write-Host "URL: $ModelUrl"
Write-Host "Destination: $DestinationPath"
Write-Host ""
Write-Host "This may take several minutes depending on your internet connection."
Write-Host "Model size: ~640 MB"
Write-Host ""

try {
    # Use BITS for better download handling with progress
    $TempPath = "$DestinationPath.download"
    
    # Try using Invoke-WebRequest with progress
    $ProgressPreference = 'Continue'
    
    # For large files, use .NET WebClient for better performance
    $webClient = New-Object System.Net.WebClient
    
    # Add event handler for progress
    $downloadComplete = $false
    $lastProgress = 0
    
    Register-ObjectEvent -InputObject $webClient -EventName DownloadProgressChanged -Action {
        $percent = $EventArgs.ProgressPercentage
        if ($percent -ne $script:lastProgress -and $percent % 5 -eq 0) {
            Write-Host "Download progress: $percent%" -ForegroundColor Gray
            $script:lastProgress = $percent
        }
    } | Out-Null
    
    Register-ObjectEvent -InputObject $webClient -EventName DownloadFileCompleted -Action {
        $script:downloadComplete = $true
    } | Out-Null
    
    Write-Host "Starting download..." -ForegroundColor Cyan
    $webClient.DownloadFileAsync([Uri]$ModelUrl, $TempPath)
    
    # Wait for download to complete
    while (-not $downloadComplete) {
        Start-Sleep -Milliseconds 500
    }
    
    $webClient.Dispose()
    
    # Verify download
    if (Test-Path $TempPath) {
        $downloadedFile = Get-Item $TempPath
        if ($downloadedFile.Length -gt 100MB) {
            # Move to final location
            Move-Item -Path $TempPath -Destination $DestinationPath -Force
            
            Write-Host ""
            Write-Host "========================================" -ForegroundColor Green
            Write-Host "Download Complete!" -ForegroundColor Green
            Write-Host "========================================" -ForegroundColor Green
            Write-Host "Model saved to: $DestinationPath"
            Write-Host "Size: $([math]::Round((Get-Item $DestinationPath).Length / 1MB, 2)) MB"
            Write-Host ""
            Write-Host "AI analysis is now enabled for TrayPerformanceMonitor!"
            exit 0
        } else {
            Write-Host "Downloaded file appears incomplete. Please try again." -ForegroundColor Red
            Remove-Item $TempPath -Force -ErrorAction SilentlyContinue
            exit 1
        }
    } else {
        Write-Host "Download failed. File not found." -ForegroundColor Red
        exit 1
    }
}
catch {
    Write-Host ""
    Write-Host "========================================" -ForegroundColor Red
    Write-Host "Download Failed" -ForegroundColor Red
    Write-Host "========================================" -ForegroundColor Red
    Write-Host "Error: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host ""
    Write-Host "You can download the model manually from:"
    Write-Host $ModelUrl
    Write-Host ""
    Write-Host "Save it as: $DestinationPath"
    
    # Cleanup temp file if exists
    if (Test-Path "$DestinationPath.download") {
        Remove-Item "$DestinationPath.download" -Force -ErrorAction SilentlyContinue
    }
    
    exit 1
}
