# TrayPerformanceMonitor

A lightweight Windows system tray application that monitors and displays real-time CPU and RAM usage metrics.

![.NET](https://img.shields.io/badge/.NET-8.0-512BD4)
![Platform](https://img.shields.io/badge/platform-Windows-0078D6)
![License](https://img.shields.io/badge/license-MIT-green)

## Features

- **Real-time Monitoring**: Displays CPU and RAM usage in a small, always-on-top window
- **System Tray Integration**: Runs quietly in the system tray
- **Process Analysis**: Identifies resource-intensive processes
- **Performance Logging**: Logs performance data for analysis with automatic log retention management
- **AI-Powered Analysis**: Optional local AI generates summaries and recommendations for performance spikes
- **Internal Program Logging**: Comprehensive internal logging tracks application behavior for debugging
- **Log Retention & Pruning**: Automatic cleanup of old log entries (configurable retention period)
- **Minimal Resource Usage**: Designed to have minimal impact on system performance

## Project Structure

```
TrayPerformanceMonitor/
├── Configuration/          # Application configuration classes
│   └── AppConfiguration.cs
├── Services/               # Business logic and services
│   ├── Interfaces/         # Service interfaces (IoC ready)
│   │   ├── IAiSummaryService.cs
│   │   ├── ILoggingService.cs
│   │   ├── IPerformanceService.cs
│   │   └── IProcessAnalyzer.cs
│   ├── AiSummaryService.cs # Local AI inference for spike analysis
│   ├── LoggingService.cs   # Performance spike and internal logging
│   ├── PerformanceService.cs
│   └── ProcessAnalyzer.cs
├── Models/                 # AI model directory
│   └── model.gguf          # Place your GGUF model here
├── UI/                     # User interface components
│   └── StatusWindow.cs
├── Program.cs              # Application entry point
└── TrayAppContext.cs       # System tray context

TrayPerformanceMonitor.Tests/
├── Configuration/          # Configuration tests
├── Services/               # Service tests
└── TrayPerformanceMonitor.Tests.csproj
```

## Getting Started

### Prerequisites

- Windows 10/11
- .NET 8.0 SDK or later
- Visual Studio 2022 (recommended) or VS Code

### Building

```bash
# Clone the repository
git clone https://github.com/Geetur/systemlogger.git

# Navigate to the project directory
cd "capstone project"

# Restore dependencies
dotnet restore

# Build the solution
dotnet build --configuration Release
```

### Running Tests

```bash
dotnet test --verbosity normal
```

### Running the Application

```bash
dotnet run --project TrayPerformanceMonitor/TrayPerformanceMonitor.csproj
```

## Development

### Architecture

The application follows **SOLID principles** and uses a modular architecture:

- **Separation of Concerns**: UI, Services, and Configuration are clearly separated
- **Interface-based Design**: Services implement interfaces for testability and IoC
- **Single Responsibility**: Each class has a focused, well-defined purpose

### Code Style

This project uses:
- `.editorconfig` for consistent code formatting
- .NET Analyzers for code quality enforcement
- XML documentation for public APIs

### CI/CD

The project includes GitHub Actions workflows:

- **CI Build & Test** (`ci.yml`): Runs on every push/PR
  - Builds the solution
  - Runs all tests
  - Performs code analysis
  - Generates code coverage reports

- **Release** (`release.yml`): Triggered by version tags
  - Creates release artifacts
  - Publishes to GitHub Releases

### Creating a Release

```bash
# Tag a new version
git tag v1.0.0
git push origin v1.0.0
```

## Configuration

The application can be configured via the `AppConfiguration` class:

| Setting | Description | Default |
|---------|-------------|---------|  
| `CpuThreshold` | CPU % threshold for spike detection | 80 |
| `RamThreshold` | RAM % threshold for spike detection | 80 |
| `SpikeTimeThresholdSeconds` | Duration before logging a spike | 10 |
| `LogRetentionDays` | Days to retain log entries before pruning | 7 |
| `AiSummaryEnabled` | Enable AI-powered spike summaries | true |
| `InternalLoggingEnabled` | Enable internal program logging | true |

## AI Model Setup (Optional)

The application can use a local LLaMA model to generate AI-powered summaries for performance spikes. This feature is **optional** - the app works perfectly fine without it.

### Downloading the Model

1. Download a small GGUF model (recommended: TinyLlama 1.1B):
   ```
   https://huggingface.co/TheBloke/TinyLlama-1.1B-Chat-v1.0-GGUF/resolve/main/tinyllama-1.1b-chat-v1.0.Q4_K_M.gguf
   ```

2. Rename the file to `model.gguf`

3. Place it in one of these locations:
   - `TrayPerformanceMonitor/Models/model.gguf` (recommended)
   - Same folder as the executable
   - Your Desktop
   - Your Documents folder

### Why isn't the model included?

The AI model file (~640MB) exceeds GitHub's 100MB file size limit. Users who want AI summaries need to download the model separately.

## Internal Logging

The application includes comprehensive internal logging to track program behavior:

- **Log Location**: `TrayPerformanceMonitor_internal.log` on user's Desktop (or application directory as fallback)
- **Log Events**: Application startup/shutdown, spike detection, AI inference, errors, and key operations
- **Log Format**: Timestamped entries with severity levels (INFO, WARN, ERROR, DEBUG)
- **Retention**: Internal logs follow the same retention period as performance logs

Internal logging can be disabled via `AppConfiguration.InternalLoggingEnabled`.

## Log Files

The application creates two log files:

| File | Purpose |
|------|---------|
| `TrayPerformanceMonitor_log.txt` | Performance spike events with process info and AI analysis |
| `TrayPerformanceMonitor_internal.log` | Internal application events for debugging |

Both files are created on the user's Desktop with automatic retention management.

## Contributing

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/amazing-feature`)
3. Commit your changes (`git commit -m 'Add amazing feature'`)
4. Push to the branch (`git push origin feature/amazing-feature`)
5. Open a Pull Request

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## Acknowledgments

- Built with .NET 8.0 and Windows Forms
- Uses Windows Performance Counters for system metrics

## Intuition and Reasoning

This is a Windows 11 program that I developed as an intern for Outback Construnction Inc.'s IT Department. 
The reason I created it was because I wanted to streamline the troubleshooting process for said department by having an objective source of truth
for the oh-so-common "slow computer" errors that we kept recieving internally. The logic is that 99% of "slow computer" errors are a result of system
resources being over-used, mismanaged, or the like, but it's tough to diagnose exactly what resource is being, say, over-used, and it's also tough to diagnose what 
specific application was the culprit of said resource mismanagement after the fact, so to speak, especially for a non-technical person wherein most of our internal teams were.

Thus, systemlogger was born. Systemlogger tracks the systems resources (just CPU and RAM because all internal computers are equipped with fast SSDs and a reliable, quick internet connection)
and whenever a resource spike occurs, logs the processes associated with that spike. This allows the IT department to very quickly diagnose if the problem was resource misuse,
and if so, what programs were associated with that misuse. AI is also leveraged to give a quick summary and notes of suggestion for what the user could do to avoid the spike in the future.

Overall, systemlogger streamlined the diagnoses of many computer issues because it served as a reliable source of truth for IT department employees. 

This is an example of a project where I owned the end-to-end development of a relevant, specific tool (C# for win 11 desktop) to increase the producticity of a specific team, to overall increase revenue of the company.


