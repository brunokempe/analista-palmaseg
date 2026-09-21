<#
.SYNOPSIS
    Assina digitalmente os executaveis/DLLs do AnalistaPalmaseg gerados pelo publish,
    usando o certificado self-signed de code signing do projeto.

.DESCRIPTION
    Roda depois de "dotnet publish" (ou do publish do Visual Studio) - o FolderProfile.pubxml
    ja chama este script automaticamente. Assina todos os .exe e .dll cuja empresa/produto seja
    do AnalistaPalmaseg (evita perder tempo assinando DLLs de terceiros/framework que nao
    precisam disso), usando Set-AuthenticodeSignature.

    Por padrao usa o certificado pelo Thumbprint, direto do repositorio Cert:\CurrentUser\My
    (onde New-CodeSigningCert.ps1 o deixou) - sem pedir senha, o que permite rodar automatizado
    a cada publish. Se preferir assinar em outra maquina (que nao gerou o certificado), use
    -PfxPath + a senha do .pfx exportado.

.PARAMETER PublishDir
    Pasta de saida do publish (ex: a mesma do FolderProfile.pubxml).

.PARAMETER Thumbprint
    Thumbprint do certificado em Cert:\CurrentUser\My. Por padrao le de
    scripts\codesigning\..\..\certs\thumbprint.txt (gerado por New-CodeSigningCert.ps1).

.PARAMETER PfxPath
    Alternativa ao -Thumbprint: caminho do .pfx gerado por New-CodeSigningCert.ps1 (pede senha).

.EXAMPLE
    ./Sign-Publish.ps1 -PublishDir "C:\Programas\Estágiaio Palma Seguros"

.EXAMPLE
    ./Sign-Publish.ps1 -PublishDir "C:\Programas\Estágiaio Palma Seguros" -PfxPath ".\certs\AnalistaPalmaseg-CodeSigning.pfx"
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$PublishDir,

    [string]$Thumbprint,

    [string]$PfxPath,

    [string]$FilePattern = "AnalistaPalmaseg*"
)

$ErrorActionPreference = "Stop"

if (-not (Test-Path $PublishDir)) {
    throw "Pasta de publish nao encontrada: $PublishDir"
}

if ($PfxPath) {
    if (-not (Test-Path $PfxPath)) {
        throw "Certificado .pfx nao encontrado: $PfxPath"
    }
    $pfxPassword = Read-Host -Prompt "Senha do .pfx" -AsSecureString
    $cert = Get-PfxCertificate -FilePath $PfxPath -Password $pfxPassword
}
else {
    if (-not $Thumbprint) {
        $thumbprintPath = Join-Path $PSScriptRoot "..\..\certs\thumbprint.txt"
        if (-not (Test-Path $thumbprintPath)) {
            throw "Nenhum -Thumbprint/-PfxPath informado e $thumbprintPath nao existe. Rode New-CodeSigningCert.ps1 primeiro, ou informe o certificado explicitamente."
        }
        $Thumbprint = (Get-Content -Path $thumbprintPath -Raw).Trim()
    }

    $cert = Get-Item -Path "Cert:\CurrentUser\My\$Thumbprint" -ErrorAction SilentlyContinue
    if (-not $cert) {
        throw "Certificado com thumbprint $Thumbprint nao encontrado em Cert:\CurrentUser\My nesta maquina. Rode New-CodeSigningCert.ps1 nesta maquina, ou use -PfxPath."
    }
}

$targets = Get-ChildItem -Path $PublishDir -Recurse -Include "$FilePattern.exe", "$FilePattern.dll"

if ($targets.Count -eq 0) {
    Write-Warning "Nenhum arquivo encontrado com o padrao '$FilePattern' em $PublishDir"
    return
}

Write-Host "Assinando $($targets.Count) arquivo(s) com certificado $($cert.Subject)..." -ForegroundColor Cyan

foreach ($file in $targets) {
    $result = Set-AuthenticodeSignature -FilePath $file.FullName -Certificate $cert -HashAlgorithm SHA256
    if ($result.Status -eq "Valid") {
        Write-Host "  OK  $($file.Name)" -ForegroundColor Green
    } else {
        Write-Warning "  FALHOU ($($result.Status)) $($file.Name): $($result.StatusMessage)"
    }
}

Write-Host ""
Write-Host "Assinatura concluida. Lembre-se: as maquinas so vao confiar nesses arquivos" -ForegroundColor Yellow
Write-Host "depois de importar o certificado publico (.cer) com Import-TrustedCert.ps1."
