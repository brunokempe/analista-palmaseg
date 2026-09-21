# Assinatura de código (self-signed) — AnalistaPalmaseg

Resolve o bloqueio do **Controle de Aplicativo Inteligente (Smart App Control)** do Windows,
que passou a barrar o `.exe` depois de cada nova publicação por falta de reputação/assinatura.

Sem certificado de code signing comprado ainda, a solução aqui é um certificado **self-signed**:
funciona igual a um certificado pago para efeito de confiança do Windows, desde que o certificado
público seja importado em todas as máquinas que rodam o app.

## Passo a passo

### 1. Gerar o certificado (uma única vez)

```powershell
./New-CodeSigningCert.ps1
```

O certificado fica instalado em `Cert:\CurrentUser\My` **na máquina que publica o app** — é isso
que permite assinar automaticamente a cada publish, sem digitar senha. Além disso, gera em `..\..\certs\`:
- `AnalistaPalmaseg-CodeSigning.pfx` — cópia da chave privada (com senha), útil apenas se outra
  máquina/pessoa também for publicar o app. **Não commitar. Guardar em local seguro** (ex: cofre de senhas da empresa).
- `AnalistaPalmaseg-CodeSigning.cer` — chave pública. Deve ser distribuído para todas as máquinas.
- `thumbprint.txt` — identifica o certificado ativo; usado automaticamente pelo publish (não é sensível, pode ser versionado).

### 2. Confiar no certificado em cada máquina (servidor + estações)

Como administrador, em cada máquina:

```powershell
./Import-TrustedCert.ps1 -CerPath "\\caminho\para\AnalistaPalmaseg-CodeSigning.cer"
```

Se as máquinas estiverem em domínio AD, prefira distribuir isso via GPO em vez de rodar
manualmente em cada estação (ver comentário no próprio script).

Isso só precisa ser feito **uma vez por máquina** (ou de novo se o certificado for renovado).

### 3. Publish — a assinatura já é automática

O `FolderProfile.pubxml` tem um target (`SignPublishedFiles`) que roda depois do publish e chama
`Sign-Publish.ps1` sozinho, usando o certificado pelo thumbprint (sem pedir senha). Ou seja, basta
publicar normalmente:

```powershell
dotnet publish src/AnalistaPalmaseg.App -p:PublishProfile=FolderProfile
```

ou usar o botão **Publish** do Visual Studio com o perfil `FolderProfile` — o `.exe` e as DLLs do
app (padrão `AnalistaPalmaseg*`) já saem assinados na pasta de destino. Se o certificado ainda não
tiver sido gerado nessa máquina (passo 1), o target apenas avisa e não quebra o publish.

Depois disso, copie a pasta publicada para o servidor/estações normalmente — os arquivos assinados
com um certificado já confiável na máquina não devem mais ser bloqueados.

Se quiser assinar manualmente (ex: outra máquina que não gerou o certificado, usando o `.pfx`):

```powershell
./Sign-Publish.ps1 -PublishDir "C:\Programas\Estágiaio Palma Seguros" -PfxPath "..\..\certs\AnalistaPalmaseg-CodeSigning.pfx"
```

## Notas

- O certificado é válido por 5 anos (parâmetro `-ValidYears` em `New-CodeSigningCert.ps1`). Quando
  expirar, gere um novo e reimporte em todas as máquinas.
- Isso resolve o Smart App Control/SmartScreen. Não é um certificado emitido por uma CA pública,
  então **fora da rede da empresa** (ex: se algum cliente externo baixar o instalador) o Windows dele
  não vai confiar, pois não terá o `.cer` importado. Para distribuição pública, será necessário um
  certificado de code signing OV/EV de uma CA reconhecida.
- Sempre que possível, mantenha o mesmo certificado entre publicações — trocar de certificado a
  cada build volta a zerar a reputação e reintroduz o problema.
