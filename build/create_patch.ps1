param(
    [string]$BaseVersion,
    [string]$NewVersion,
    [string]$BasePath,
    [string]$NewPathX64,
    [string]$NewPathX86,
    [string]$OutputPath
)

$ErrorActionPreference = "Stop"

Write-Host "Criando patch inteligente (somente arquivos da aplicacao)..." -ForegroundColor Cyan
Write-Host "Base: $BaseVersion -> Nova: $NewVersion" -ForegroundColor White

# Validar parametros
if (-not $BaseVersion -or -not $NewVersion -or -not $BasePath -or -not $OutputPath) {
    Write-Host "ERRO: Parametros obrigatorios faltando" -ForegroundColor Red
    exit 1
}

# Verificar caminhos
if (-not (Test-Path $BasePath)) {
    Write-Host "ERRO: Caminho base nao existe: $BasePath" -ForegroundColor Red
    exit 1
}

if (-not (Test-Path $NewPathX64)) {
    Write-Host "ERRO: Caminho x64 nao existe: $NewPathX64" -ForegroundColor Red
    exit 1
}

if (-not (Test-Path $NewPathX86)) {
    Write-Host "ERRO: Caminho x86 nao existe: $NewPathX86" -ForegroundColor Red
    exit 1
}

# ===== LISTA DE ARQUIVOS DA APLICACAO (NAO DO RUNTIME) =====
$appFiles = @(
    'ValidadorJornada.exe',
    'ValidadorJornada.dll',
    'ValidadorJornada.deps.json',
    'ValidadorJornada.runtimeconfig.json',
    'config.json',
    
    # Dependencias NuGet que VOCE instalou (nao sao do .NET runtime)
    'ExcelDataReader.dll',
    'ExcelDataReader.DataSet.dll',
    'NJsonSchema.dll',
    'QuestPDF.dll',
    'EPPlus.dll',
    'SkiaSharp.dll',
    'HarfBuzzSharp.dll',
    'System.Text.Json.dll',  # Versao especifica do seu projeto
    
    # Recursos
    'Resources\*'
)

# Criar estrutura
$outX64 = Join-Path $OutputPath "x64"
$outX86 = Join-Path $OutputPath "x86"

if (Test-Path $OutputPath) {
    Remove-Item $OutputPath -Recurse -Force
}
New-Item -Path $outX64 -ItemType Directory -Force | Out-Null
New-Item -Path $outX86 -ItemType Directory -Force | Out-Null

$totalCopied = 0
$newFiles = @()
$modifiedFiles = @()

# Funcao para comparar e copiar APENAS arquivos da aplicacao
function Copy-AppFilesOnly {
    param(
        [string]$NewPath, 
        [string]$BaseArch, 
        [string]$OutPath,
        [string]$Architecture
    )
    
    $copied = 0
    $archNewFiles = @()
    $archModifiedFiles = @()
    
    Write-Host "`n  Analisando $Architecture (somente arquivos da aplicacao)..." -ForegroundColor White
    
    foreach ($pattern in $script:appFiles) {
        $files = Get-ChildItem -Path $NewPath -Filter $pattern -Recurse -File -ErrorAction SilentlyContinue
        
        foreach ($file in $files) {
            $rel = $file.FullName.Substring($NewPath.Length + 1)
            $baseFile = Join-Path $BaseArch $rel
            
            $shouldCopy = $false
            $fileStatus = ""
            
            if (-not (Test-Path $baseFile)) {
                $shouldCopy = $true
                $fileStatus = "NOVO"
                $archNewFiles += $rel
            } else {
                $newHash = (Get-FileHash $file.FullName -Algorithm SHA256).Hash
                $baseHash = (Get-FileHash $baseFile -Algorithm SHA256).Hash
                if ($newHash -ne $baseHash) {
                    $shouldCopy = $true
                    $fileStatus = "MODIFICADO"
                    $archModifiedFiles += $rel
                }
            }
            
            if ($shouldCopy) {
                $dest = Join-Path $OutPath $rel
                $destDir = Split-Path $dest -Parent
                if (-not (Test-Path $destDir)) {
                    New-Item -Path $destDir -ItemType Directory -Force | Out-Null
                }
                Copy-Item $file.FullName $dest -Force
                $copied++
                
                if ($fileStatus -eq "NOVO") {
                    Write-Host "    + $rel" -ForegroundColor Green
                } else {
                    Write-Host "    * $rel" -ForegroundColor Yellow
                }
            }
        }
    }
    
    if ($copied -eq 0) {
        Write-Host "  Nenhum arquivo da aplicacao foi modificado!" -ForegroundColor Cyan
    } else {
        Write-Host "  Total $Architecture : $copied arquivos ($($archNewFiles.Count) novos, $($archModifiedFiles.Count) modificados)" -ForegroundColor Cyan
    }
    
    return @{
        Count = $copied
        NewFiles = $archNewFiles
        ModifiedFiles = $archModifiedFiles
    }
}

