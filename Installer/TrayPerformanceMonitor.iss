; TrayPerformanceMonitor Installer Script
; Built with Inno Setup - https://jrsoftware.org/isinfo.php

#define MyAppName "TrayPerformanceMonitor"
#define MyAppVersion "1.0.0"
#define MyAppPublisher "Jeter Pontes"
#define MyAppURL "https://github.com/Geetur/systemlogger"
#define MyAppExeName "TrayPerformanceMonitor.exe"

[Setup]
; NOTE: The value of AppId uniquely identifies this application.
AppId={{8A5E2D3F-4B6C-4D8E-9F1A-2B3C4D5E6F7A}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppVerName={#MyAppName} {#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
AppSupportURL={#MyAppURL}
AppUpdatesURL={#MyAppURL}
DefaultDirName={autopf}\{#MyAppName}
DefaultGroupName={#MyAppName}
AllowNoIcons=yes
; Output settings
OutputDir=Output
OutputBaseFilename=TrayPerformanceMonitor_Setup_{#MyAppVersion}
; Compression
Compression=lzma2/ultra64
SolidCompression=yes
; Installer appearance
WizardStyle=modern
; Custom icon for installer and uninstaller
SetupIconFile=..\TrayPerformanceMonitor\Resources\app.ico
; Privileges - per-user install by default
PrivilegesRequired=lowest
PrivilegesRequiredOverridesAllowed=dialog
; Uninstaller
UninstallDisplayIcon={app}\{#MyAppExeName}
UninstallDisplayName={#MyAppName}

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked
Name: "startupicon"; Description: "Start {#MyAppName} when Windows starts"; GroupDescription: "Startup Options:"; Flags: checkedonce
Name: "downloadai"; Description: "Download AI model for intelligent spike analysis (~640 MB)"; GroupDescription: "AI Features:"; Flags: unchecked

[Files]
; Main application files - adjust source path based on your publish output
Source: "..\publish\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs
; Download script for AI model
Source: "Scripts\DownloadModel.ps1"; DestDir: "{app}\Scripts"; Flags: ignoreversion

[Icons]
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{group}\{cm:UninstallProgram,{#MyAppName}}"; Filename: "{uninstallexe}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Registry]
; Add to startup if selected
Root: HKCU; Subkey: "Software\Microsoft\Windows\CurrentVersion\Run"; ValueType: string; ValueName: "{#MyAppName}"; ValueData: """{app}\{#MyAppExeName}"""; Flags: uninsdeletevalue; Tasks: startupicon

[Run]
; Download AI model if selected - runs in visible window so user sees progress
Filename: "powershell.exe"; Parameters: "-ExecutionPolicy Bypass -File ""{app}\Scripts\DownloadModel.ps1"" -DestinationPath ""{app}\Models\model.gguf"""; Description: "Download AI Model (~640 MB)"; Flags: postinstall skipifsilent shellexec; Tasks: downloadai
; Launch application after install
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchProgram,{#StringChange(MyAppName, '&', '&&')}}"; Flags: nowait postinstall skipifsilent

[UninstallDelete]
; Clean up downloaded model on uninstall
Type: files; Name: "{app}\Models\model.gguf"
Type: dirifempty; Name: "{app}\Models"
Type: files; Name: "{app}\Scripts\DownloadModel.ps1"
Type: dirifempty; Name: "{app}\Scripts"

[Dirs]
; Create Models directory for AI model
Name: "{app}\Models"

[Code]
// Custom page for AI information
var
  AIInfoPage: TOutputMsgMemoWizardPage;

procedure InitializeWizard;
begin
  AIInfoPage := CreateOutputMsgMemoPage(wpSelectTasks,
    'AI Analysis Feature', 
    'Information about the AI-powered spike analysis',
    'The AI analysis feature uses a local language model to provide intelligent summaries and recommendations when performance spikes occur.' + #13#10 + #13#10 +
    'Benefits:' + #13#10 +
    '• Automatic analysis of what caused the spike' + #13#10 +
    '• Recommendations on how to resolve issues' + #13#10 +
    '• No internet required after download' + #13#10 +
    '• Your data stays private on your machine' + #13#10 + #13#10 +
    'Note: The AI model is approximately 640 MB. If you choose to download it, the installer will fetch it from HuggingFace. You can always download it later manually.',
    '');
end;
