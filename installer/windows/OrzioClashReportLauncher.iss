; Orzio Clash Report Desktop - Windows installer
;
; Installs per user by default, into {localappdata}\Programs\Orzio\ClashReportLauncher, with no
; administrator rights. A machine-wide install exists only as an explicit fallback for environments
; where AppLocker blocks execution from %LOCALAPPDATA%; it is never the default.
;
; The uninstaller removes what this installer created and nothing else. A user's exports, manifests,
; snapshots, run indexes, project catalogs, governance documents and reports are their own files, in
; their own folders, and are never touched.

#define LauncherName "Orzio Clash Report"
#define LauncherVersion "0.2.0"
#define LauncherFullVersion "0.2.0-launcher-preview.1"
#define LauncherPublisher "Orzio"
#define LauncherExeName "OrzioClashReport.Launcher.Desktop.exe"

#ifndef StagingDir
  #define StagingDir "..\..\artifacts\launcher\staging"
#endif

#ifndef OutputDir
  #define OutputDir "..\..\artifacts\launcher\installer"
#endif

; Requires Inno Setup 6.3 or later for ArchitecturesAllowed=x64compatible.

[Setup]
AppId={{7F0B4E2C-6A4E-4B3D-9E1A-2C7D5A9F1B84}
AppName={#LauncherName}
AppVersion={#LauncherVersion}
AppVerName={#LauncherName} {#LauncherFullVersion}
VersionInfoVersion={#LauncherVersion}
AppPublisher={#LauncherPublisher}

; {autopf} resolves to {localappdata}\Programs in the default per-user mode, and only becomes
; Program Files when the user deliberately elevates.
DefaultDirName={autopf}\Orzio\ClashReportLauncher
DefaultGroupName=Orzio
DisableProgramGroupPage=yes

; The application never needs elevation: it writes only to the user's own local application data.
PrivilegesRequired=lowest
PrivilegesRequiredOverridesAllowed=dialog

ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible

OutputDir={#OutputDir}
OutputBaseFilename=orzio-clash-report-desktop-v{#LauncherFullVersion}-win-x64-setup
Compression=lzma2
SolidCompression=yes
WizardStyle=modern
UninstallDisplayName={#LauncherName} {#LauncherFullVersion}
UninstallDisplayIcon={app}\{#LauncherExeName}

; This build is not code signed. That is a deliberate, documented state, not an oversight:
; SmartScreen will warn on first run, and the published SHA-256 is how the download is verified.

[Languages]
Name: "portuguese"; MessagesFile: "compiler:Languages\Portuguese.isl"
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
; Unchecked by default: a desktop shortcut is an opinion about someone else's desktop.
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked

[Files]
Source: "{#StagingDir}\{#LauncherExeName}"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#StagingDir}\*"; DestDir: "{app}"; Excludes: "*.pdb,*.tmp"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\{#LauncherName}"; Filename: "{app}\{#LauncherExeName}"
Name: "{autodesktop}\{#LauncherName}"; Filename: "{app}\{#LauncherExeName}"; Tasks: desktopicon

[UninstallDelete]
; Only what the installer laid down. Everything here lives under {app}.
Type: filesandordirs; Name: "{app}\engine"
Type: filesandordirs; Name: "{app}\samples"
Type: filesandordirs; Name: "{app}\docs"
Type: dirifempty; Name: "{app}"

[Code]
{
  Uninstall never removes the user's own data by default. It offers once, with No preselected, and
  removes only the launcher's own local state: settings, the recent list, logs, job journals and
  diagnostic bundles. Project files, reports and exports are never in scope.
}
procedure CurUninstallStepChanged(CurUninstallStep: TUninstallStep);
var
  LocalDataDir: string;
begin
  if CurUninstallStep = usPostUninstall then
  begin
    LocalDataDir := ExpandConstant('{localappdata}\Orzio\ClashReportLauncher');

    if DirExists(LocalDataDir) then
    begin
      if SuppressibleMsgBox(
           'Apagar também as definições, a lista de recentes e os registos locais desta aplicação?'
           + #13#10#13#10
           + 'Os seus projetos, snapshots e relatórios NÃO são apagados.'
           + #13#10
           + 'Se responder Não, estes dados ficam guardados para uma futura reinstalação.',
           mbConfirmation, MB_YESNO, IDNO) = IDYES then
      begin
        DelTree(LocalDataDir, True, True, True);
      end;
    end;
  end;
end;
