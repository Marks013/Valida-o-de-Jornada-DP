@echo off
setlocal enabledelayedexpansion

echo ===============================================
echo  BUILD COMPLETO - Validador de Jornada DP
echo  x64 + x86
echo ===============================================

:: Definir ROOT do projeto
set "ROOT_DIR=%~dp0.."
cd /d "%ROOT_DIR%\src\ValidadorJornada"

:: Ler versao do .csproj
for /f %%v in ('powershell -ExecutionPolicy Bypass -File "%ROOT_DIR%\build\get_version.ps1"') do set VERSION=%%v

echo Versao: %VERSION%
echo Root: %ROOT_DIR%
echo.

if "%VERSION%"=="" (
    echo ERRO: Versao nao encontrada no .csproj
    pause
    exit /b 1
)

:: Certificado HTTPS
echo [0/10] Configurando certificado HTTPS...
dotnet dev-certs https --trust >nul 2>&1

:: [1/10] Limpar
echo [1/10] Limpando builds anteriores...
dotnet clean --nologo -v q 2>nul
if exist "bin" rd /s /q "bin" 2>nul
if exist "obj" rd /s /q "obj" 2>nul

:: [2/10] Restaurar
echo [2/9] Restaurando dependencias...
dotnet restore --nologo -v q
if errorlevel 1 (
    echo ERRO: Falha ao restaurar dependencias
    pause
    exit /b 1
)

:: [3/10] Compilar x64
echo [3/10] Compilando x64...
dotnet publish -c Release -r win-x64 --self-contained true ^
    /p:PublishSingleFile=false /p:PublishTrimmed=false --nologo -v q

if errorlevel 1 (
    echo ERRO: Compilacao x64 falhou
    pause
    exit /b 1
)

:: [4/10] Compilar x86
echo [4/10] Compilando x86...
dotnet publish -c Release -r win-x86 --self-contained true ^
    /p:PublishSingleFile=false /p:PublishTrimmed=false --nologo -v q

if errorlevel 1 (
    echo ERRO: Compilacao x86 falhou
    pause
    exit /b 1
)

:: [5/10] Assinar x64
echo [5/10] Assinando x64...
set "EXE_X64=bin\Release\net8.0-windows\win-x64\publish\ValidadorJornada.exe"
if exist "%EXE_X64%" (
    powershell -ExecutionPolicy Bypass -File "%ROOT_DIR%\build\sign.ps1" -ExePath "%CD%\%EXE_X64%" >nul 2>&1
    if errorlevel 1 echo AVISO: Assinatura x64 falhou
)

:: [6/10] Assinar x86
echo [6/10] Assinando x86...
set "EXE_X86=bin\Release\net8.0-windows\win-x86\publish\ValidadorJornada.exe"
if exist "%EXE_X86%" (
    powershell -ExecutionPolicy Bypass -File "%ROOT_DIR%\build\sign.ps1" -ExePath "%CD%\%EXE_X86%" >nul 2>&1
    if errorlevel 1 echo AVISO: Assinatura x86 falhou
)

:: [7/10] Copiar para releases
echo [7/10] Copiando para releases\%VERSION%\...
set "RELEASE_BASE=%ROOT_DIR%\releases\%VERSION%"
set "RELEASE_X64=%RELEASE_BASE%\x64"
set "RELEASE_X86=%RELEASE_BASE%\x86"

:: Remover versao antiga
if exist "%RELEASE_BASE%" (
    echo Removendo versao antiga...
    if exist "%RELEASE_BASE%.old" rd /s /q "%RELEASE_BASE%.old" 2>nul
    move "%RELEASE_BASE%" "%RELEASE_BASE%.old" >nul 2>&1
)

:: Criar pastas
if not exist "%RELEASE_X64%" mkdir "%RELEASE_X64%"
if not exist "%RELEASE_X86%" mkdir "%RELEASE_X86%"

:: Copiar x64
xcopy "bin\Release\net8.0-windows\win-x64\publish\*" "%RELEASE_X64%\" /E /Y /Q >nul
if errorlevel 1 (
    echo ERRO: Falha ao copiar arquivos x64
    pause
    exit /b 1
)

