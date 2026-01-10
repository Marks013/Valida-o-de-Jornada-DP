#define MyAppName "Validador de Jornada DP"
#define MyAppVersion "5.1.2"
#define MyAppPublisher "Samuel Fernandes - DP"
#define MyAppExeName "ValidadorJornada.exe"
#define MyAppPassword "U35mOyNY"

[Setup]
AppId={{8F7A9B2C-3D4E-5F6A-7B8C-9D0E1F2A3B4C}
AppName={#MyAppName} (Patch)
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
DefaultDirName={code:GetInstallDir}
OutputDir={#SourcePath}..\releases\Output
OutputBaseFilename=ValidadorJornada_Patch_{#MyAppVersion}
SetupIconFile={#SourcePath}..\src\ValidadorJornada\Resources\icon.ico

; *** CRIPTOGRAFIA (sem senha de extração) ***
Encryption=yes
Password={#MyAppPassword}

; Compressão máxima
Compression=lzma2/ultra64
SolidCompression=yes
InternalCompressLevel=ultra64

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

[CustomMessages]
brazilianportuguese.PasswordPrompt=Digite a senha de atualização:
brazilianportuguese.InvalidPassword=Senha incorreta! A atualização será cancelada.
brazilianportuguese.PasswordTitle=Autenticação Necessária
brazilianportuguese.PasswordDescription=Este patch é protegido por senha.%n%nContate o administrador do sistema para obter a senha.
brazilianportuguese.PatchInfo=Atualização de Patch
brazilianportuguese.VersionCheck=Verificando versão instalada...

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
  PasswordPage: TInputQueryWizardPage;
  InstallPassword: String;
  MaxAttempts: Integer;
  CurrentAttempt: Integer;

// Senha de atualização (deve ser a mesma do instalador completo)
const
  INSTALL_PASSWORD = 'U35mOyNY';

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

function GetInstallDir(Param: String): String;
begin
  if InstallPath <> '' then
    Result := InstallPath
  else
    Result := ExpandConstant('{autopf}\ValidadorJornada');
end;

procedure InitializeWizard;
begin
  MaxAttempts := 3;
  CurrentAttempt := 0;
  
  // Criar página de senha (após verificação de versão)
  PasswordPage := CreateInputQueryPage(wpWelcome,
    ExpandConstant('{cm:PasswordTitle}'),
    ExpandConstant('{cm:PasswordDescription}'),
    ExpandConstant('{cm:PatchInfo}'));
  
  PasswordPage.Add(ExpandConstant('{cm:PasswordPrompt}'), True);
end;

function ShouldSkipPage(PageID: Integer): Boolean;
begin
  Result := False;
  
  // Não pular a página de senha
  if PageID = PasswordPage.ID then
    Result := False;
end;

function NextButtonClick(CurPageID: Integer): Boolean;
var
  ErrorMsg: String;
begin
  Result := True;
  
  if CurPageID = PasswordPage.ID then
  begin
    InstallPassword := PasswordPage.Values[0];
    
    // Verificar senha
    if InstallPassword <> INSTALL_PASSWORD then
    begin
      CurrentAttempt := CurrentAttempt + 1;
      
      if CurrentAttempt >= MaxAttempts then
      begin
        MsgBox(ExpandConstant('{cm:InvalidPassword}') + #13#10 + 
               'Tentativas esgotadas (' + IntToStr(MaxAttempts) + ').',
               mbError, MB_OK);
        Result := False;
        WizardForm.Close;
      end
      else
      begin
        ErrorMsg := 'Senha incorreta!' + #13#10 + 
                    'Tentativa ' + IntToStr(CurrentAttempt) + ' de ' + IntToStr(MaxAttempts);
        MsgBox(ErrorMsg, mbError, MB_OK);
        Result := False;
        PasswordPage.Values[0] := '';
      end;
    end
    else
    begin
      // Senha correta
      CurrentAttempt := 0;
    end;
  end;
end;

function InitializeSetup(): Boolean;
var
  InstalledVersion, BaseVersion, ManifestPath: String;
begin
  Result := False;
  
  // Verificar se aplicativo está instalado
  if not GetInstallationInfo(InstalledVersion, InstallPath) then
  begin
    MsgBox('Aplicativo não encontrado.' + #13#10 + 
           'Instale a versão completa primeiro.', mbError, MB_OK);
    Exit;
  end;
  
  // Extrair e verificar manifest
  ExtractTemporaryFile('manifest.json');
  ManifestPath := ExpandConstant('{tmp}\manifest.json');
  BaseVersion := LoadJsonValue(ManifestPath, 'baseVersion');
  
  if BaseVersion = '' then
  begin
    MsgBox('Manifesto do patch inválido.', mbError, MB_OK);
    Exit;
  end;
  
  // Verificar compatibilidade de versão
  if (InstalledVersion <> 'unknown') and (CompareStr(InstalledVersion, BaseVersion) <> 0) then
  begin
    MsgBox('Versão instalada incompatível!' + #13#10 + #13#10 +
           'Instalada: ' + InstalledVersion + #13#10 +
           'Requerida: ' + BaseVersion + #13#10 + #13#10 +
           'Instale a versão ' + BaseVersion + ' antes deste patch.',
           mbError, MB_OK);
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
  
  // Remover backup antigo se existir
  if DirExists(BackupPath) then
    DelTree(BackupPath, True, True, True);
    
  // Criar backup da instalação atual
  if DirExists(InstallPath) then
  begin
    CreateDir(BackupPath);
    Exec('xcopy', '"' + InstallPath + '" "' + BackupPath + '" /E /I /Y /Q', '', 
         SW_HIDE, ewWaitUntilTerminated, ResultCode);
         
    if ResultCode <> 0 then
    begin
      Result := 'Falha ao criar backup da instalação atual.' + #13#10 +
                'Código: ' + IntToStr(ResultCode);
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
    
    // Smoke test: verificar se executável existe
    if not FileExists(ExePath) then
    begin
      MsgBox('Instalação do patch falhou!' + #13#10 +
             'Executável não encontrado.' + #13#10 + #13#10 +
             'Revertendo para versão anterior...',
             mbError, MB_OK);
      
      // Rollback automático
      if DirExists(BackupPath) then
      begin
        DelTree(InstallPath, True, True, True);
        Exec('xcopy', '"' + BackupPath + '" "' + InstallPath + '" /E /I /Y /Q', '', 
             SW_HIDE, ewWaitUntilTerminated, ResultCode);
        
        if ResultCode = 0 then
          MsgBox('Rollback concluído com sucesso.' + #13#10 +
                 'Versão anterior restaurada.',
                 mbInformation, MB_OK)
        else
          MsgBox('ERRO: Falha no rollback!' + #13#10 +
                 'Backup disponível em: ' + BackupPath,
                 mbError, MB_OK);
      end;
    end
    else
    begin
      // Patch instalado com sucesso
      MsgBox('Patch instalado com sucesso!' + #13#10 + #13#10 +
             'Versão: {#MyAppVersion}' + #13#10 +
             'Proteções: DLL ofuscada + criptografia',
             mbInformation, MB_OK);
    end;
  end;
end;

procedure DeinitializeSetup();
begin
  // Limpar backup após instalação bem-sucedida
  if DirExists(BackupPath) then
    DelTree(BackupPath, True, True, True);
end;
