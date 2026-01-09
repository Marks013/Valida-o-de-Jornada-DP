#define MyAppName "Validador de Jornada DP"
#define MyAppVersion "5.1.2"
#define MyAppPublisher "Samuel Fernandes - DP"
#define MyAppExeName "ValidadorJornada.exe"

[Setup]
AppId={{8F7A9B2C-3D4E-5F6A-7B8C-9D0E1F2A3B4C}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
DefaultDirName={autopf}\ValidadorJornada
DefaultGroupName=Validador de Jornada DP
AllowNoIcons=yes
OutputDir={#SourcePath}..\releases\Output
OutputBaseFilename=ValidadorJornada_Setup_{#MyAppVersion}
SetupIconFile={#SourcePath}..\src\ValidadorJornada\Resources\icon.ico
Compression=lzma2/ultra64
SolidCompression=yes
WizardStyle=modern
PrivilegesRequired=lowest
ArchitecturesAllowed=x64compatible x86compatible
ArchitecturesInstallIn64BitMode=x64compatible
UninstallDisplayIcon={app}\{#MyAppExeName}
DisableProgramGroupPage=yes
DisableWelcomePage=no

[Languages]
Name: "brazilianportuguese"; MessagesFile: "compiler:Languages\BrazilianPortuguese.isl"

[Tasks]
Name: "desktopicon"; Description: "Criar atalho na área de trabalho"; GroupDescription: "Atalhos:"

[Files]
; x64
Source: "{#SourcePath}..\releases\{#MyAppVersion}\x64\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs; Excludes: "checksums.sha256"; Check: Is64BitInstallMode

; x86
Source: "{#SourcePath}..\releases\{#MyAppVersion}\x86\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs; Excludes: "checksums.sha256"; Check: not Is64BitInstallMode
Source: "{#SourcePath}..\tools\RollbackHelper.ps1"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#SourcePath}..\tools\RollbackHelper.bat"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#SourcePath}..\Banco Horario.xlsx"; DestDir: "{app}"; Flags: ignoreversion

[Icons]
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "Executar {#MyAppName}"; Flags: nowait postinstall skipifsilent

[UninstallDelete]
Type: filesandordirs; Name: "{userappdata}\ValidadorJornada"

[Code]
function InitializeSetup(): Boolean;
var
  UninstallKey: String;
  Arch: String;
begin
  Result := True;
  
  if Is64BitInstallMode then
    Arch := 'x64'
  else
    Arch := 'x86';
  
  UninstallKey := 'Software\Microsoft\Windows\CurrentVersion\Uninstall\{#SetupSetting("AppId")}_is1';
  
  if RegKeyExists(HKEY_LOCAL_MACHINE, UninstallKey) or RegKeyExists(HKEY_CURRENT_USER, UninstallKey) then
  begin
    if MsgBox('Versão anterior detectada (' + Arch + '). Continuar?', mbConfirmation, MB_YESNO) = IDNO then
      Result := False;
  end;
end;

procedure CurStepChanged(CurStep: TSetupStep);
var
  AppDataPath: String;
begin
  if CurStep = ssPostInstall then
  begin
    AppDataPath := ExpandConstant('{userappdata}\ValidadorJornada');
    if not DirExists(AppDataPath) then
      CreateDir(AppDataPath);
  end;
end;

procedure CurUninstallStepChanged(CurUninstallStep: TUninstallStep);
var
  AppDataPath: String;
begin
  if CurUninstallStep = usPostUninstall then
  begin
    AppDataPath := ExpandConstant('{userappdata}\ValidadorJornada');
    if DirExists(AppDataPath) then
    begin
      if MsgBox('Remover dados (histórico/códigos)?', mbConfirmation, MB_YESNO or MB_DEFBUTTON2) = IDYES then
        DelTree(AppDataPath, True, True, True);
    end;
  end;
end;
