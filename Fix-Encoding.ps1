# Script de Correcao de Encoding UTF-8 - RECURSIVO
# Validador de Jornada DP

param(
    [string]$ProjectPath = ".",
    [switch]$DryRun = $false,
    [switch]$CreateBackup = $true
)

$ErrorActionPreference = "Stop"

Write-Host "==========================================" -ForegroundColor Cyan
Write-Host "  CORRECAO DE ENCODING UTF-8 (RECURSIVO)" -ForegroundColor Cyan
Write-Host "==========================================" -ForegroundColor Cyan
Write-Host ""

# Mapa de correcoes
$replacements = @{
    'Ã§' = 'ç'
    'Ã£' = 'ã'
    'Ã©' = 'é'
    'Ãª' = 'ê'
    'Ã³' = 'ó'
    'Ã´' = 'ô'
    'Ã¡' = 'á'
    'Ã¢' = 'â'
    'Ã­' = 'í'
    'Ãº' = 'ú'
    'Ã ' = 'à'
    'Â©' = '©'
    'Âª' = 'ª'
    'Âº' = 'º'
    'Ã‡' = 'Ç'
    'Ã‰' = 'É'
    'Ãƒ' = 'Ã'
}

$filesProcessed = 0
$filesModified = 0
$totalReplacements = 0

function Fix-FileEncoding {
    param([string]$FilePath)
    
    try {
        $content = Get-Content -Path $FilePath -Raw -Encoding UTF8
        $replaced = 0
        
        foreach ($key in $replacements.Keys) {
            $pattern = [regex]::Escape($key)
            $matches = [regex]::Matches($content, $pattern)
            if ($matches.Count -gt 0) {
                $content = $content -replace $pattern, $replacements[$key]
                $replaced += $matches.Count
            }
        }
        
        if ($replaced -gt 0) {
            $relativePath = $FilePath -replace [regex]::Escape((Get-Location).Path), '.'
            Write-Host "  OK $relativePath" -ForegroundColor Green
            Write-Host "    -> $replaced correcoes" -ForegroundColor Gray
            
            if (-not $DryRun) {
                if ($CreateBackup) {
                    Copy-Item -Path $FilePath -Destination "$FilePath.bak" -Force
                }
                
                $utf8BOM = New-Object System.Text.UTF8Encoding $true
                [System.IO.File]::WriteAllText($FilePath, $content, $utf8BOM)
            }
            
            return @{ Modified = $true; Replacements = $replaced }
        } else {
            return @{ Modified = $false; Replacements = 0 }
        }
    }
    catch {
        Write-Host "  ERRO: $FilePath - $($_.Exception.Message)" -ForegroundColor Red
        return @{ Modified = $false; Replacements = 0 }
    }
}

Write-Host "Buscando arquivos..." -ForegroundColor Yellow
Write-Host ""

# Buscar RECURSIVAMENTE
$csFiles = Get-ChildItem -Path $ProjectPath -Filter "*.cs" -File -Recurse -ErrorAction SilentlyContinue
$csprojFiles = Get-ChildItem -Path $ProjectPath -Filter "*.csproj" -File -Recurse -ErrorAction SilentlyContinue
$xamlFiles = Get-ChildItem -Path $ProjectPath -Filter "*.xaml" -File -Recurse -ErrorAction SilentlyContinue

Write-Host "Encontrados:" -ForegroundColor Cyan
Write-Host "  - $($csFiles.Count) arquivos .cs" -ForegroundColor Gray
Write-Host "  - $($csprojFiles.Count) arquivos .csproj" -ForegroundColor Gray
Write-Host "  - $($xamlFiles.Count) arquivos .xaml" -ForegroundColor Gray
Write-Host ""

if ($csFiles.Count -eq 0 -and $csprojFiles.Count -eq 0) {
    Write-Host "ERRO: Nenhum arquivo encontrado!" -ForegroundColor Red
    Write-Host "Verifique se esta no diretorio correto." -ForegroundColor Yellow
    Write-Host "Diretorio atual: $(Get-Location)" -ForegroundColor Gray
    exit 1
}

Write-Host "Processando arquivos .cs..." -ForegroundColor Yellow

foreach ($file in $csFiles) {
    $result = Fix-FileEncoding -FilePath $file.FullName
    $filesProcessed++
    if ($result.Modified) {
        $filesModified++
        $totalReplacements += $result.Replacements
    }
}

Write-Host ""
Write-Host "Processando arquivos .csproj..." -ForegroundColor Yellow

foreach ($file in $csprojFiles) {
    $result = Fix-FileEncoding -FilePath $file.FullName
    $filesProcessed++
    if ($result.Modified) {
        $filesModified++
        $totalReplacements += $result.Replacements
    }
}

Write-Host ""
Write-Host "Processando arquivos .xaml..." -ForegroundColor Yellow

foreach ($file in $xamlFiles) {
    $result = Fix-FileEncoding -FilePath $file.FullName
    $filesProcessed++
    if ($result.Modified) {
        $filesModified++
        $totalReplacements += $result.Replacements
    }
}

Write-Host ""
Write-Host "==========================================" -ForegroundColor Cyan
Write-Host "  RESULTADO" -ForegroundColor Cyan
Write-Host "==========================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "  Arquivos processados: $filesProcessed" -ForegroundColor White

if ($filesModified -gt 0) {
    Write-Host "  Arquivos modificados: $filesModified" -ForegroundColor Green
    Write-Host "  Total de correcoes: $totalReplacements" -ForegroundColor Green
} else {
    Write-Host "  Arquivos modificados: 0" -ForegroundColor Green
    Write-Host "  PARABENS! Nenhum problema de encoding encontrado!" -ForegroundColor Green
}

if ($DryRun) {
    Write-Host ""
    Write-Host "  MODO DRY-RUN - Nenhum arquivo foi modificado" -ForegroundColor Yellow
    if ($filesModified -gt 0) {
        Write-Host "  Execute sem -DryRun para aplicar as $totalReplacements correcoes" -ForegroundColor Yellow
    }
}

if ($CreateBackup -and -not $DryRun -and $filesModified -gt 0) {
    Write-Host ""
    Write-Host "  INFO: Backups criados (.bak)" -ForegroundColor Cyan
}

Write-Host ""
Write-Host "==========================================" -ForegroundColor Cyan
Write-Host ""

# Exemplo de uso
if ($filesModified -eq 0 -and -not $DryRun) {
    Write-Host "PROXIMOS PASSOS:" -ForegroundColor Yellow
    Write-Host "  1. Verificar dependencias: Get-ChildItem -Recurse *.cs | Select-String 'Newtonsoft'" -ForegroundColor Gray
    Write-Host "  2. Adicionar .editorconfig na raiz do projeto" -ForegroundColor Gray
    Write-Host "  3. Build: dotnet build --configuration Release" -ForegroundColor Gray
}