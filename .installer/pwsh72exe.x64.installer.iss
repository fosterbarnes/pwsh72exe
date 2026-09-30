#ifndef AppVersion
#define AppVersion "0.1.0"
#endif
[Setup]
AppId={{9EBDD321-C1AC-4881-8BBD-5220D11ABE1C}
DefaultDirName={userappdata}\pwsh72exe
OutputDir=Output
OutputBaseFilename=pwsh72exe-x64-installer
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
Uninstallable=yes
[Files]
Source: "..\publish\build\x64\*"; DestDir: "{app}"; Flags: recursesubdirs ignoreversion
#include "pwsh72exe.installer.iss"
