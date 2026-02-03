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
; Download AI model if selected - model type passed as parameter
Filename: "powershell.exe"; Parameters: "-ExecutionPolicy Bypass -File ""{app}\Scripts\DownloadModel.ps1"" -DestinationPath ""{app}\Models\model.gguf"" -ModelType ""{code:GetSelectedModelType}"""; Description: "Download AI Model"; Flags: postinstall skipifsilent shellexec; Check: ShouldDownloadModel
; Launch application after install
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchProgram,{#StringChange(MyAppName, '&', '&&')}}"; Flags: nowait postinstall skipifsilent

[UninstallDelete]
; Clean up downloaded model on uninstall
Type: files; Name: "{app}\Models\model.gguf"
Type: dirifempty; Name: "{app}\Models"
Type: files; Name: "{app}\Scripts\DownloadModel.ps1"
Type: dirifempty; Name: "{app}\Scripts"
Type: files; Name: "{app}\settings.json"

[Dirs]
; Create Models directory for AI model
Name: "{app}\Models"

[Code]
var
  ConfigPage: TWizardPage;
  ProcessCountCombo: TNewComboBox;
  AIModelCombo: TNewComboBox;
  NoAICheckbox: TNewCheckBox;
  ProcessCountLabel: TNewStaticText;
  AIModelLabel: TNewStaticText;
  AIInfoLabel: TNewStaticText;

procedure InitializeWizard;
var
  i: Integer;
begin
  // Create custom configuration page
  ConfigPage := CreateCustomPage(wpSelectTasks,
    'Configuration Options',
    'Configure application settings');

  // Process Count Label
  ProcessCountLabel := TNewStaticText.Create(ConfigPage);
  ProcessCountLabel.Parent := ConfigPage.Surface;
  ProcessCountLabel.Caption := 'Number of processes to log per spike (1-10):';
  ProcessCountLabel.Left := 0;
  ProcessCountLabel.Top := 16;
  ProcessCountLabel.Width := ConfigPage.SurfaceWidth;
  ProcessCountLabel.Font.Style := [fsBold];

  // Process Count Dropdown
  ProcessCountCombo := TNewComboBox.Create(ConfigPage);
  ProcessCountCombo.Parent := ConfigPage.Surface;
  ProcessCountCombo.Left := 0;
  ProcessCountCombo.Top := ProcessCountLabel.Top + ProcessCountLabel.Height + 8;
  ProcessCountCombo.Width := 80;
  ProcessCountCombo.Style := csDropDownList;
  
  // Add items 1-10
  for i := 1 to 10 do
  begin
    ProcessCountCombo.Items.Add(IntToStr(i));
  end;
  ProcessCountCombo.ItemIndex := 2; // Default to 3

  // AI Model Label
  AIModelLabel := TNewStaticText.Create(ConfigPage);
  AIModelLabel.Parent := ConfigPage.Surface;
  AIModelLabel.Caption := 'AI Model for spike analysis:';
  AIModelLabel.Left := 0;
  AIModelLabel.Top := ProcessCountCombo.Top + ProcessCountCombo.Height + 32;
  AIModelLabel.Width := ConfigPage.SurfaceWidth;
  AIModelLabel.Font.Style := [fsBold];

  // AI Model Dropdown
  AIModelCombo := TNewComboBox.Create(ConfigPage);
  AIModelCombo.Parent := ConfigPage.Surface;
  AIModelCombo.Left := 0;
  AIModelCombo.Top := AIModelLabel.Top + AIModelLabel.Height + 8;
  AIModelCombo.Width := 400;
  AIModelCombo.Style := csDropDownList;
  AIModelCombo.Items.Add('Full Model - TinyLlama 1.1B (~640 MB) - Best quality');
  AIModelCombo.Items.Add('Lite Model - Qwen2 0.5B (~320 MB) - Faster, smaller');
  AIModelCombo.ItemIndex := 0;

  // No AI Checkbox
  NoAICheckbox := TNewCheckBox.Create(ConfigPage);
  NoAICheckbox.Parent := ConfigPage.Surface;
  NoAICheckbox.Caption := 'Skip AI download (I''ll download manually or don''t want AI features)';
  NoAICheckbox.Left := 0;
  NoAICheckbox.Top := AIModelCombo.Top + AIModelCombo.Height + 16;
  NoAICheckbox.Width := ConfigPage.SurfaceWidth;
  NoAICheckbox.Checked := False;

  // AI Info Label
  AIInfoLabel := TNewStaticText.Create(ConfigPage);
  AIInfoLabel.Parent := ConfigPage.Surface;
  AIInfoLabel.Caption := 
    'The AI model provides intelligent analysis of performance spikes.' + #13#10 +
    '• Full Model: Better understanding and recommendations' + #13#10 +
    '• Lite Model: Faster responses, uses less disk space and RAM' + #13#10 +
    '• Both run 100% locally - your data stays private';
  AIInfoLabel.Left := 0;
  AIInfoLabel.Top := NoAICheckbox.Top + NoAICheckbox.Height + 24;
  AIInfoLabel.Width := ConfigPage.SurfaceWidth;
  AIInfoLabel.Height := 80;
  AIInfoLabel.AutoSize := False;
  AIInfoLabel.WordWrap := True;
end;

function GetSelectedProcessCount: Integer;
begin
  Result := ProcessCountCombo.ItemIndex + 1;
end;

function GetSelectedModelType(Param: String): String;
begin
  if AIModelCombo.ItemIndex = 0 then
    Result := 'full'
  else
    Result := 'lite';
end;

function ShouldDownloadModel: Boolean;
begin
  Result := not NoAICheckbox.Checked;
end;

procedure SaveSettings;
var
  SettingsFile: String;
  ProcessCount: Integer;
begin
  SettingsFile := ExpandConstant('{app}\settings.json');
  ProcessCount := GetSelectedProcessCount;
  
  SaveStringToFile(SettingsFile, 
    '{' + #13#10 +
    '  "TopProcessCount": ' + IntToStr(ProcessCount) + #13#10 +
    '}', False);
end;

procedure CurStepChanged(CurStep: TSetupStep);
begin
  if CurStep = ssPostInstall then
  begin
    SaveSettings;
  end;
end;
