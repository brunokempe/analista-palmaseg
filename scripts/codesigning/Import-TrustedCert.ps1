<#
.SYNOPSIS
    Importa o certificado publico (.cer) do AnalistaPalmaseg como confiavel na maquina local,
    para que o Windows (Smart App Control / SmartScreen) pare de bloquear os binarios assinados.

.DESCRIPTION
    Precisa ser executado COMO ADMINISTRADOR em cada maquina (servidor e todas as estacoes).
    Importa o certificado em dois repositorios da maquina local:
      - Trusted Root Certification Authorities (Cert:\LocalMachine\Root)
      - Trusted Publishers            (Cert:\LocalMachine\TrustedPublisher)

    Como o certificado eh self-signed (nao emitido por uma CA publica), ele so passa a ser
    confiavel depois desse import - sem isso, mesmo um binario assinado continua sendo tratado
    como nao confiavel.

    Se as maquinas estiverem em dominio Active Directory, o ideal e distribuir esse mesmo .cer
    via GPO (Configuracao do Computador > Politicas > Configuracoes do Windows > Configuracoes
    de Seguranca > Politicas de Chave Publica > Trusted Publishers / Trusted Root CA) em vez de
    rodar este script manualmente em cada estacao.

.PARAMETER CerPath
    Caminho do arquivo .cer gerado por New-CodeSigningCert.ps1.

.EXAMPLE
    ./Import-TrustedCert.ps1 -CerPath ".\certs\AnalistaPalmaseg-CodeSigning.cer"
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$CerPath
)

$ErrorActionPreference = "Stop"

$isAdmin = ([Security.Principal.WindowsPrincipal][Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltinRole]::Administrator)
if (-not $isAdmin) {
    throw "Rode este script como Administrador (clique com botao direito no PowerShell > Executar como administrador)."
}

if (-not (Test-Path $CerPath)) {
    throw "Arquivo .cer nao encontrado: $CerPath"
}

$rootOk = $false
$publisherOk = $false

Write-Host "Importando certificado em Trusted Root Certification Authorities..." -ForegroundColor Cyan
try {
    Import-Certificate -FilePath $CerPath -CertStoreLocation "Cert:\LocalMachine\Root" -ErrorAction Stop | Out-Null
    $rootOk = $true
}
catch {
    Write-Warning "Falhou ao importar em Trusted Root: $($_.Exception.Message)"
}

Write-Host "Importando certificado em Trusted Publishers..." -ForegroundColor Cyan
try {
    Import-Certificate -FilePath $CerPath -CertStoreLocation "Cert:\LocalMachine\TrustedPublisher" -ErrorAction Stop | Out-Null
    $publisherOk = $true
}
catch {
    # Alguns antivirus/EDR bloqueiam escrita neste repositorio especifico (alvo comum de malware),
    # mesmo com elevacao de administrador. Nao eh fatal: o Trusted Root ja costuma bastar para o
    # Smart App Control validar a cadeia de confianca.
    Write-Warning "Falhou ao importar em Trusted Publishers (comum quando ha antivirus/EDR protegendo esse repositorio): $($_.Exception.Message)"
}

Write-Host ""
if ($rootOk) {
    Write-Host "Certificado importado em Trusted Root nesta maquina." -ForegroundColor Green
    Write-Host "Arquivos assinados com esse certificado devem ser aceitos pelo Windows a partir de agora."
    if (-not $publisherOk) {
        Write-Host "(Trusted Publishers nao foi importado - normalmente nao impede o funcionamento, mas se o bloqueio persistir, verifique o antivirus/EDR da maquina.)" -ForegroundColor Yellow
    }
}
else {
    throw "Nao foi possivel importar o certificado em nenhum repositorio nesta maquina. Verifique permissoes/antivirus."
}
