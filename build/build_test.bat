@echo off
setlocal enabledelayedexpansion

echo ===============================================
echo  Validador de Jornada DP - Testes
echo ===============================================
echo.

:: Definir diretorio raiz
set "ROOT_DIR=%~dp0.."
set "PROJECT_DIR=%ROOT_DIR%\tests\ValidadorJornada.Tests"
set "PROJECT_FILE=%PROJECT_DIR%\ValidadorJornada.Tests.csproj"

:: Verificar .NET SDK
where dotnet >nul 2>&1
if %errorlevel% neq 0 (
    echo ERRO: .NET SDK nao encontrado!
    echo Instale: https://dotnet.microsoft.com/download/dotnet/8.0
    pause
    exit /b 1
)

:: Detectar versao do .NET
for /f "tokens=1" %%i in ('dotnet --version') do set DOTNET_VERSION=%%i
echo Versao .NET SDK: %DOTNET_VERSION%

:: Verificar .NET 8
dotnet --list-sdks | findstr /r "^8\." >nul 2>&1
if %errorlevel% neq 0 (
    echo ERRO: .NET 8 SDK nao encontrado
    pause
    exit /b 1
)

:: Verificar se projeto de testes existe
if not exist "%PROJECT_FILE%" (
    echo ERRO: Projeto de testes nao encontrado
    echo Esperado em: %PROJECT_FILE%
    echo.
    echo Execute setup-testes.ps1 primeiro
    pause
    exit /b 1
)

echo.
echo ===============================================
echo  Executando Testes
echo ===============================================
echo.

cd /d "%PROJECT_DIR%"

:: Restaurar pacotes
echo [1/3] Restaurando pacotes...
dotnet restore --nologo -v q
if errorlevel 1 (
    echo ERRO: Falha ao restaurar pacotes
    pause
    exit /b 1
)

:: Build
echo [2/3] Compilando testes...
dotnet build -c Release --nologo -v q
if errorlevel 1 (
    echo ERRO: Falha na compilacao
    pause
    exit /b 1
)

:: Executar testes
echo [3/3] Executando testes...
echo.
dotnet test --no-build -c Release --logger "console;verbosity=normal"

if errorlevel 1 (
    echo.
    echo ===============================================
    echo  TESTES FALHARAM
    echo ===============================================
) else (
    echo.
    echo ===============================================
    echo  TODOS OS TESTES PASSARAM
    echo ===============================================
)

)
:: Limpar pasta obj do build.
if exist "%PROJECT_DIR%\obj" (
    rd /s /q "%PROJECT_DIR%\obj" 2>nul
)

)
:: Limpar pasta bin do build.
if exist "%PROJECT_DIR%\bin" (
    rd /s /q "%PROJECT_DIR%\bin" 2>nul
)

pause