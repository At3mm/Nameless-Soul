#ifndef GameSource
  #error GameSource must point to the completed Windows player directory.
#endif
#ifndef InstallerOutput
  #error InstallerOutput must point to the output directory.
#endif
#ifndef AppVersion
  #define AppVersion "0.1.0"
#endif

[Setup]
AppId={{2D1FB9B4-6F78-47BA-918D-982FA8246569}
AppName=Nameless Soul
AppVersion={#AppVersion}
AppPublisher=Nameless Soul
DefaultDirName={localappdata}\Programs\Nameless Soul
DefaultGroupName=Nameless Soul
PrivilegesRequired=lowest
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
OutputDir={#InstallerOutput}
OutputBaseFilename=Nameless-Soul-{#AppVersion}-Windows-Setup
Compression=lzma2
SolidCompression=yes
WizardStyle=modern
UninstallDisplayName=Nameless Soul
UninstallDisplayIcon={app}\NamelessSoul.exe
DisableProgramGroupPage=yes
CloseApplications=yes
SetupLogging=yes

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "Create a desktop shortcut"; GroupDescription: "Shortcuts:"; Flags: unchecked

[Files]
Source: "{#GameSource}\*"; DestDir: "{app}"; Excludes: "*.pdb,*.log,*_BurstDebugInformation_DoNotShip\*,*_BackUpThisFolder_ButDontShipItWithYourGame\*"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\Nameless Soul"; Filename: "{app}\NamelessSoul.exe"; WorkingDir: "{app}"
Name: "{autodesktop}\Nameless Soul"; Filename: "{app}\NamelessSoul.exe"; WorkingDir: "{app}"; Tasks: desktopicon

[Run]
Filename: "{app}\NamelessSoul.exe"; Description: "Launch Nameless Soul"; Flags: nowait postinstall skipifsilent
