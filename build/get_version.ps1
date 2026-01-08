$proj = Join-Path $PSScriptRoot "..\src\ValidadorJornada\ValidadorJornada.csproj"
if (-not (Test-Path $proj)) {
    Write-Error "Projeto nao encontrado: $proj"
    exit 1
}
[xml]$xml = Get-Content $proj
$version = $xml.Project.PropertyGroup.Version
if ($version) {
    Write-Output $version
} else {
    Write-Error "Versao nao encontrada no .csproj"
    exit 1
}