param(
    [Parameter(Mandatory=$true)]
    [string]$ExePath
)

if (-not (Test-Path $ExePath)) {
    Write-Error "Arquivo nao encontrado: $ExePath"
    exit 1
}

# Tentar usar certificado existente ou criar temporario
$cert = Get-ChildItem -Path Cert:\CurrentUser\My -CodeSigningCert | 
    Where-Object {$_.Subject -like "*ValidadorJornada*"} | 
    Select-Object -First 1

if (-not $cert) {
    # Criar certificado self-signed temporario
    $cert = New-SelfSignedCertificate `
        -Type CodeSigningCert `
        -Subject "CN=ValidadorJornada Dev Certificate" `
        -KeySpec Signature `
        -KeyUsage DigitalSignature `
        -FriendlyName "ValidadorJornada Dev" `
        -CertStoreLocation "Cert:\CurrentUser\My" `
        -NotAfter (Get-Date).AddYears(2)
}

# Assinar arquivo
try {
    Set-AuthenticodeSignature -FilePath $ExePath -Certificate $cert -TimestampServer "http://timestamp.digicert.com" | Out-Null
    Write-Host "Assinado: $ExePath" -ForegroundColor Green
    exit 0
} catch {
    Write-Warning "Falha na assinatura: $_"
    exit 1
}