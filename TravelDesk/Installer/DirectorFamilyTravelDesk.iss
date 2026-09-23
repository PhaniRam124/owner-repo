#define MyAppName "Director Family Travel Desk"
#define MyAppVersion "1.1.0"
#define MyAppPublisher "CPRD"
#define MyAppExeName "DirectorFamilyTravelDesk.exe"

[Setup]
AppId={{D6D1D4F7-4518-4B9C-93E1-1B72E71A7A71}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
DefaultDirName={autopf}\Director Family Travel Desk
DefaultGroupName={#MyAppName}
OutputDir=output
OutputBaseFilename=DirectorFamilyTravelDesk_Setup_v1.1.0
Compression=lzma2
SolidCompression=yes
WizardStyle=modern
PrivilegesRequired=lowest
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
UninstallDisplayIcon={app}\{#MyAppExeName}

[Files]
Source: "..\publish\win-x64\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Tasks]
Name: "desktopicon"; Description: "Create a desktop shortcut"; GroupDescription: "Additional icons:"

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "Launch {#MyAppName}"; Flags: nowait postinstall skipifsilent