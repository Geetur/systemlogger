<div align="center">

# 🖥️ TrayPerformanceMonitor

### Intelligent System Resource Monitoring for Windows

*A lightweight, AI-powered system tray application that monitors CPU and RAM usage, automatically detects performance spikes, and provides actionable insights.*

[![.NET 8.0](https://img.shields.io/badge/.NET-8.0-512BD4?style=for-the-badge&logo=dotnet&logoColor=white)](https://dotnet.microsoft.com/)
[![Windows](https://img.shields.io/badge/Windows-10%2F11-0078D6?style=for-the-badge&logo=windows&logoColor=white)](https://www.microsoft.com/windows)
[![License MIT](https://img.shields.io/badge/License-MIT-green?style=for-the-badge)](LICENSE)
[![Release](https://img.shields.io/github/v/release/Geetur/systemlogger?style=for-the-badge&color=orange)](https://github.com/Geetur/systemlogger/releases/latest)

[**📥 Download Latest Release**](https://github.com/Geetur/systemlogger/releases/latest) · [**🐛 Report Bug**](https://github.com/Geetur/systemlogger/issues) · [**💡 Request Feature**](https://github.com/Geetur/systemlogger/issues)

---

<img src="https://raw.githubusercontent.com/microsoft/fluentui-emoji/main/assets/Bar%20chart/3D/bar_chart_3d.png" width="120" alt="Performance Chart"/>

</div>

## 🎯 The Problem

> *"My computer is slow!"*

Every IT department hears this daily. But by the time a technician investigates, the problem is gone. **What caused it? Which application? When exactly?**

**TrayPerformanceMonitor solves this** by continuously monitoring system resources and automatically logging performance spikes with:
- ✅ Exact timestamps
- ✅ Resource usage percentages  
- ✅ Top processes consuming resources
- ✅ **AI-generated analysis and recommendations**

---

## ✨ Features

<table>
<tr>
<td width="50%">

### 📊 Real-Time Monitoring
- Live CPU & RAM usage display
- Always-on-top status window
- System tray integration
- Minimal resource footprint

</td>
<td width="50%">

### 🔍 Intelligent Spike Detection
- Configurable thresholds (default: 80%)
- Sustained spike tracking (10+ seconds)
- Automatic process identification
- **Configurable process count (1-10)**

</td>
</tr>
<tr>
<td width="50%">

### 🤖 AI-Powered Analysis
- **Choose your model**: Full (640 MB) or Lite (320 MB)
- No internet required after download
- Privacy-first: data never leaves your machine
- Actionable recommendations

</td>
<td width="50%">

### 📝 Comprehensive Logging
- Desktop log file for easy access
- Daily headers for organization
- Automatic 7-day retention
- **Single-instance enforcement** (mutex)

</td>
</tr>
</table>

---

## 🚀 Quick Start

### Option 1: Download & Install (Recommended)

<div align="center">

### [📥 Download TrayPerformanceMonitor Installer](https://github.com/Geetur/systemlogger/releases/latest)

*Self-contained — No .NET installation required!*

</div>

1. Download `TrayPerformanceMonitor_Setup_x.x.x.exe` from [Releases](https://github.com/Geetur/systemlogger/releases/latest)
2. Run the installer
3. Choose your options:
   - 🔢 **Process count (1-10)** — How many top processes to log per spike
   - 🤖 **AI Model** — Full (~640 MB, best quality) or Lite (~320 MB, faster)
   - ✅ **Start with Windows** — Launch automatically on login
4. Done! The app appears in your system tray.

### Option 2: Portable Version

1. Download `TrayPerformanceMonitor-vX.X.X-portable.zip`
2. Extract anywhere
3. Run `TrayPerformanceMonitor.exe`

---

## 📸 How It Works

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                         TrayPerformanceMonitor                               │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                              │
│   ┌──────────────┐      ┌──────────────┐      ┌──────────────┐             │
│   │   Monitor    │ ───► │   Detect     │ ───► │     Log      │             │
│   │   CPU/RAM    │      │   Spikes     │      │   + Analyze  │             │
│   └──────────────┘      └──────────────┘      └──────────────┘             │
│         │                      │                      │                     │
│         ▼                      ▼                      ▼                     │
│   ┌──────────────┐      ┌──────────────┐      ┌──────────────┐             │
│   │ Status Window│      │  >80% for    │      │ Desktop Log  │             │
│   │  [CPU: 45%]  │      │   10+ sec?   │      │    + AI      │             │
│   │  [RAM: 62%]  │      │   LOG IT!    │      │  Summary     │             │
│   └──────────────┘      └──────────────┘      └──────────────┘             │
│                                                                              │
└─────────────────────────────────────────────────────────────────────────────┘
```

### Sample Log Output

```
===== 2026-02-03 (Monday) =====
Started: 09:15:23  (Local)
------------------------------------------------

14:32:15 - CPU spike detected (>= 10s): 92.4%
Top CPU-consuming processes:
  1. chrome.exe - 45.2% CPU
  2. Teams.exe - 23.1% CPU  
  3. Outlook.exe - 12.8% CPU

AI Analysis (for CPU spike at 14:32:15):
  The high CPU usage appears to be caused by Google Chrome consuming 
  nearly half of your CPU resources. This is often due to:
  - Multiple tabs open with heavy web applications
  - Extensions running in the background
  
  Recommendations:
  - Close unused Chrome tabs
  - Check for memory-heavy extensions (chrome://extensions)
  - Consider using Chrome's built-in task manager (Shift+Esc)
```

---

## ⚙️ Configuration

All settings are in `Configuration/AppConfiguration.cs`:

| Setting | Description | Default |
|---------|-------------|---------|
| `CpuThreshold` | CPU % that triggers spike detection | `80` |
| `RamThreshold` | RAM % that triggers spike detection | `80` |
| `SpikeTimeThresholdSeconds` | Seconds above threshold before logging | `10` |
| `LogRetentionDays` | Days to keep log entries | `7` |
| `TopProcessCount` | Number of top processes to log | `3` |
| `AiSummaryEnabled` | Enable AI analysis | `true` |

### User Settings (via Installer or settings.json)

| Setting | Description | Range |
|---------|-------------|-------|
| `TopProcessCount` | Processes logged per spike | `1-10` |

The installer creates a `settings.json` file with your chosen configuration:
```json
{
  "TopProcessCount": 5
}
```

> 💡 **Single Instance:** The app uses a mutex to ensure only one instance runs at a time. If you try to launch it again, you'll see a notification.

---

## 🤖 AI Model Setup (Optional)

The AI feature uses **local** models — your data never leaves your computer.

### Model Options

| Model | Size | Quality | Best For |
|-------|------|---------|----------|
| **Full** (TinyLlama 1.1B) | ~640 MB | ⭐⭐⭐ Best | Detailed analysis, complex recommendations |
| **Lite** (Qwen2 0.5B) | ~320 MB | ⭐⭐ Good | Faster responses, lower RAM usage |

### Automatic (During Installation)
Select your preferred model in the installer configuration page. Done!

### Manual Download

**Full Model:**
1. Download [TinyLlama 1.1B GGUF](https://huggingface.co/TheBloke/TinyLlama-1.1B-Chat-v1.0-GGUF/resolve/main/tinyllama-1.1b-chat-v1.0.Q4_K_M.gguf) (~640 MB)

**Lite Model:**
1. Download [Qwen2 0.5B GGUF](https://huggingface.co/Qwen/Qwen2-0.5B-Instruct-GGUF/resolve/main/qwen2-0_5b-instruct-q2_k.gguf) (~320 MB)

**Then:**
2. Rename to `model.gguf`
3. Place in one of:
   - `%INSTALL_DIR%\Models\model.gguf`
   - Your Desktop
   - Your Documents folder

> **Why isn't the model included?** GitHub has a 100MB file limit. The model must be downloaded separately.

---

## 🏗️ Architecture

```
TrayPerformanceMonitor/
├── 📁 Configuration/
│   └── AppConfiguration.cs      # All app settings in one place
│
├── 📁 Services/
│   ├── 📁 Interfaces/           # Contracts for dependency injection
│   │   ├── IAiSummaryService.cs
│   │   ├── ILoggingService.cs
│   │   ├── IPerformanceService.cs
│   │   └── IProcessAnalyzer.cs
│   │
│   ├── AiSummaryService.cs      # LLaMA integration for AI summaries
│   ├── LoggingService.cs        # File logging with retention
│   ├── PerformanceService.cs    # Windows Performance Counters
│   └── ProcessAnalyzer.cs       # Process enumeration & sorting
│
├── 📁 UI/
│   └── StatusWindow.cs          # Always-on-top status display
│
├── 📁 Models/
│   └── model.gguf               # AI model (user-provided)
│
├── Program.cs                   # Entry point
└── TrayAppContext.cs            # System tray & app lifecycle
```

### Design Principles

| Principle | Implementation |
|-----------|----------------|
| **SOLID** | Interface-based services, single responsibility |
| **Testable** | All services mockable via interfaces |
| **Configurable** | Centralized settings in `AppConfiguration` |
| **Resilient** | Graceful degradation if AI model unavailable |

---

## 🛠️ Development

### Prerequisites

- Windows 10/11
- [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- Visual Studio 2022 or VS Code

### Build & Run

```powershell
# Clone
git clone https://github.com/Geetur/systemlogger.git
cd "capstone project"

# Build
dotnet build --configuration Release

# Run
dotnet run --project TrayPerformanceMonitor/TrayPerformanceMonitor.csproj

# Test
dotnet test --verbosity normal
```

### Build the Installer

```powershell
# Requires Inno Setup 6 (https://jrsoftware.org/isdl.php)
cd Installer
.\Build.ps1
# Output: Installer\Output\TrayPerformanceMonitor_Setup_x.x.x.exe
```

---

## 🔄 CI/CD Pipeline

| Workflow | Trigger | Actions |
|----------|---------|---------|
| **CI Build & Test** | Push/PR to `main` | Build → Test → Analyze → Coverage |
| **Release** | Tag `v*.*.*` | Build → Test → Create Installer → Publish to GitHub Releases |

### Creating a Release

```powershell
git tag v1.0.2
git push origin v1.0.2
# GitHub Actions automatically builds and publishes the release
```

---

## 📄 Log Files

| File | Location | Purpose |
|------|----------|---------|
| `TrayPerformanceMonitor_log.txt` | Desktop | Performance spikes + AI analysis |
| `TrayPerformanceMonitor_internal.log` | Desktop | Debug/diagnostic events |

Both files auto-prune entries older than 7 days.

---

## 🤝 Contributing

Contributions are welcome! Here's how:

1. **Fork** the repository
2. **Create** a feature branch (`git checkout -b feature/amazing-feature`)
3. **Commit** your changes (`git commit -m 'Add amazing feature'`)
4. **Push** to the branch (`git push origin feature/amazing-feature`)
5. **Open** a Pull Request

---

## 📜 License

Distributed under the **MIT License**. See [LICENSE](LICENSE) for details.

---

## 💼 Project Background

<div align="center">

*Developed during an internship at **Outback Construction Inc.***

</div>

### The Challenge

The IT department was overwhelmed with "my computer is slow" tickets. By the time they investigated, the issue had resolved itself. There was no way to:
- Know **what** caused the slowdown
- Identify **which application** was responsible  
- Prove whether it was a **real issue** or user perception

### The Solution

**TrayPerformanceMonitor** provides an objective source of truth:

| Before | After |
|--------|-------|
| "My computer was slow yesterday" | "At 2:32 PM, Chrome used 92% CPU for 45 seconds" |
| "I don't know what caused it" | "Top processes: Chrome (45%), Teams (23%), Outlook (13%)" |
| "It happens sometimes" | "AI: Close unused Chrome tabs, check extensions" |

### Impact

- ⏱️ **Reduced diagnosis time** from hours to minutes
- 📊 **Objective evidence** for IT decisions
- 🎯 **Actionable recommendations** for end users
- 🔄 **Self-service insights** reduce ticket volume

---

<div align="center">

### Built With

[![.NET](https://img.shields.io/badge/.NET_8.0-512BD4?style=flat-square&logo=dotnet&logoColor=white)](https://dotnet.microsoft.com/)
[![Windows Forms](https://img.shields.io/badge/Windows_Forms-0078D6?style=flat-square&logo=windows&logoColor=white)](https://docs.microsoft.com/en-us/dotnet/desktop/winforms/)
[![LLamaSharp](https://img.shields.io/badge/LLamaSharp-FF6B6B?style=flat-square)](https://github.com/SciSharp/LLamaSharp)
[![Inno Setup](https://img.shields.io/badge/Inno_Setup-264DE4?style=flat-square)](https://jrsoftware.org/isinfo.php)

---

**⭐ Star this repo if you find it useful!**

[Back to Top](#-trayperformancemonitor)

</div>
