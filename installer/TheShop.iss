[Setup]
AppId=TheShop.RHabenicht
AppName=The Shop
AppVersion=0.2.13
DefaultDirName={localappdata}\Programs\The Shop
DefaultGroupName=The Shop
PrivilegesRequired=lowest
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
MinVersion=10.0
OutputDir=..\installer-output
OutputBaseFilename=The-Shop-Setup-x64
Compression=lzma2
SolidCompression=yes
WizardStyle=modern
UninstallDisplayIcon={app}\TheShop.Desktop.exe

[Files]
Source: "..\publish\*"; DestDir: "{app}"; Excludes: "*.pdb,TheShop.Design.exe,TheShop.Design.deps.json,TheShop.Design.runtimeconfig.json"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\The Shop"; Filename: "{app}\TheShop.Desktop.exe"
