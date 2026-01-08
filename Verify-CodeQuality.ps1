# Script de Verificacao de Qualidade
# Validador de Jornada DP

param(
    [string]$ProjectPath = "."
)

$ErrorActionPreference = "Continue"

Write-Host "=========================================" -ForegroundColor Cyan
Write-Host "  VERIFICACAO DE QUALIDADE" -ForegroundColor Cyan
Write-Host "=========================================" -ForegroundColor Cyan
Write-Host ""

$totalIssues = 0
$criticalIssues = 0
$warnings = 0

# 1. VERIFICAR ENCODING
Write-Host "1. Verificando Encoding UTF-8..." -ForegroundColor Yellow

$badEncodingFiles = @()
$csFiles = Get-ChildItem -Path $ProjectPath -Filter "*.cs" -File

foreach ($file in $csFiles) {
    $content = Get-Content -Path $file.FullName -Raw -Encoding UTF8
    if ($content -match 'Ã|Â') {
        $badEncodingFiles += $file.Name
    }
}

if ($badEncodingFiles.Count -gt 0) {
    Write-Host "  CRITICO: $($badEncodingFiles.Count) arquivos com encoding incorreto" -ForegroundColor Red
    $criticalIssues += $badEncodingFiles.Count
    $totalIssues += $badEncodingFiles.Count
} else {
    Write-Host "  OK: Todos os arquivos com encoding correto" -ForegroundColor Green
}

# 2. VERIFICAR DEPENDENCIAS
Write-Host ""
Write-Host "2. Verificando Dependencias..." -ForegroundColor Yellow

$csprojPath = Get-ChildItem -Path $ProjectPath -Filter "*.csproj" -File | Select-Object -First 1

if ($csprojPath) {
    $csprojContent = Get-Content -Path $csprojPath.FullName -Raw
    
    if ($csprojContent -match 'Newtonsoft\.Json') {
        $hasNewtonsoftUsage = $false
        
        foreach ($file in $csFiles) {
            $content = Get-Content -Path $file.FullName -Raw
            if ($content -match 'using Newtonsoft\.Json|JsonConvert') {
                $hasNewtonsoftUsage = $true
                break
            }
        }
        
        if (-not $hasNewtonsoftUsage) {
            Write-Host "  AVISO: Newtonsoft.Json nao utilizado" -ForegroundColor Yellow
            $warnings++
            $totalIssues++
        } else {
            Write-Host "  OK: Newtonsoft.Json esta sendo usado" -ForegroundColor Green
        }
    } else {
        Write-Host "  OK: Newtonsoft.Json nao esta no projeto" -ForegroundColor Green
    }
}

# 3. VERIFICAR VERSOES
Write-Host ""
Write-Host "3. Verificando Versoes..." -ForegroundColor Yellow

if ($csprojPath) {
    $csprojContent = Get-Content -Path $csprojPath.FullName -Raw
    
    $version = [regex]::Match($csprojContent, '<Version>([\d.]+)</Version>').Groups[1].Value
    $assemblyVersion = [regex]::Match($csprojContent, '<AssemblyVersion>([\d.]+)</AssemblyVersion>').Groups[1].Value
    
    if ($version -and $assemblyVersion) {
        if ($version -eq $assemblyVersion) {
            Write-Host "  OK: Versoes consistentes: $version" -ForegroundColor Green
        } else {
            Write-Host "  AVISO: Versoes inconsistentes" -ForegroundColor Yellow
            Write-Host "     Version: $version" -ForegroundColor Gray
            Write-Host "     AssemblyVersion: $assemblyVersion" -ForegroundColor Gray
            $warnings++
            $totalIssues++
        }
    }
}

# 4. VERIFICAR DUPLICACOES
Write-Host ""
Write-Host "4. Verificando Duplicacoes..." -ForegroundColor Yellow

$classes = @{}

foreach ($file in $csFiles) {
    $content = Get-Content -Path $file.FullName -Raw
    $matches = [regex]::Matches($content, 'class\s+(\w+)')
    
    foreach ($match in $matches) {
        $className = $match.Groups[1].Value
        
        if (-not $classes.ContainsKey($className)) {
            $classes[$className] = @()
        }
        
        $classes[$className] += $file.Name
    }
}

$duplicates = $classes.GetEnumerator() | Where-Object { $_.Value.Count -gt 1 }

if ($duplicates) {
    Write-Host "  AVISO: Classes duplicadas" -ForegroundColor Yellow
    foreach ($dup in $duplicates) {
        Write-Host "     - $($dup.Key) em: $($dup.Value -join ', ')" -ForegroundColor Gray
    }
    $warnings += $duplicates.Count
    $totalIssues += $duplicates.Count
} else {
    Write-Host "  OK: Nenhuma duplicacao" -ForegroundColor Green
}

# 5. VERIFICAR EDITORCONFIG
Write-Host ""
Write-Host "5. Verificando .editorconfig..." -ForegroundColor Yellow

if (Test-Path (Join-Path $ProjectPath ".editorconfig")) {
    Write-Host "  OK: .editorconfig encontrado" -ForegroundColor Green
} else {
    Write-Host "  RECOMENDADO: Adicionar .editorconfig" -ForegroundColor Yellow
    $warnings++
    $totalIssues++
}

# RESUMO
Write-Host ""
Write-Host "=========================================" -ForegroundColor Cyan
Write-Host "  RESUMO" -ForegroundColor Cyan
Write-Host "=========================================" -ForegroundColor Cyan
Write-Host ""

if ($totalIssues -eq 0) {
    Write-Host "  EXCELENTE! Nenhum problema encontrado!" -ForegroundColor Green
} else {
    Write-Host "  Total de problemas: $totalIssues" -ForegroundColor White
    Write-Host "    - Criticos: $criticalIssues" -ForegroundColor Red
    Write-Host "    - Avisos: $warnings" -ForegroundColor Yellow
}

Write-Host ""

if ($criticalIssues -gt 0) {
    Write-Host "ACOES URGENTES:" -ForegroundColor Red
    Write-Host "  1. Execute: .\Fix-Encoding.ps1" -ForegroundColor White
}

if ($warnings -gt 0 -and $criticalIssues -eq 0) {
    Write-Host "RECOMENDACOES:" -ForegroundColor Yellow
    Write-Host "  1. Revisar avisos acima" -ForegroundColor White
}

Write-Host ""
Write-Host "=========================================" -ForegroundColor Cyan
