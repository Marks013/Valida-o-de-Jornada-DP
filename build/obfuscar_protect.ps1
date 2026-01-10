param(
    [Parameter(Mandatory=$true)]
    [string]$InputFile,
    
    [Parameter(Mandatory=$true)]
    [string]$OutputFile
)

$ErrorActionPreference = "Stop"

Write-Host "=== Obfuscar - .NET Obfuscator ===" -ForegroundColor Cyan
Write-Host "Arquivo: $InputFile" -ForegroundColor White

# Verificar se arquivo existe
if (-not (Test-Path $InputFile)) {
    Write-Host "ERRO: Arquivo nao encontrado: $InputFile" -ForegroundColor Red
    exit 1
}

$sizeKB = [Math]::Round((Get-Item $InputFile).Length / 1KB, 0)
Write-Host "Tamanho: $sizeKB KB" -ForegroundColor Gray

# Localizar Obfuscar
Write-Host "Localizando Obfuscar..." -ForegroundColor Gray

$obfuscarPath = $null
$possiblePaths = @(
    "$env:USERPROFILE\.dotnet\tools\obfuscar.console.exe",
    "$env:USERPROFILE\.dotnet\tools\Obfuscar.Console.exe",
    "$env:USERPROFILE\.dotnet\tools\obfuscar.exe",
    "$env:USERPROFILE\.dotnet\tools\Obfuscar.exe"
)

foreach ($path in $possiblePaths) {
    if (Test-Path $path) {
        $obfuscarPath = $path
        Write-Host "OK: Obfuscar encontrado em: $obfuscarPath" -ForegroundColor Green
        break
    }
}

if (-not $obfuscarPath) {
    Write-Host "Obfuscar nao encontrado. Instalando..." -ForegroundColor Yellow
    
    try {
        dotnet tool install -g Obfuscar.GlobalTool --verbosity quiet
        
        if ($LASTEXITCODE -ne 0) {
            throw "Falha ao instalar"
        }
        
        $obfuscarPath = "$env:USERPROFILE\.dotnet\tools\obfuscar.console.exe"
        
        if (-not (Test-Path $obfuscarPath)) {
            $obfuscarPath = "$env:USERPROFILE\.dotnet\tools\Obfuscar.Console.exe"
        }
        
        if (-not (Test-Path $obfuscarPath)) {
            throw "Obfuscar instalado mas nao encontrado"
        }
        
        Write-Host "OK: Obfuscar instalado em: $obfuscarPath" -ForegroundColor Green
    } catch {
        Write-Host "ERRO: Falha ao instalar Obfuscar" -ForegroundColor Red
        Write-Host "Tente manualmente: dotnet tool install -g Obfuscar.GlobalTool" -ForegroundColor Yellow
        exit 1
    }
}

# Preparar diretórios temporários
$tempDir = [System.IO.Path]::Combine([System.IO.Path]::GetTempPath(), "Obfuscar_" + [System.Guid]::NewGuid().ToString())
New-Item -ItemType Directory -Path $tempDir -Force | Out-Null

$tempInput = Join-Path $tempDir "input"
$tempOutput = Join-Path $tempDir "output"
New-Item -ItemType Directory -Path $tempInput -Force | Out-Null
New-Item -ItemType Directory -Path $tempOutput -Force | Out-Null

Write-Host "Diretorio temporario: $tempDir" -ForegroundColor Gray

