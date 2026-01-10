@echo off
setlocal enabledelayedexpansion

echo ===============================================
echo  BUILD PATCH - Validador de Jornada DP
echo ===============================================

set "ROOT_DIR=%~dp0.."
cd /d "%ROOT_DIR%\src\ValidadorJornada"

:: Ler versao nova
for /f %%v in ('powershell -ExecutionPolicy Bypass -File "%ROOT_DIR%\build\get_version.ps1"') do set NEW_VERSION=%%v

:: Auto-detectar versao base
for /f %%v in ('powershell -ExecutionPolicy Bypass -File "%ROOT_DIR%\build\get_last_version.ps1"') do set DETECTED_VERSION=%%v

if not "%DETECTED_VERSION%"=="" (
    echo Versao base detectada: %DETECTED_VERSION%
    set /p BASE_VERSION="Confirme [Enter] ou digite outra: "
    if "!BASE_VERSION!"=="" set BASE_VERSION=%DETECTED_VERSION%
) else (
    set /p BASE_VERSION="Versao base: "
)

echo.
echo Base: %BASE_VERSION% ^| Nova: %NEW_VERSION%
echo.

if "%NEW_VERSION%"=="" (
    echo ERRO: Versao nao encontrada
    pause
    exit /b 1
)

if "%BASE_VERSION%"=="" (
    echo ERRO: Versao base obrigatoria
    pause
    exit /b 1
)

:: Verificar se versao base existe
if not exist "%ROOT_DIR%\releases\%BASE_VERSION%\x64" (
    echo ERRO: Versao base %BASE_VERSION% nao encontrada
    echo Verifique se a pasta %ROOT_DIR%\releases\%BASE_VERSION% existe
    pause
    exit /b 1
)

:: [1/7] Compilar
echo [1/7] Compilando x64 e x86...
dotnet publish -c Release -r win-x64 --self-contained true /p:PublishSingleFile=false /p:PublishTrimmed=false --nologo -v q
if errorlevel 1 (
    echo ERRO: Compilacao x64 falhou
    pause
    exit /b 1
)

dotnet publish -c Release -r win-x86 --self-contained true /p:PublishSingleFile=false /p:PublishTrimmed=false --nologo -v q
if errorlevel 1 (
    echo ERRO: Compilacao x86 falhou
    pause
    exit /b 1
)

:: [2/7] Ofuscar x64
echo [2/7] Ofuscando x64...
set "PS_SCRIPT=%ROOT_DIR%\build\obfuscar_protect.ps1"
set "BUILD_X64=%CD%\bin\Release\net8.0-windows\win-x64\publish"
set "BUILD_X86=%CD%\bin\Release\net8.0-windows\win-x86\publish"

if exist "%BUILD_X64%\ValidadorJornada.dll" (
    echo Ofuscando ValidadorJornada.dll x64...
    
    powershell -ExecutionPolicy Bypass -File "%PS_SCRIPT%" ^
        -InputFile "%BUILD_X64%\ValidadorJornada.dll" ^
        -OutputFile "%BUILD_X64%\ValidadorJornada_protected.dll"
    
    if errorlevel 1 (
        echo ERRO: Ofuscacao x64 falhou
        pause
        exit /b 1
    )
    
    del "%BUILD_X64%\ValidadorJornada.dll"
    ren "%BUILD_X64%\ValidadorJornada_protected.dll" "ValidadorJornada.dll"
    echo OK: DLL x64 ofuscada
) else (
    echo AVISO: ValidadorJornada.dll x64 nao encontrado
)

:: [3/7] Ofuscar x86
echo [3/7] Ofuscando x86...

if exist "%BUILD_X86%\ValidadorJornada.dll" (
    echo Ofuscando ValidadorJornada.dll x86...
    
    powershell -ExecutionPolicy Bypass -File "%PS_SCRIPT%" ^
        -InputFile "%BUILD_X86%\ValidadorJornada.dll" ^
        -OutputFile "%BUILD_X86%\ValidadorJornada_protected.dll"
    
    if errorlevel 1 (
        echo ERRO: Ofuscacao x86 falhou
        pause
        exit /b 1
    )
    
    del "%BUILD_X86%\ValidadorJornada.dll"
    ren "%BUILD_X86%\ValidadorJornada_protected.dll" "ValidadorJornada.dll"
    echo OK: DLL x86 ofuscada
) else (
    echo AVISO: ValidadorJornada.dll x86 nao encontrado
)

:: [4/7] Limpar arquivos desnecessarios antes de criar patch
echo [4/7] Limpando arquivos desnecessarios...
for /d %%d in ("%BUILD_X64%\cs","%BUILD_X64%\de","%BUILD_X64%\es","%BUILD_X64%\fr","%BUILD_X64%\it","%BUILD_X64%\ja","%BUILD_X64%\ko","%BUILD_X64%\pl","%BUILD_X64%\pt-BR","%BUILD_X64%\ru","%BUILD_X64%\tr","%BUILD_X64%\zh-Hans","%BUILD_X64%\zh-Hant") do rd /s /q "%%d" 2>nul
del "%BUILD_X64%\*.pdb" /Q 2>nul
del "%BUILD_X64%\*.xml" /Q 2>nul
del "%BUILD_X64%\createdump.exe" /Q 2>nul

