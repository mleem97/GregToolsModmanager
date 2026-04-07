; Inno Setup 6 — GregTools Modmanager (WorkshopUploader)
; Kompilieren: ISCC.exe GregToolsModmanager.iss  (oder ..\build.ps1)
; Version wird per ..\build.ps1 aus WorkshopUploader.csproj als /DMyAppVersion übergeben.

#ifndef MyAppVersion
#define MyAppVersion "1.0.0"
#endif

#define MyAppName "GregTools Modmanager"
#define MyAppPublisher "GregFramework"
#define MyAppExeName "WorkshopUploader.exe"
#define MyAppURL "https://github.com/mleem97/gregFramework"

[Setup]
; Gleiche AppId = Update/Reinstall überschreibt dieselbe Installation (Programme & Features).
AppId={{7A2F9E1B-4C3D-5E6F-7890-ABCDEF123401}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppVerName={#MyAppName} {#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
AppSupportURL={#MyAppURL}
AppUpdatesURL={#MyAppURL}
DefaultDirName={autopf64}\{#MyAppName}
UsePreviousAppDir=yes
DefaultGroupName={#MyAppName}
AllowNoIcons=yes
; Vor Update/Reinstall laufende Instanz schließen (Restart Manager). Bei Problemen beim Start auf „no“ setzen.
CloseApplications=yes
RestartApplications=no
PrivilegesRequired=admin
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
OutputDir=Output
OutputBaseFilename=GregToolsModmanager-{#MyAppVersion}-Setup
Compression=lzma2/ultra64
SolidCompression=yes
WizardStyle=modern
DisableWelcomePage=no
DisableProgramGroupPage=no
UninstallDisplayIcon={app}\{#MyAppExeName}
MinVersion=10.0.17763
VersionInfoVersion={#MyAppVersion}
VersionInfoCompany={#MyAppPublisher}
VersionInfoDescription={#MyAppName} Setup
VersionInfoProductName={#MyAppName}

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"
Name: "german"; MessagesFile: "compiler:Languages\German.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: checkedonce

[Files]
; ignoreversion: Dateien immer durch die neue Version ersetzen (auch gleiche Versionsnummer).
Source: "..\bin\Release\net9.0-windows10.0.19041.0\win10-x64\publish\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[InstallDelete]
; Wichtig für self-contained MAUI/WinAppSDK: alte Runtime-Dateien vor dem Kopieren entfernen,
; damit kein gemischter DLL-Bestand zwischen Releases übrig bleibt (sonst Start-/UI-Crashes).
Type: filesandordirs; Name: "{app}\*"

[Icons]
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; WorkingDir: "{app}"
Name: "{group}\{cm:UninstallProgram,{#MyAppName}}"; Filename: "{uninstallexe}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; WorkingDir: "{app}"; Tasks: desktopicon

[Run]
; runasoriginaluser: App als normaler Benutzer starten (nicht vererbt „Als Administrator“ vom Setup).
Filename: "{app}\{#MyAppExeName}"; WorkingDir: "{app}"; Description: "{cm:LaunchProgram,{#StringChange(MyAppName, '&', '&&')}}"; Flags: nowait postinstall skipifsilent runasoriginaluser
