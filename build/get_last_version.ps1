$releasesDir = Join-Path $PSScriptRoot "..\releases"
if (-not (Test-Path $releasesDir)) { exit 1 }

$versions = Get-ChildItem $releasesDir -Directory | 
    Where-Object { $_.Name -match '^\d+\.\d+\.\d+$' } |
    Sort-Object { [Version]$_.Name } -Descending

# Pular a versão atual se existir
$currentVersion = (Select-Xml -Path (Join-Path $PSScriptRoot "..\src\ValidadorJornada\ValidadorJornada.csproj") -XPath "//Version").Node.InnerText

$baseVersion = $versions | Where-Object { $_.Name -ne $currentVersion } | Select-Object -First 1

if ($baseVersion) { 
    Write-Output $baseVersion.Name 
} else {
    exit 1
}