for /d %%d in ("%BUILD_X86%\cs","%BUILD_X86%\de","%BUILD_X86%\es","%BUILD_X86%\fr","%BUILD_X86%\it","%BUILD_X86%\ja","%BUILD_X86%\ko","%BUILD_X86%\pl","%BUILD_X86%\pt-BR","%BUILD_X86%\ru","%BUILD_X86%\tr","%BUILD_X86%\zh-Hans","%BUILD_X86%\zh-Hant") do rd /s /q "%%d" 2>nul
del "%BUILD_X86%\*.pdb" /Q 2>nul
del "%BUILD_X86%\*.xml" /Q 2>nul
del "%BUILD_X86%\createdump.exe" /Q 2>nul

:: [5/7] Criar patch
echo [5/7] Criando patch...
echo Comparando arquivos por conteudo...
set "PATCH_DIR=%ROOT_DIR%\releases\patch_%NEW_VERSION%"

if not exist "%ROOT_DIR%\build\create_patch.ps1" (
    echo ERRO: Script create_patch.ps1 nao encontrado
    pause
    exit /b 1
)

powershell -ExecutionPolicy Bypass -File "%ROOT_DIR%\build\create_patch.ps1" ^
    -BaseVersion "%BASE_VERSION%" ^
    -NewVersion "%NEW_VERSION%" ^
    -BasePath "%ROOT_DIR%\releases\%BASE_VERSION%" ^
    -NewPathX64 "%BUILD_X64%" ^
    -NewPathX86 "%BUILD_X86%" ^
    -OutputPath "%PATCH_DIR%"

if errorlevel 1 (
    echo ERRO: Falha ao criar patch
    pause
    exit /b 1
)

:: Verificar se patch foi criado
if not exist "%PATCH_DIR%\manifest.json" (
    echo AVISO: Nenhum arquivo modificado encontrado
    echo Versoes sao identicas
    pause
    exit /b 0
)

:: [6/7] Assinar executaveis
echo [6/7] Assinando executaveis...
for %%f in ("%PATCH_DIR%\x64\*.exe" "%PATCH_DIR%\x86\*.exe") do (
    if exist "%%f" (
        powershell -ExecutionPolicy Bypass -File "%ROOT_DIR%\build\sign.ps1" -ExePath "%%f" >nul 2>&1
        if errorlevel 1 echo AVISO: Falha ao assinar %%f
    )
)

:: [7/7] Gerar instalador
echo [7/7] Gerando instalador...
powershell -ExecutionPolicy Bypass -File "%ROOT_DIR%\build\version.ps1"

set "OUTPUT_DIR=%ROOT_DIR%\releases\Output"
if not exist "%OUTPUT_DIR%" mkdir "%OUTPUT_DIR%"

if exist "C:\Program Files (x86)\Inno Setup 6\ISCC.exe" (
    "C:\Program Files (x86)\Inno Setup 6\ISCC.exe" "%ROOT_DIR%\build\installer_patch.iss" /Q
    
    if errorlevel 1 (
        echo ERRO: Falha ao compilar instalador
        pause
        exit /b 1
    )
    
    echo.
    echo ===============================================
    echo  PATCH CONCLUIDO COM SUCESSO
    echo ===============================================
    echo Versao: %BASE_VERSION% -^> %NEW_VERSION%
    echo.
    echo Protecoes aplicadas:
    echo   - DLL x64 ofuscada
    echo   - DLL x86 ofuscada
    echo   - Instalador com senha
    echo.
    set "PATCH_FILE=%OUTPUT_DIR%\ValidadorJornada_Patch_%NEW_VERSION%.exe"
    if exist "!PATCH_FILE!" (
        echo Instalador: !PATCH_FILE!
        dir "!PATCH_FILE!" 2>nul | find ".exe"
    )
    echo.
    echo SENHA DO INSTALADOR: DP2026@Secure
    echo (ou a senha customizada definida no installer_patch.iss)
    echo ===============================================
) else (
    echo.
    echo ===============================================
    echo  PATCH CRIADO (SEM INSTALADOR)
    echo ===============================================
    echo Patch disponivel em: %PATCH_DIR%
    echo Protecoes aplicadas:
    echo   - DLL x64 ofuscada
    echo   - DLL x86 ofuscada
    echo ===============================================
)

:: Limpar pasta obj do build
if exist "%ROOT_DIR%\src\ValidadorJornada\obj" (
    rd /s /q "%ROOT_DIR%\src\ValidadorJornada\obj" 2>nul
)

:: Limpar pasta bin do build
if exist "%ROOT_DIR%\src\ValidadorJornada\bin" (
    rd /s /q "%ROOT_DIR%\src\ValidadorJornada\bin" 2>nul
)

pause