; Orzio Clash Report Desktop - Windows installer
;
; Installs per user, without administrator rights, into the user's own programs directory.
; A machine-wide install exists only as an explicit fallback for environments where AppLocker
; blocks execution from %LOCALAPPDATA%; it is not the normal path.
;
; The uninstaller removes the application, the engine, the shortcuts, the docs, and the samples.
; It never removes %LOCALAPPDATA%\Orzio\, and it never removes a coordinator's XML exports,
; manifests, snapshots, run indexes, project catalogs, governance documents, or reports.

#ifndef LauncherVersion
  #define LauncherVersion "0.2.0-launcher-preview.1"
#endif

#ifndef StagingDirectory
  #define StagingDirectory "..\..\artifacts\launcher\staging"
#endif

#ifndef OutputDirectory
  #define OutputDirectory "..\..\artifacts\launcher"
#endif

#define AppName "Orzio Clash Report"
#define AppPublisher "Orzio"
#define AppExeName "OrzioClashReport.Launcher.Desktop.exe"

[Setup]
AppId={{9E1B0B39-6E2A-4D57-9E1E-6C1B0E6A2B41}
AppName={#AppName}
AppVersion={#LauncherVersion}
AppVerName={#AppName} {#LauncherVersion}
AppPublisher={#AppPublisher}
VersionInfoVersion=0.2.0.0

; Per-user by default: no elevation, no administrator, no UAC prompt.
PrivilegesRequired=lowest
PrivilegesRequiredOverridesAllowed=dialog
DefaultDirName={localappdata}\Programs\Orzio\ClashReportLauncher
DefaultGroupName={#AppName}
DisableProgramGroupPage=yes
DisableDirPage=no
AllowNoIcons=yes

OutputDir={#OutputDirectory}
OutputBaseFilename=OrzioClashReportSetup-{#LauncherVersion}
Compression=lzma2/max
SolidCompression=yes
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
WizardStyle=modern
UninstallDisplayName={#AppName} {#LauncherVersion}
UninstallDisplayIcon={app}\{#AppExeName}

[Languages]
Name: "portuguese"; MessagesFile: "compiler:Languages\Portuguese.isl"
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
; Unchecked by default: a desktop shortcut is a choice, not an assumption.
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked

[Files]
Source: "{#StagingDirectory}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\{#AppName}"; Filename: "{app}\{#AppExeName}"
Name: "{group}\{cm:UninstallProgram,{#AppName}}"; Filename: "{uninstallexe}"
Name: "{autodesktop}\{#AppName}"; Filename: "{app}\{#AppExeName}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#AppExeName}"; Description: "{cm:LaunchProgram,{#AppName}}"; Flags: nowait postinstall skipifsilent

[UninstallDelete]
; Only what the installer itself put on disk.
Type: filesandordirs; Name: "{app}\engine"
Type: filesandordirs; Name: "{app}\samples"
Type: filesandordirs; Name: "{app}\docs"
Type: dirifempty; Name: "{app}"
Type: dirifempty; Name: "{localappdata}\Programs\Orzio"

[Code]
var
  RemoveLocalDataPage: TInputOptionWizardPage;

procedure InitializeWizard;
begin
  RemoveLocalDataPage := CreateInputOptionPage(
    wpSelectTasks,
    'Dados locais da aplicação',
    'O que fazer com as definições e os registos desta aplicação',
    'As suas definições, registos e estado de trabalhos ficam em' + #13#10 +
    '%LOCALAPPDATA%\Orzio\ClashReportLauncher.' + #13#10#13#10 +
    'Os seus projetos, exports, snapshots, índices e relatórios NUNCA são guardados aí e ' +
    'nunca são removidos por esta aplicação.',
    False, False);

  { Unchecked by default: nothing local is deleted unless the human asks for it. }
  RemoveLocalDataPage.Add('Apagar definições e registos locais ao desinstalar');
  RemoveLocalDataPage.Values[0] := False;
end;

procedure CurStepChanged(CurStep: TSetupStep);
begin
  if CurStep = ssPostInstall then
  begin
    if RemoveLocalDataPage.Values[0] then
      RegWriteStringValue(HKCU, 'Software\Orzio\ClashReportLauncher', 'RemoveLocalDataOnUninstall', '1')
    else
      RegWriteStringValue(HKCU, 'Software\Orzio\ClashReportLauncher', 'RemoveLocalDataOnUninstall', '0');
  end;
end;

procedure CurUninstallStepChanged(CurUninstallStep: TUninstallStep);
var
  RemoveLocalData: String;
begin
  if CurUninstallStep = usPostUninstall then
  begin
    { The local data directory is removed only when the human opted in at install time.
      Project data lives elsewhere and is never touched under any circumstance. }
    if RegQueryStringValue(HKCU, 'Software\Orzio\ClashReportLauncher',
                           'RemoveLocalDataOnUninstall', RemoveLocalData) then
    begin
      if RemoveLocalData = '1' then
        DelTree(ExpandConstant('{localappdata}\Orzio\ClashReportLauncher'), True, True, True);
    end;

    RegDeleteKeyIncludingSubkeys(HKCU, 'Software\Orzio\ClashReportLauncher');
  end;
end;