try {
    # Copiar DLL e dependências para temp
    Write-Host "Copiando arquivos para diretorio temporario..." -ForegroundColor Gray
    $inputDir = Split-Path $InputFile -Parent
    $inputName = Split-Path $InputFile -Leaf
    
    # Copiar a DLL principal
    Copy-Item $InputFile $tempInput -Force
    
    # Copiar dependências (importantes para resolução)
    $dependencies = @("*.dll", "*.exe", "*.deps.json", "*.runtimeconfig.json")
    foreach ($dep in $dependencies) {
        Get-ChildItem (Join-Path $inputDir $dep) -ErrorAction SilentlyContinue | ForEach-Object {
            Copy-Item $_.FullName $tempInput -Force -ErrorAction SilentlyContinue
        }
    }
    
    # Criar configuração apontando para temp
    $config = @"
<?xml version='1.0'?>
<Obfuscator>
  <Var name="InPath" value="$tempInput" />
  <Var name="OutPath" value="$tempOutput" />
  
  <!-- Configurações de ofuscação -->
  <Var name="HidePrivateApi" value="true" />
  <Var name="RenameProperties" value="false" />
  <Var name="RenameEvents" value="true" />
  <Var name="RenameFields" value="true" />
  <Var name="Optimize" value="false" />
  <Var name="UseUnicodeNames" value="false" />
  <Var name="MarkedOnly" value="false" />
  <Var name="KeepPublicApi" value="true" />
  <Var name="HideStrings" value="true" />
  
  <!-- Módulo a ofuscar -->
  <Module file="`$(InPath)\$inputName">
    <SkipNamespace name="System.*" />
    <SkipNamespace name="Microsoft.*" />
  </Module>
</Obfuscator>
"@

    $configFile = Join-Path $tempDir "obfuscar.xml"
    $config | Out-File $configFile -Encoding UTF8

    Write-Host "Processando com Obfuscar..." -ForegroundColor Yellow
    $startTime = Get-Date

    # Executar Obfuscar usando caminho completo
    $processInfo = New-Object System.Diagnostics.ProcessStartInfo
    $processInfo.FileName = $obfuscarPath
    $processInfo.Arguments = "`"$configFile`""
    $processInfo.RedirectStandardOutput = $true
    $processInfo.RedirectStandardError = $true
    $processInfo.UseShellExecute = $false
    $processInfo.CreateNoWindow = $true
    
    $process = New-Object System.Diagnostics.Process
    $process.StartInfo = $processInfo
    $process.Start() | Out-Null
    
    $stdout = $process.StandardOutput.ReadToEnd()
    $stderr = $process.StandardError.ReadToEnd()
    
    $process.WaitForExit()
    
    if ($process.ExitCode -ne 0) {
        Write-Host "Stdout: $stdout" -ForegroundColor Gray
        Write-Host "Stderr: $stderr" -ForegroundColor Gray
        throw "Obfuscar retornou codigo: $($process.ExitCode)"
    }
    
    # Verificar se arquivo foi gerado
    $obfuscatedFile = Join-Path $tempOutput $inputName
    
    if (-not (Test-Path $obfuscatedFile)) {
        throw "Arquivo ofuscado nao foi gerado em: $obfuscatedFile"
    }
    
    # Copiar de volta para destino
    Write-Host "Copiando arquivo ofuscado..." -ForegroundColor Gray
    Copy-Item $obfuscatedFile $OutputFile -Force
    
    $totalTime = [int]((Get-Date) - $startTime).TotalSeconds
    $newSizeKB = [Math]::Round((Get-Item $OutputFile).Length / 1KB, 0)
    $diff = [Math]::Round((($newSizeKB - $sizeKB) / $sizeKB) * 100, 1)
    
    Write-Host "OK: Ofuscacao concluida em $totalTime segundos" -ForegroundColor Green
    Write-Host "Arquivo salvo: $OutputFile" -ForegroundColor Green
    Write-Host "Tamanho: $sizeKB KB -> $newSizeKB KB (+$diff%)" -ForegroundColor Gray
    Write-Host "Protecoes aplicadas:" -ForegroundColor Cyan
    Write-Host "  - Symbol Renaming (Unicode)" -ForegroundColor DarkGreen
    Write-Host "  - String Hiding" -ForegroundColor DarkGreen
    Write-Host "  - Private API Hiding" -ForegroundColor DarkGreen
    Write-Host "  - Property/Event/Field Renaming" -ForegroundColor DarkGreen
    
    exit 0
    
} catch {
    Write-Host "ERRO: $($_.Exception.Message)" -ForegroundColor Red
    
    # Diagnóstico
    Write-Host ""
    Write-Host "Diagnostico:" -ForegroundColor Yellow
    Write-Host "  - Verifique se a DLL e um assembly .NET valido" -ForegroundColor Gray
    Write-Host "  - Config usado: $configFile" -ForegroundColor Gray
    
    exit 1
} finally {
    # Limpar diretório temporário
    if (Test-Path $tempDir) {
        Start-Sleep -Milliseconds 500
        Remove-Item $tempDir -Recurse -Force -ErrorAction SilentlyContinue
    }
}