:: Copiar x86
xcopy "bin\Release\net8.0-windows\win-x86\publish\*" "%RELEASE_X86%\" /E /Y /Q >nul
if errorlevel 1 (
    echo ERRO: Falha ao copiar arquivos x86
    pause
    exit /b 1
)

:: Verificar executaveis
if not exist "%RELEASE_X64%\ValidadorJornada.exe" (
    echo ERRO: Executavel x64 nao copiado
    echo Path: %RELEASE_X64%
    pause
    exit /b 1
)

if not exist "%RELEASE_X86%\ValidadorJornada.exe" (
    echo ERRO: Executavel x86 nao copiado
    echo Path: %RELEASE_X86%
    pause
    exit /b 1
)

echo OK: Arquivos copiados
dir "%RELEASE_X64%\ValidadorJornada.exe" 2>nul | find "ValidadorJornada.exe"
dir "%RELEASE_X86%\ValidadorJornada.exe" 2>nul | find "ValidadorJornada.exe"

:: [8/10] Ofuscar com Obfuscar x64
echo [8/10] Ofuscando com Obfuscar x64...
set "PS_SCRIPT=%~dp0obfuscar_protect.ps1"

:: Verificar se DLL existe
if not exist "%RELEASE_X64%\ValidadorJornada.dll" (
    echo ERRO: ValidadorJornada.dll nao encontrado em %RELEASE_X64%
    echo.
    echo DIAGNOSTICO:
    dir "%RELEASE_X64%\*.dll" 2>nul | find "ValidadorJornada"
    pause
    exit /b 1
)

echo Ofuscando ValidadorJornada.dll...

powershell -ExecutionPolicy Bypass -File "%PS_SCRIPT%" ^
    -InputFile "%RELEASE_X64%\ValidadorJornada.dll" ^
    -OutputFile "%RELEASE_X64%\ValidadorJornada_protected.dll"

if errorlevel 1 (
    echo.
    echo ERRO: Ofuscacao x64 falhou
    echo.
    pause
    exit /b 1
)

:: Substituir DLL
del "%RELEASE_X64%\ValidadorJornada.dll"
ren "%RELEASE_X64%\ValidadorJornada_protected.dll" "ValidadorJornada.dll"

echo OK: DLL x64 ofuscada com sucesso

:: [8/10] Ofuscar com Obfuscar x86
echo [8/10] Ofuscando com Obfuscar x86...

if not exist "%RELEASE_X86%\ValidadorJornada.dll" (
    echo ERRO: ValidadorJornada.dll nao encontrado em %RELEASE_X86%
    pause
    exit /b 1
)

powershell -ExecutionPolicy Bypass -File "%PS_SCRIPT%" ^
    -InputFile "%RELEASE_X86%\ValidadorJornada.dll" ^
    -OutputFile "%RELEASE_X86%\ValidadorJornada_protected.dll"

if errorlevel 1 (
    echo ERRO: Ofuscacao x86 falhou
    pause
    exit /b 1
)

:: Substituir DLL
del "%RELEASE_X86%\ValidadorJornada.dll"
ren "%RELEASE_X86%\ValidadorJornada_protected.dll" "ValidadorJornada.dll"

echo OK: DLL x86 ofuscada com sucesso

:: [8/10] Limpar arquivos desnecessarios
echo [8/10] Limpar arquivos desnecessarios...
del "%RELEASE_X64%\*.pdb" /Q 2>nul
del "%RELEASE_X64%\*.xml" /Q 2>nul
del "%RELEASE_X64%\createdump.exe" /Q 2>nul
for /d %%d in ("%RELEASE_X64%\cs","%RELEASE_X64%\de","%RELEASE_X64%\es","%RELEASE_X64%\fr","%RELEASE_X64%\it","%RELEASE_X64%\ja","%RELEASE_X64%\ko","%RELEASE_X64%\pl","%RELEASE_X64%\ru","%RELEASE_X64%\tr","%RELEASE_X64%\zh-Hans","%RELEASE_X64%\zh-Hant") do rd /s /q "%%d" 2>nul
del "%RELEASE_X86%\*.pdb" /Q 2>nul
del "%RELEASE_X86%\*.xml" /Q 2>nul
del "%RELEASE_X86%\createdump.exe" /Q 2>nul
for /d %%d in ("%RELEASE_X86%\cs","%RELEASE_X86%\de","%RELEASE_X86%\es","%RELEASE_X86%\fr","%RELEASE_X86%\it","%RELEASE_X86%\ja","%RELEASE_X86%\ko","%RELEASE_X86%\pl","%RELEASE_X86%\ru","%RELEASE_X86%\tr","%RELEASE_X86%\zh-Hans","%RELEASE_X86%\zh-Hant") do rd /s /q "%%d" 2>nul