# Processar x64
Write-Host "`nProcessando arquitetura x64..." -ForegroundColor Yellow
$baseX64 = Join-Path $BasePath "x64"
$resultX64 = Copy-AppFilesOnly -NewPath $NewPathX64 -BaseArch $baseX64 -OutPath $outX64 -Architecture "x64"
$totalCopied += $resultX64.Count

# Processar x86
Write-Host "`nProcessando arquitetura x86..." -ForegroundColor Yellow
$baseX86 = Join-Path $BasePath "x86"
$resultX86 = Copy-AppFilesOnly -NewPath $NewPathX86 -BaseArch $baseX86 -OutPath $outX86 -Architecture "x86"
$totalCopied += $resultX86.Count

if ($totalCopied -eq 0) {
    Write-Host "`n========================================" -ForegroundColor Yellow
    Write-Host "NENHUMA ALTERACAO DETECTADA" -ForegroundColor Yellow
    Write-Host "========================================" -ForegroundColor Yellow
    Write-Host "As versoes $BaseVersion e $NewVersion sao identicas." -ForegroundColor White
    Write-Host "Nao e necessario criar patch." -ForegroundColor White
    Write-Host "========================================`n" -ForegroundColor Yellow
    
    # Limpar pasta vazia
    Remove-Item $OutputPath -Recurse -Force
    exit 0
}

# Criar manifesto detalhado
$manifest = @{
    version = $NewVersion
    baseVersion = $BaseVersion
    buildDate = Get-Date -Format "yyyy-MM-dd HH:mm:ss"
    patchType = "application-only"
    x64 = @{
        totalFiles = $resultX64.Count
        newFiles = $resultX64.NewFiles
        modifiedFiles = $resultX64.ModifiedFiles
    }
    x86 = @{
        totalFiles = $resultX86.Count
        newFiles = $resultX86.NewFiles
        modifiedFiles = $resultX86.ModifiedFiles
    }
    totalFiles = $totalCopied
}

$manifestPath = Join-Path $OutputPath "manifest.json"
$manifest | ConvertTo-Json -Depth 3 | Out-File $manifestPath -Encoding UTF8

# Gerar checksums para o patch
Write-Host "`nGerando checksums do patch..." -ForegroundColor White

# Checksum x64
if (Test-Path $outX64) {
    $files = Get-ChildItem $outX64 -Recurse -File
    if ($files) {
        $checksums = @()
        foreach ($f in $files) {
            $hash = (Get-FileHash $f.FullName -Algorithm SHA256).Hash
            $rel = $f.FullName.Replace("$outX64\", "")
            $checksums += "$hash  $rel"
        }
        $checksums | Out-File "$outX64\checksums.sha256" -Encoding UTF8
    }
}

# Checksum x86
if (Test-Path $outX86) {
    $files = Get-ChildItem $outX86 -Recurse -File
    if ($files) {
        $checksums = @()
        foreach ($f in $files) {
            $hash = (Get-FileHash $f.FullName -Algorithm SHA256).Hash
            $rel = $f.FullName.Replace("$outX86\", "")
            $checksums += "$hash  $rel"
        }
        $checksums | Out-File "$outX86\checksums.sha256" -Encoding UTF8
    }
}

Write-Host "`n========================================" -ForegroundColor Cyan
Write-Host "PATCH CRIADO COM SUCESSO" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Versao: $BaseVersion -> $NewVersion" -ForegroundColor White
Write-Host "Tipo: Somente arquivos da aplicacao" -ForegroundColor White
Write-Host "x64: $($resultX64.Count) arquivos" -ForegroundColor White
Write-Host "x86: $($resultX86.Count) arquivos" -ForegroundColor White
Write-Host "Total: $totalCopied arquivos no patch" -ForegroundColor Yellow
Write-Host "========================================`n" -ForegroundColor Cyan

exit 0