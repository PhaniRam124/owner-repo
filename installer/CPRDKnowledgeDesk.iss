#define MyAppName "CPRD Knowledge Desk"
#define MyAppVersion "1.0.0-beta2"
#define MyAppPublisher "CPRD"
#define MyAppExeName "CPRD.KnowledgeDesk.App.exe"

[Setup]
AppId={{9C6BFE2E-2789-4CF1-8AA9-CA4E8A6E4B70}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
DefaultDirName={localappdata}\Programs\CPRD Knowledge Desk
DefaultGroupName=CPRD Knowledge Desk
DisableProgramGroupPage=yes
OutputDir=..\artifacts\release
OutputBaseFilename=CPRD-Knowledge-Desk-Setup-1.0.0-beta2
Compression=lzma2
SolidCompression=yes
WizardStyle=modern
PrivilegesRequired=lowest
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
UninstallDisplayIcon={app}\{#MyAppExeName}
SetupLogging=yes

[Files]
Source: "..\artifacts\publish\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\CPRD Knowledge Desk"; Filename: "{app}\{#MyAppExeName}"
Name: "{userdesktop}\CPRD Knowledge Desk"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Tasks]
Name: "desktopicon"; Description: "Create a desktop shortcut"; GroupDescription: "Additional shortcuts:"; Flags: unchecked

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "Launch CPRD Knowledge Desk"; Flags: nowait postinstall skipifsilent