:: [9/10] Gerar checksums
echo [9/10] Gerando checksums...

:: Checksum x64
powershell -Command "$files = Get-ChildItem '%RELEASE_X64%' -Recurse -File; $output = @(); foreach ($f in $files) { $hash = (Get-FileHash $f.FullName -Algorithm SHA256).Hash; $rel = $f.FullName.Replace('%RELEASE_X64%\', ''); $output += \"$hash  $rel\" }; $output | Out-File '%RELEASE_X64%\checksums.sha256' -Encoding UTF8"

:: Checksum x86
powershell -Command "$files = Get-ChildItem '%RELEASE_X86%' -Recurse -File; $output = @(); foreach ($f in $files) { $hash = (Get-FileHash $f.FullName -Algorithm SHA256).Hash; $rel = $f.FullName.Replace('%RELEASE_X86%\', ''); $output += \"$hash  $rel\" }; $output | Out-File '%RELEASE_X86%\checksums.sha256' -Encoding UTF8"

:: Atualizar versao nos instaladores
echo.
echo Atualizando versoes nos instaladores...
powershell -ExecutionPolicy Bypass -File "%ROOT_DIR%\build\version.ps1"

:: Criar pasta Output
set "OUTPUT_DIR=%ROOT_DIR%\releases\Output"
if not exist "%OUTPUT_DIR%" mkdir "%OUTPUT_DIR%"

:: Compilar instalador
echo Compilando instalador...

if exist "C:\Program Files (x86)\Inno Setup 6\ISCC.exe" (
    "C:\Program Files (x86)\Inno Setup 6\ISCC.exe" "%ROOT_DIR%\build\installer_full.iss" /Q
    
    if errorlevel 1 (
        echo.
        echo ERRO: Falha ao compilar instalador
        pause
        exit /b 1
    )
    
    echo.
    echo ===============================================
    echo  BUILD CONCLUIDO COM SUCESSO!
    echo ===============================================
    echo.
    echo Versao: %VERSION%
    echo.
    echo Arquivos x64:
    dir "%RELEASE_X64%\ValidadorJornada.exe" 2>nul | find "ValidadorJornada.exe"
    echo.
    echo Arquivos x86:
    dir "%RELEASE_X86%\ValidadorJornada.exe" 2>nul | find "ValidadorJornada.exe"
    echo.
    echo Instalador:
    if exist "%OUTPUT_DIR%\ValidadorJornada_Setup_%VERSION%.exe" (
        echo   %OUTPUT_DIR%\ValidadorJornada_Setup_%VERSION%.exe
        dir "%OUTPUT_DIR%\ValidadorJornada_Setup_%VERSION%.exe" 2>nul | find ".exe"
    ) else (
        echo   ERRO: Instalador nao encontrado
    )
    echo.
    echo ===============================================
) else (
    echo.
    echo AVISO: Inno Setup nao encontrado
    echo Builds criados mas instalador nao foi gerado
    echo.
    echo ===============================================
    echo  BUILD CONCLUIDO (SEM INSTALADOR)
    echo ===============================================
    echo Arquivos disponiveis em:
    echo   x64: %RELEASE_X64%
    echo   x86: %RELEASE_X86%
    echo ===============================================
)

:: Limpar versao antiga se tudo ocorreu bem
if exist "%RELEASE_BASE%.old" (
    rd /s /q "%RELEASE_BASE%.old" 2>nul
)

)
:: Limpar pasta obj do build.
if exist "%ROOT_DIR%\src\ValidadorJornada\obj" (
    rd /s /q "%ROOT_DIR%\src\ValidadorJornada\obj" 2>nul
)

)
:: Limpar pasta bin do build.
if exist "%ROOT_DIR%\src\ValidadorJornada\bin" (
    rd /s /q "%ROOT_DIR%\src\ValidadorJornada\bin" 2>nul
)
pause