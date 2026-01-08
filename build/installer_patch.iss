#define MyAppName "Validador de Jornada DP"
#define MyAppVersion "4.1.2"
#define MyAppPublisher "Samuel Fernandes - DP"
#define MyAppExeName "ValidadorJornada.exe"

[Setup]
AppId={{8F7A9B2C-3D4E-5F6A-7B8C-9D0E1F2A3B4C}
AppName={#MyAppName} (Patch)
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
DefaultDirName={code:GetInstallDir}
OutputDir={#SourcePath}..\releases\Output
OutputBaseFilename=ValidadorJornada_Patch_{#MyAppVersion}
SetupIconFile={#SourcePath}..\src\ValidadorJornada\Resources\icon.ico
Compression=lzma2/ultra64
SolidCompression=yes
WizardStyle=modern
PrivilegesRequired=lowest
DisableWelcomePage=yes
DisableDirPage=yes
DisableProgramGroupPage=yes
ArchitecturesAllowed=x64compatible x86compatible
ArchitecturesInstallIn64BitMode=x64compatible
UninstallDisplayIcon={app}\{#MyAppExeName}

[Languages]
Name: "brazilianportuguese"; MessagesFile: "compiler:Languages\BrazilianPortuguese.isl"

[Files]
Source: "{#SourcePath}..\releases\patch_{#MyAppVersion}\x64\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs; Check: Is64BitInstallMode
Source: "{#SourcePath}..\releases\patch_{#MyAppVersion}\x86\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs; Check: not Is64BitInstallMode
Source: "{#SourcePath}..\releases\patch_{#MyAppVersion}\manifest.json"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#SourcePath}..\tools\RollbackHelper.ps1"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#SourcePath}..\tools\RollbackHelper.bat"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#SourcePath}..\Banco Horario.xlsx"; DestDir: "{app}"; Flags: ignoreversion

[Code]
var
  BackupPath: String;
  InstallPath: String;

function GetInstallationInfo(var Version, Path: String): Boolean;
var
  UninstallKey: String;
begin
  Result := False;
  UninstallKey := 'Software\Microsoft\Windows\CurrentVersion\Uninstall\{#SetupSetting("AppId")}_is1';
  
  if RegQueryStringValue(HKCU, UninstallKey, 'DisplayVersion', Version) and
     RegQueryStringValue(HKCU, UninstallKey, 'InstallLocation', Path) then
  begin
    Result := True;
    Exit;
  end;
  
  if RegQueryStringValue(HKLM, UninstallKey, 'DisplayVersion', Version) and
     RegQueryStringValue(HKLM, UninstallKey, 'InstallLocation', Path) then
  begin
    Result := True;
    Exit;
  end;
  
  Path := ExpandConstant('{autopf}\ValidadorJornada');
  if FileExists(Path + '\{#MyAppExeName}') then
  begin
    Version := 'unknown';
    Result := True;
  end;
end;

function StrPos(const SubStr, Str: String; Offset: Integer): Integer;
var
  I, MaxLen, SubStrLen: Integer;
begin
  Result := 0;
  SubStrLen := Length(SubStr);
  MaxLen := Length(Str) - SubStrLen + 1;
  
  for I := Offset to MaxLen do
  begin
    if Copy(Str, I, SubStrLen) = SubStr then
    begin
      Result := I;
      Exit;
    end;
  end;
end;

function LoadJsonValue(FilePath, Key: String): String;
var
  Lines: TArrayOfString;
  I, P, P2: Integer;
  Content, SearchKey: String;
begin
  Result := '';
  if LoadStringsFromFile(FilePath, Lines) then
  begin
    Content := '';
    for I := 0 to GetArrayLength(Lines) - 1 do
      Content := Content + Lines[I];
    
    SearchKey := '"' + Key + '"';
    P := Pos(SearchKey, Content);
    if P > 0 then
    begin
      P := P + Length(SearchKey);
      P := StrPos('"', Content, P) + 1;
      P2 := StrPos('"', Content, P);
      if (P > 0) and (P2 > 0) then
        Result := Copy(Content, P, P2 - P);
    end;
  end;
end;

function GetInstallDir(Param: String): String;
begin
  if InstallPath <> '' then
    Result := InstallPath
  else
    Result := ExpandConstant('{autopf}\ValidadorJornada');
end;

function InitializeSetup(): Boolean;
var
  InstalledVersion, BaseVersion, ManifestPath: String;
begin
  Result := False;
  
  if not GetInstallationInfo(InstalledVersion, InstallPath) then
  begin
    MsgBox('Aplicativo não encontrado. Instale a versão completa.', mbError, MB_OK);
    Exit;
  end;
  
  ExtractTemporaryFile('manifest.json');
  ManifestPath := ExpandConstant('{tmp}\manifest.json');
  BaseVersion := LoadJsonValue(ManifestPath, 'baseVersion');
  
  if BaseVersion = '' then
  begin
    MsgBox('Manifesto inválido.', mbError, MB_OK);
    Exit;
  end;
  
  if (InstalledVersion <> 'unknown') and (CompareStr(InstalledVersion, BaseVersion) <> 0) then
  begin
    MsgBox('Versão instalada (' + InstalledVersion + ') incompatível.' + #13#10 +
           'Requer: ' + BaseVersion, mbError, MB_OK);
    Exit;
  end;
  
  Result := True;
end;

function PrepareToInstall(var NeedsRestart: Boolean): String;
var
  ResultCode: Integer;
begin
  Result := '';
  
  if InstallPath = '' then
    InstallPath := ExpandConstant('{app}');
  
  BackupPath := InstallPath + '.backup';
  
  if DirExists(BackupPath) then
    DelTree(BackupPath, True, True, True);
    
  if DirExists(InstallPath) then
  begin
    CreateDir(BackupPath);
    Exec('xcopy', '"' + InstallPath + '" "' + BackupPath + '" /E /I /Y /Q', '', 
         SW_HIDE, ewWaitUntilTerminated, ResultCode);
         
    if ResultCode <> 0 then
    begin
      Result := 'Falha ao criar backup';
      Exit;
    end;
  end;
end;

procedure CurStepChanged(CurStep: TSetupStep);
var
  ResultCode: Integer;
  ExePath: String;
begin
  if CurStep = ssPostInstall then
  begin
    ExePath := ExpandConstant('{app}\{#MyAppExeName}');
    
    // Smoke test
    if not FileExists(ExePath) then
    begin
      MsgBox('Instalação falhou: executável não encontrado.' + #13#10 + 'Revertendo...', mbError, MB_OK);
      
      // Rollback
      if DirExists(BackupPath) then
      begin
        DelTree(InstallPath, True, True, True);
        Exec('xcopy', '"' + BackupPath + '" "' + InstallPath + '" /E /I /Y /Q', '', 
             SW_HIDE, ewWaitUntilTerminated, ResultCode);
        MsgBox('Rollback concluído.', mbInformation, MB_OK);
      end;
    end;
  end;
end;

procedure DeinitializeSetup();
begin
  if DirExists(BackupPath) then
    DelTree(BackupPath, True, True, True);
end;
