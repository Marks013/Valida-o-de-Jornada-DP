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

:: [1/5] Compilar
echo [1/5] Compilando x64 e x86...
dotnet publish -c Release -r win-x64 --self-contained true /p:PublishSingleFile=false --nologo -v q
if errorlevel 1 (
    echo ERRO: Compilacao x64 falhou
    pause
    exit /b 1
)

dotnet publish -c Release -r win-x86 --self-contained true /p:PublishSingleFile=false --nologo -v q
if errorlevel 1 (
    echo ERRO: Compilacao x86 falhou
    pause
    exit /b 1
)

:: [2/5] Criar patch
:: Limpar pastas de idiomas ANTES de criar patch
for /d %%d in ("%CD%\bin\Release\net8.0-windows\win-x64\publish\cs","%CD%\bin\Release\net8.0-windows\win-x64\publish\de","%CD%\bin\Release\net8.0-windows\win-x64\publish\es","%CD%\bin\Release\net8.0-windows\win-x64\publish\fr","%CD%\bin\Release\net8.0-windows\win-x64\publish\it","%CD%\bin\Release\net8.0-windows\win-x64\publish\ja","%CD%\bin\Release\net8.0-windows\win-x64\publish\ko","%CD%\bin\Release\net8.0-windows\win-x64\publish\pl","%CD%\bin\Release\net8.0-windows\win-x64\publish\pt-BR","%CD%\bin\Release\net8.0-windows\win-x64\publish\ru","%CD%\bin\Release\net8.0-windows\win-x64\publish\tr","%CD%\bin\Release\net8.0-windows\win-x64\publish\zh-Hans","%CD%\bin\Release\net8.0-windows\win-x64\publish\zh-Hant") do rd /s /q "%%d" 2>nul
del "%CD%\bin\Release\net8.0-windows\win-x64\publish\*.pdb" /Q 2>nul
del "%CD%\bin\Release\net8.0-windows\win-x64\publish\*.xml" /Q 2>nul
for /d %%d in ("%CD%\bin\Release\net8.0-windows\win-X86\publish\cs","%CD%\bin\Release\net8.0-windows\win-X86\publish\de","%CD%\bin\Release\net8.0-windows\win-X86\publish\es","%CD%\bin\Release\net8.0-windows\win-X86\publish\fr","%CD%\bin\Release\net8.0-windows\win-X86\publish\it","%CD%\bin\Release\net8.0-windows\win-X86\publish\ja","%CD%\bin\Release\net8.0-windows\win-X86\publish\ko","%CD%\bin\Release\net8.0-windows\win-X86\publish\pl","%CD%\bin\Release\net8.0-windows\win-X86\publish\pt-BR","%CD%\bin\Release\net8.0-windows\win-X86\publish\ru","%CD%\bin\Release\net8.0-windows\win-X86\publish\tr","%CD%\bin\Release\net8.0-windows\win-X86\publish\zh-Hans","%CD%\bin\Release\net8.0-windows\win-X86\publish\zh-Hant") do rd /s /q "%%d" 2>nul
del "%CD%\bin\Release\net8.0-windows\win-X86\publish\*.pdb" /Q 2>nul
del "%CD%\bin\Release\net8.0-windows\win-X86\publish\*.xml" /Q 2>nul

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
    -NewPathX64 "%CD%\bin\Release\net8.0-windows\win-x64\publish" ^
    -NewPathX86 "%CD%\bin\Release\net8.0-windows\win-x86\publish" ^
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

:: [3/5] Assinar executaveis
echo [3/5] Assinando executaveis...
for %%f in ("%PATCH_DIR%\x64\*.exe" "%PATCH_DIR%\x86\*.exe") do (
    if exist "%%f" (
        powershell -ExecutionPolicy Bypass -File "%ROOT_DIR%\build\sign.ps1" -ExePath "%%f" >nul 2>&1
        if errorlevel 1 echo AVISO: Falha ao assinar %%f
    )
)

:: [4/5]Limpar arquivos desnecessários
echo [4/5] Limpar arquivos desnecessários...
del "%RELEASE_X64%\*.pdb" /Q 2>nul
del "%RELEASE_X64%\*.xml" /Q 2>nul
del "%RELEASE_X64%\createdump.exe" /Q 2>nul
for /d %%d in ("%RELEASE_X64%\cs","%RELEASE_X64%\de","%RELEASE_X64%\es","%RELEASE_X64%\fr","%RELEASE_X64%\it","%RELEASE_X64%\ja","%RELEASE_X64%\ko","%RELEASE_X64%\pl","%RELEASE_X64%\ru","%RELEASE_X64%\tr","%RELEASE_X64%\zh-Hans","%RELEASE_X64%\zh-Hant") do rd /s /q "%%d" 2>nul

:: [5/5] Gerar instalador
echo [5/5] Gerando instalador...
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
    set "PATCH_FILE=%OUTPUT_DIR%\ValidadorJornada_Patch_%NEW_VERSION%.exe"
    if exist "!PATCH_FILE!" (
        echo Instalador: !PATCH_FILE!
        dir "!PATCH_FILE!" 2>nul | find ".exe"
    )
    echo ===============================================
) else (
    echo.
    echo ===============================================
    echo  PATCH CRIADO (SEM INSTALADOR)
    echo ===============================================
    echo Patch disponivel em: %PATCH_DIR%
    echo ===============================================
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