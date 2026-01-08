param(
    [string]$AppPath = "$env:ProgramFiles\ValidadorJornada"
)

# Verificar privilégios
$isAdmin = ([Security.Principal.WindowsPrincipal][Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)

if (-not $isAdmin) {
    Write-Host "ERRO: Execute como Administrador" -ForegroundColor Red
    Write-Host "Clique com botao direito > Executar como administrador" -ForegroundColor Yellow
    Read-Host "`nPressione Enter para sair"
    exit 1
}

$parent = Split-Path $AppPath
$backups = Get-ChildItem -Path $parent -Directory -Filter "*.backup*" |
           Sort-Object LastWriteTime -Descending

if ($backups.Count -eq 0) {
    Write-Host "Nenhum backup encontrado em: $parent" -ForegroundColor Red
    Read-Host "`nPressione Enter para sair"
    exit 1
}

Write-Host "=== ROLLBACK HELPER ===" -ForegroundColor Cyan
Write-Host "`nBackups disponiveis:" -ForegroundColor White
for ($i = 0; $i -lt $backups.Count; $i++) {
    Write-Host "  [$i] $($backups[$i].Name) - $($backups[$i].LastWriteTime)" -ForegroundColor Yellow
}

$choice = Read-Host "`nEscolha o numero do backup"

if ([int]::TryParse($choice, [ref]$null) -and [int]$choice -ge 0 -and [int]$choice -lt $backups.Count) {
    $backup = $backups[[int]$choice]
    
    Write-Host "`nRestaurando de: $($backup.Name)..." -ForegroundColor Cyan
    
    try {
        if (Test-Path $AppPath) {
            Remove-Item -Path $AppPath -Recurse -Force -ErrorAction Stop
        }
        Copy-Item -Path $backup.FullName -Destination $AppPath -Recurse -Force -ErrorAction Stop
        Write-Host "`nConcluido! Aplicacao restaurada." -ForegroundColor Green
    }
    catch {
        Write-Host "`nERRO: $($_.Exception.Message)" -ForegroundColor Red
    }
} else {
    Write-Host "`nOpcao invalida" -ForegroundColor Red
}

Read-Host "`nPressione Enter para sair"