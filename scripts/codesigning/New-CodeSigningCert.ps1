<#
.SYNOPSIS
    Gera um certificado self-signed de Code Signing para assinar os binarios do
    AnalistaPalmaseg e evitar o bloqueio do Controle de Aplicativo Inteligente (Smart App Control).

.DESCRIPTION
    Cria o certificado no repositorio de certificados do usuario atual, depois exporta:
      - um .pfx (chave privada, protegido por senha) usado para ASSINAR os builds.
      - um .cer (somente chave publica) que deve ser IMPORTADO em todas as maquinas
        (servidor + estacoes) para que o Windows passe a confiar em arquivos assinados
        com esse certificado. Veja Import-TrustedCert.ps1.

    Rode este script UMA VEZ (ou sempre que o certificado expirar). Guarde o .pfx e a
    senha em local seguro: quem tiver o .pfx pode assinar executaveis como "confiaveis"
    para as maquinas que importarem o .cer.

.EXAMPLE
    ./New-CodeSigningCert.ps1
#>
[CmdletBinding()]
param(
    [string]$Subject = "CN=KempalmaTech Analista Palmaseg, O=KempalmaTech",
    [string]$OutDir = (Join-Path $PSScriptRoot "..\..\certs"),
    [int]$ValidYears = 5
)

$ErrorActionPreference = "Stop"

if (-not (Test-Path $OutDir)) {
    New-Item -ItemType Directory -Path $OutDir -Force | Out-Null
}
$OutDir = (Resolve-Path $OutDir).Path

Write-Host "Gerando certificado de code signing: $Subject" -ForegroundColor Cyan

$cert = New-SelfSignedCertificate `
    -Type CodeSigningCert `
    -Subject $Subject `
    -CertStoreLocation "Cert:\CurrentUser\My" `
    -KeyExportPolicy Exportable `
    -KeyAlgorithm RSA `
    -KeyLength 2048 `
    -KeyUsage DigitalSignature `
    -NotAfter (Get-Date).AddYears($ValidYears)

Write-Host "Certificado criado. Thumbprint: $($cert.Thumbprint)" -ForegroundColor Green

$pfxPassword = Read-Host -Prompt "Defina uma senha para proteger o .pfx (chave privada)" -AsSecureString

$pfxPath = Join-Path $OutDir "AnalistaPalmaseg-CodeSigning.pfx"
$cerPath = Join-Path $OutDir "AnalistaPalmaseg-CodeSigning.cer"

Export-PfxCertificate -Cert $cert -FilePath $pfxPath -Password $pfxPassword | Out-Null
Export-Certificate -Cert $cert -FilePath $cerPath | Out-Null

Write-Host ""
Write-Host "Arquivos gerados em: $OutDir" -ForegroundColor Green
Write-Host "  - $pfxPath  (PRIVADO - usar para assinar builds, guardar com seguranca, NAO versionar)"
Write-Host "  - $cerPath  (PUBLICO - distribuir/importar em todas as maquinas, pode ir para o repositorio)"
Write-Host ""
Write-Host "Proximos passos:" -ForegroundColor Yellow
Write-Host "  1. Rode Import-TrustedCert.ps1 (como administrador) no servidor e em cada estacao,"
Write-Host "     apontando para $cerPath"
Write-Host "  2. O publish (dotnet publish / Publish do Visual Studio) ja assina automaticamente,"
Write-Host "     usando o certificado que ficou no repositorio Cert:\CurrentUser\My desta maquina."

# O certificado fica no repositorio do usuario (Cert:\CurrentUser\My) de propósito: eh o que
# permite o Sign-Publish.ps1 assinar automaticamente (via thumbprint) sem pedir senha a cada
# publish. O .pfx exportado acima serve apenas de backup/para habilitar outra maquina a publicar.
$thumbprintPath = Join-Path $OutDir "thumbprint.txt"
Set-Content -Path $thumbprintPath -Value $cert.Thumbprint -NoNewline
Write-Host "  - $thumbprintPath  (thumbprint do certificado ativo, usado automaticamente pelo publish)"
