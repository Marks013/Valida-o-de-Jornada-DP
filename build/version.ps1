$proj = Join-Path $PSScriptRoot "..\src\ValidadorJornada\ValidadorJornada.csproj"
[xml]$xml = Get-Content $proj
$ver = $xml.Project.PropertyGroup.Version

$files = @("installer_full.iss", "installer_patch.iss")
foreach ($f in $files) {
    $path = Join-Path $PSScriptRoot $f
    if (Test-Path $path) {
        (Get-Content $path) -replace '#define MyAppVersion ".*"', "#define MyAppVersion `"$ver`"" | Set-Content $path
    }
}

Write-Host "Versao atualizada: $ver" -ForegroundColor Green