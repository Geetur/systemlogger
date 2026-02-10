# TrayPerformanceMonitor - Technical Overview

## Table of Contents
1. [High-Level Overview](#high-level-overview)
2. [Low-Level Technical Details](#low-level-technical-details)
3. [Architecture Diagrams](#architecture-diagrams)
4. [Data Flow](#data-flow)
5. [API Reference](#api-reference)

---

# High-Level Overview

## Executive Summary

**TrayPerformanceMonitor** is a Windows desktop application that provides real-time system performance monitoring with AI-powered analysis. The application runs in the system tray and automatically detects, logs, and analyzes sustained CPU and RAM usage spikes using local large language models (LLMs).

## Key Features

| Feature | Description |
|---------|-------------|
| 🖥️ **Real-time Monitoring** | Continuous CPU and RAM usage tracking at 500ms intervals |
| 📊 **Always-visible Display** | Transparent overlay widget positioned near the Windows taskbar |
| 🚨 **Spike Detection** | Automatic detection of sustained high-resource usage (>80% for 10+ seconds) |
| 🤖 **AI Analysis** | Local LLM-powered explanations and recommendations for performance issues |
| 📝 **Automatic Logging** | Performance spike events logged to user's Desktop with 7-day retention |
| ⚙️ **Configurable** | User settings for model selection and process count preferences |

## Target Users

- **IT Professionals** managing Windows workstations
- **System Administrators** troubleshooting performance issues
- **Power Users** wanting insights into system resource consumption
- **Help Desk Staff** diagnosing end-user performance complaints

## Technology Stack

| Component | Technology |
|-----------|------------|
| **Runtime** | .NET 8.0 (Windows Forms) |
| **AI Framework** | LLamaSharp 0.18.0 with CPU backend |
| **AI Models** | GGUF format (TinyLlama 1.1B / Qwen2 0.5B) |
| **Performance Counters** | System.Diagnostics.PerformanceCounter |
| **Testing** | xUnit with 49 unit tests |
| **Installer** | Inno Setup |
| **IPC** | Named Pipes (single-instance signaling) |

## System Requirements

- **OS**: Windows 10/11 (64-bit)
- **RAM**: 4GB minimum (8GB recommended for AI features)
- **Disk**: ~700MB for full model, ~350MB for lite model
- **.NET**: .NET 8.0 Desktop Runtime

---

# Low-Level Technical Details

## Project Structure

```
TrayPerformanceMonitor/
├── Configuration/           # Application settings and constants
│   ├── AppConfiguration.cs  # Compile-time constants (thresholds, intervals)
│   └── UserSettings.cs      # Runtime user preferences (JSON persistence)
├── Services/                # Core business logic
│   ├── Interfaces/          # Service contracts (dependency injection)
│   │   ├── IAiSummaryService.cs
│   │   ├── ILoggingService.cs
│   │   ├── IPerformanceService.cs
│   │   └── IProcessAnalyzer.cs
│   ├── AiSummaryService.cs      # LLM integration and prompt engineering
│   ├── LoggingService.cs        # File-based logging with retention
│   ├── PerformanceService.cs    # Windows performance counter wrapper
│   └── ProcessAnalyzer.cs       # Process enumeration and ranking
├── UI/                      # User interface components
│   ├── LogViewerWindow.cs   # Terminal-style log viewer with search
│   ├── MainHubWindow.cs     # Application hub (desktop shortcut entry)
│   ├── SettingsDialog.cs    # Settings configuration dialog
│   └── StatusWindow.cs      # Transparent overlay widget
├── TrayAppContext.cs        # Main application coordinator + IPC listener
├── Program.cs               # Entry point (mutex + named-pipe IPC)
└── Resources/               # Embedded resources (icons)
```

## Core Components

### 1. TrayAppContext (Application Coordinator)

**Purpose**: Central orchestrator managing the application lifecycle, service coordination, and event handling.

**Key Responsibilities**:
- Initializes and wires up all services via constructor injection
- Creates and manages the system tray icon and context menu
- Runs the performance monitoring timer (500ms tick)
- Coordinates spike detection using `SpikeTracker` instances
- Triggers AI analysis asynchronously when spikes are detected

**Spike Detection Algorithm**:
```
SpikeTracker:
├── Threshold: 80% (configurable)
├── Duration: 10 seconds sustained
├── State: seconds_above_threshold, spike_active
└── Logic:
    IF current_value > threshold:
        seconds_above_threshold += interval
        IF seconds_above_threshold >= duration_threshold AND NOT spike_active:
            spike_active = true
            TRIGGER spike_event
    ELSE:
        seconds_above_threshold = 0
        spike_active = false
```

### 2. PerformanceService (Metrics Collection)

**Purpose**: Wraps Windows Performance Counters to provide CPU and RAM usage metrics.

**Implementation Details**:
- Uses `PerformanceCounter` class from `System.Diagnostics`
- CPU Counter: `Processor Information` / `% Processor Utility` / `_Total`
- RAM Counter: `Memory` / `% Committed Bytes In Use`
- Thread-safe with disposal pattern implementation

**Performance Considerations**:
- First call to `NextValue()` returns 0 (warm-up required)
- Counters are expensive to create; instances are cached
- Disposal releases unmanaged counter handles

### 3. ProcessAnalyzer (Resource Attribution)

**Purpose**: Identifies which processes are consuming the most CPU or RAM.

**Implementation**:
```csharp
GetTopCpuProcesses(count):
    1. Enumerate all processes via Process.GetProcesses()
    2. Calculate CPU time delta over sampling period
    3. Sort by TotalProcessorTime descending
    4. Return top N with formatted output

GetTopRamProcesses(count):
    1. Enumerate all processes
    2. Read WorkingSet64 (physical memory)
    3. Sort by memory usage descending
    4. Return top N with formatted output
```

**Output Format**:
```
  - chrome (PID 1234): 45.2%
  - code (PID 5678): 23.1%
  - teams (PID 9012): 12.8%
```

### 4. AiSummaryService (LLM Integration)

**Purpose**: Generates human-readable explanations of performance spikes using local AI models.

**Architecture**:
```
┌─────────────────────────────────────────────────────────┐
│                    AiSummaryService                      │
├─────────────────────────────────────────────────────────┤
│  ┌─────────────┐    ┌─────────────┐    ┌─────────────┐ │
│  │ LLamaWeights│───▶│ LLamaContext│───▶│  Executor   │ │
│  │  (Model)    │    │  (Session)  │    │ (Inference) │ │
│  └─────────────┘    └─────────────┘    └─────────────┘ │
├─────────────────────────────────────────────────────────┤
│  Model Detection: File size → full (>400MB) / lite     │
│  Prompt Format: TinyLlama (Llama) / Qwen2 (ChatML)     │
│  Thread Safety: Lock-based synchronization             │
└─────────────────────────────────────────────────────────┘
```

**Model-Specific Prompt Formats**:

*TinyLlama (Full Model)*:
```
<|system|>
{system_prompt}</s>
<|user|>
{user_message}</s>
<|assistant|>
```

*Qwen2 (Lite Model)*:
```
<|im_start|>system
{system_prompt}<|im_end|>
<|im_start|>user
{user_message}<|im_end|>
<|im_start|>assistant
```

**Inference Parameters**:
| Parameter | Full Model | Lite Model |
|-----------|------------|------------|
| Context Size | 2048 | 1024 |
| Batch Size | 512 | 256 |
| Max Tokens | 300+ | 200 |
| Temperature | 0.7 | 0.7 |
| Threads | CPU_cores / 2 | CPU_cores / 2 |

### 5. LoggingService (Persistence)

**Purpose**: Writes performance spike events to a log file on the user's Desktop.

**Log Format**:
```
========================================
Performance Log - 2026-02-04
========================================

[14:32:15] CPU Spike Detected: 92.3%
Top Processes:
  - chrome (PID 1234): 45.2%
  - code (PID 5678): 23.1%

[AI Analysis appended at 14:32:45]
📋 What's Happening: Your computer is working hard...
```

**Retention Policy**:
- Entries older than 7 days are automatically pruned
- Pruning occurs at application startup
- Daily headers separate entries by date

### 6. StatusWindow (UI Overlay)

**Purpose**: Always-visible transparent widget showing real-time CPU/RAM percentages.

**Win32 Integration**:
```csharp
CreateParams.ExStyle:
├── WS_EX_TOOLWINDOW  // No taskbar button
├── WS_EX_LAYERED     // Transparency support
└── WS_EX_NOACTIVATE  // Never steal focus

SetWindowPos(HWND_TOPMOST):
├── SWP_NOMOVE | SWP_NOSIZE
├── SWP_NOACTIVATE
└── SWP_SHOWWINDOW
```

**Focus-Stealing Prevention**:
- `EnsurePinned()` skips when any other app window has focus (`ContainsFocus` check)
- Prevents the 250ms keep-pinned timer from disrupting LogViewer/MainHub interaction
- `SuspendPinning` property for explicit suspension during modal dialogs

### 8. LogViewerWindow (Built-In Log Viewer)

**Purpose**: Terminal-style dark UI for viewing, searching, and navigating spike logs without leaving the app.

**Key Features**:
- `RichTextBox` with per-line syntax highlighting (spike → red, AI → green, dates → cyan)
- **Live search** with 300ms debounce timer, case-insensitive `string.IndexOf` scan
- All matches highlighted (dim amber), current match bright amber with `ScrollToMatchCentered`
- Match counter label (e.g. "3 / 51") with ◀ Prev / ▶ Next navigation
- `FlowLayoutPanel` toolbar with margin-based spacing (no absolute positioning)
- Auto-refresh pauses when search is active, search box is focused, or text is selected
- Focus preservation: `LoadLogContents()` saves/restores `ActiveControl` in a `finally` block

**Layout (Z-order)**:
```
Controls[0]: _toolbarPanel  (FlowLayoutPanel, Dock.Top, front)
Controls[1]: _headerPanel   (Panel, Dock.Top)
Controls[2]: _statusBarPanel(Panel, Dock.Bottom)
Controls[3]: _logTextBox    (RichTextBox, Dock.Fill, back)
```

### 9. MainHubWindow (Application Hub)

**Purpose**: Central landing page shown when the desktop shortcut is clicked or tray icon is double-clicked.

**Layout**: DPI-aware `TableLayoutPanel` (4 equal rows, 1 column) with `AutoScaleMode.Dpi`.

**Buttons**: View Logs (primary/gold border), Settings, Show Performance, Exit — all `Dock.Fill` with `Margin`.

### 10. Single-Instance IPC (Named Pipes)

**Purpose**: When a second instance launches, it signals the running instance to show the MainHub.

**Flow**:
```
Second Launch (Program.cs):
  1. Mutex acquired? No → connect to named pipe
  2. Send "SHOW" message via NamedPipeClientStream
  3. Exit gracefully

Running Instance (TrayAppContext.cs):
  1. ListenForShowSignalAsync() loops on NamedPipeServerStream
  2. Receives "SHOW" → Invoke(ShowMainHub) on UI thread
  3. MainHubWindow shown and brought to front
```

**Pipe Name**: `TrayPerformanceMonitor_ShowHub_8A5E2D3F`

**Positioning Logic**:
1. Find taskbar via `FindWindow("Shell_TrayWnd")`
2. Get taskbar bounds via `GetWindowRect`
3. Position widget inside taskbar area (left side)
4. Fallback: Bottom-left of working area

### 7. UserSettings (Configuration)

**Purpose**: Persists user preferences to JSON file for runtime configuration.

**Settings Schema**:
```json
{
  "TopProcessCount": 3,    // 1-10: Processes to log per spike
  "ModelType": "full"      // "full" or "lite"
}
```

**Model File Naming**:
- Full Model: `Models/model-full.gguf`
- Lite Model: `Models/model-lite.gguf`
- Legacy: `Models/model.gguf` (auto-migrated)

---

## Architecture Diagrams

### Component Interaction

```
┌─────────────────────────────────────────────────────────────────┐
│                         TrayAppContext                          │
│  ┌─────────────┐  ┌──────────────┐  ┌────────────────────────┐ │
│  │ NotifyIcon  │  │ StatusWindow │  │    Timer (500ms)       │ │
│  │ (Tray Icon) │  │  (Overlay)   │  │                        │ │
│  └──────┬──────┘  └──────┬───────┘  └───────────┬────────────┘ │
│         │                │                       │              │
│         ▼                ▼                       ▼              │
│  ┌─────────────────────────────────────────────────────────┐   │
│  │                    Event Handlers                        │   │
│  │  • Context Menu Actions                                  │   │
│  │  • Timer Tick → UpdatePerformanceMetrics()              │   │
│  │  • Spike Detection → CheckAndLogSpike()                 │   │
│  └─────────────────────────────────────────────────────────┘   │
└───────────────────────────────┬─────────────────────────────────┘
                                │
        ┌───────────────────────┼───────────────────────┐
        ▼                       ▼                       ▼
┌───────────────┐     ┌─────────────────┐     ┌─────────────────┐
│ Performance   │     │ ProcessAnalyzer │     │  LoggingService │
│   Service     │     │                 │     │                 │
│ ─────────────│     │ ───────────────│     │ ───────────────│
│ • GetCpuUsage│     │ • GetTopCpu()   │     │ • LogSpike()    │
│ • GetRamUsage│     │ • GetTopRam()   │     │ • AppendAI()    │
└───────────────┘     └─────────────────┘     └─────────────────┘
                                                      │
                                                      ▼
                                              ┌─────────────────┐
                                              │ AiSummaryService│
                                              │ ───────────────│
                                              │ • LoadModel()   │
                                              │ • GenerateAsync│
                                              └─────────────────┘
```

### Data Flow

```
    ┌────────────────┐
    │  Timer Tick    │
    │   (500ms)      │
    └───────┬────────┘
            │
            ▼
    ┌────────────────┐      ┌──────────────────┐
    │ PerformanceService │──▶│ StatusWindow     │
    │ GetCurrentMetrics  │   │ UpdateStatus()   │
    └───────┬────────┘      └──────────────────┘
            │
            ▼
    ┌────────────────┐
    │ SpikeTracker   │
    │ (CPU & RAM)    │
    └───────┬────────┘
            │
            │ spike_detected?
            ▼
    ┌────────────────┐
    │ ProcessAnalyzer│
    │ GetTopProcesses│
    └───────┬────────┘
            │
            ├─────────────────────────────┐
            ▼                             ▼
    ┌────────────────┐           ┌────────────────┐
    │ LoggingService │           │ AiSummaryService│
    │ LogSpike()     │           │ GenerateAsync() │
    │ (immediate)    │           │ (background)    │
    └────────────────┘           └───────┬────────┘
                                         │
                                         ▼
                                 ┌────────────────┐
                                 │ LoggingService │
                                 │ AppendAiSummary│
                                 └────────────────┘
```

---

## API Reference

### IPerformanceService

```csharp
public interface IPerformanceService : IDisposable
{
    /// <summary>Gets current CPU usage (0-100%).</summary>
    float GetCpuUsage();
    
    /// <summary>Gets current RAM usage (0-100%).</summary>
    float GetRamUsage();
    
    /// <summary>Gets both metrics in a single call.</summary>
    PerformanceMetrics GetCurrentMetrics();
}

public record PerformanceMetrics(float CpuUsagePercent, float RamUsagePercent);
```

### IProcessAnalyzer

```csharp
public interface IProcessAnalyzer
{
    /// <summary>Gets top CPU-consuming processes.</summary>
    /// <param name="count">Number of processes (1-10).</param>
    /// <returns>Formatted string with process names and usage.</returns>
    string GetTopCpuProcesses(int count);
    
    /// <summary>Gets top RAM-consuming processes.</summary>
    string GetTopRamProcesses(int count);
}
```

### IAiSummaryService

```csharp
public interface IAiSummaryService : IDisposable
{
    /// <summary>Whether an AI model is loaded and ready.</summary>
    bool IsModelLoaded { get; }
    
    /// <summary>Attempts to load a GGUF model file.</summary>
    bool TryLoadModel(string modelPath);
    
    /// <summary>Generates a synchronous spike summary.</summary>
    string GenerateSpikeSummary(string metricName, float value, string topProcessesInfo);
    
    /// <summary>Generates an async spike summary with cancellation.</summary>
    Task<string> GenerateSpikeSummaryAsync(
        string metricName, 
        float value, 
        string topProcessesInfo,
        CancellationToken cancellationToken = default);
}
```

### ILoggingService

```csharp
public interface ILoggingService : IDisposable
{
    /// <summary>Gets the path to the active log file.</summary>
    string LogFilePath { get; }
    
    /// <summary>Logs a performance spike event.</summary>
    void LogPerformanceSpike(string metricName, float value, string topProcessesInfo);
    
    /// <summary>Appends AI analysis to the most recent spike.</summary>
    void AppendAiSummary(string metricName, DateTime spikeTime, string aiSummary);
    
    /// <summary>Removes log entries older than retention period.</summary>
    void PruneOldEntries();
}
```

---

## Configuration Constants

### AppConfiguration

| Constant | Value | Description |
|----------|-------|-------------|
| `TimerIntervalMs` | 500 | Metric polling interval |
| `CpuThreshold` | 80.0% | CPU spike detection threshold |
| `RamThreshold` | 80.0% | RAM spike detection threshold |
| `SpikeTimeThresholdSeconds` | 10 | Sustained duration for spike |
| `TopProcessCount` | 3 | Default processes to log |
| `LogRetentionDays` | 7 | Log file retention period |
| `AiContextSize` | 2048 | LLM context window |
| `AiMaxTokens` | 400 | Max tokens per response |
| `AiTimeoutSeconds` | 60 | AI inference timeout |

---

## Security Considerations

1. **Local-Only Processing**: All AI inference runs locally; no data leaves the device
2. **No Elevation Required**: Runs with standard user permissions
3. **Process Access**: Limited to read-only process enumeration
4. **File Access**: Only writes to user's Desktop (log file) and app directory (settings)
5. **No Network Access**: Application does not make network requests (except model download)

---

## Performance Characteristics

| Metric | Value |
|--------|-------|
| **Idle Memory** | ~50-80 MB (without model) |
| **With Full Model** | ~800-1200 MB |
| **With Lite Model** | ~400-600 MB |
| **CPU Usage (Idle)** | <1% |
| **CPU Usage (AI Active)** | 30-80% (during inference) |
| **AI Response Time** | 5-30 seconds (CPU dependent) |

---

## Build & Deployment

### Build Commands

```powershell
# Debug build
dotnet build --configuration Debug

# Release build
dotnet build --configuration Release

# Run tests
dotnet test --configuration Release

# Publish self-contained
dotnet publish -c Release -r win-x64 --self-contained
```

### Installer Build

```powershell
cd Installer
.\Build.ps1
# Creates: Output/TrayPerformanceMonitor_Setup.exe
```

---

*Document Version: 1.3 | Last Updated: February 10, 2026*
