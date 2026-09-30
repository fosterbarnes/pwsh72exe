#ifndef AppVersion
#define AppVersion "0.1.0"
#endif
#ifndef AppName
#define AppName "pwsh72exe"
#endif
#ifndef AppExeName
#define AppExeName "pwsh72exe.Gui.exe"
#endif
#ifndef AppPublisher
#define AppPublisher "fosterbarnes"
#endif
#ifndef AppURL
#define AppURL "https://github.com/fosterbarnes/pwsh72exe"
#endif
#ifndef SetupIconFile
#define SetupIconFile "..\.res\icon\icon.ico"
#endif
#ifndef WizardImageFile
#define WizardImageFile "..\.res\icon\installer-wizard-large.png"
#endif
#ifndef WizardSmallImageFile
#define WizardSmallImageFile "..\.res\icon\installer-wizard-small.png"
#endif
#ifndef LicenseFile
#define LicenseFile "..\LICENSE"
#endif

[Setup]
AppName={#AppName}
AppVersion={#AppVersion}
AppPublisher={#AppPublisher}
AppPublisherURL={#AppURL}
AppSupportURL={#AppURL}
UninstallDisplayIcon={app}\{#AppExeName}
WizardStyle=modern dark
WizardBackColor=#1B1B1B
WizardImageBackColor=#1B1B1B
WizardSmallImageBackColor=#1B1B1B
SetupIconFile={#SetupIconFile}
WizardImageFile={#WizardImageFile}
WizardSmallImageFile={#WizardSmallImageFile}
LicenseFile={#LicenseFile}
DisableWelcomePage=no
DisableProgramGroupPage=yes
PrivilegesRequired=lowest
ChangesEnvironment=yes

[Tasks]
Name: startmenuicon; Description: "Create a Start Menu shortcut"; GroupDescription: "{cm:AdditionalIcons}"
Name: desktopicon; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"
Name: addtopath; Description: "Add pwsh72exe.Cli to PATH"; GroupDescription: "Command line:"; Flags: unchecked

[Icons]
Name: "{autoprograms}\{#AppName}"; Filename: "{app}\{#AppExeName}"; Tasks: startmenuicon
Name: "{autodesktop}\{#AppName}"; Filename: "{app}\{#AppExeName}"; Tasks: desktopicon

[Registry]
Root: HKCU; Subkey: "Environment"; ValueType: expandsz; ValueName: "Path"; ValueData: "{olddata};{app}"; Tasks: addtopath; Check: NeedsAddPath(ExpandConstant('{app}'))

[Run]
Filename: "{app}\{#AppExeName}"; Description: "{cm:LaunchProgram,{#AppName}}"; Flags: nowait postinstall skipifsilent

[Code]
procedure InitializeWizard;
begin
  WizardForm.LicenseAcceptedRadio.Checked := True;
end;

function NeedsAddPath(param: string): Boolean;
var
  path: string;
begin
  if not RegQueryStringValue(HKCU, 'Environment', 'Path', path) then
  begin
    Result := True;
    Exit;
  end;
  Result := Pos(';' + Lowercase(param) + ';', ';' + Lowercase(path) + ';') = 0;
end;

procedure CurUninstallStepChanged(step: TUninstallStep);
var
  path: string;
begin
  if step <> usPostUninstall then Exit;
  if not RegQueryStringValue(HKCU, 'Environment', 'Path', path) then Exit;
  path := ';' + path + ';';
  StringChangeEx(path, ';' + ExpandConstant('{app}') + ';', ';', True);
  if (Length(path) > 0) and (path[1] = ';') then Delete(path, 1, 1);
  if (Length(path) > 0) and (path[Length(path)] = ';') then Delete(path, Length(path), 1);
  RegWriteExpandStringValue(HKCU, 'Environment', 'Path', path);
end;
