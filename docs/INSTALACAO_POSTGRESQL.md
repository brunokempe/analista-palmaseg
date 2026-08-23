# Instalação do PostgreSQL e migração do AnalistaPalmaseg

> Passo a passo para instalar o PostgreSQL no servidor Windows da empresa, liberar acesso para as máquinas da rede, criar o banco de dados e migrar os dados existentes do `dados.db` (SQLite) para o Postgres.

## Índice

1. [Pré-requisitos](#1-pré-requisitos)
2. [Instalar o PostgreSQL no servidor Windows](#2-instalar-o-postgresql-no-servidor-windows)
3. [Liberar acesso remoto (rede)](#3-liberar-acesso-remoto-rede)
4. [Liberar a porta no Firewall do Windows](#4-liberar-a-porta-no-firewall-do-windows)
5. [Criar o banco de dados e o usuário do app](#5-criar-o-banco-de-dados-e-o-usuário-do-app)
6. [Testar o acesso remoto](#6-testar-o-acesso-remoto)
7. [Configurar o `appsettings.json` do app](#7-configurar-o-appsettingsjson-do-app)
8. [Criar as tabelas no banco (aplicar as migrations)](#8-criar-as-tabelas-no-banco-aplicar-as-migrations)
9. [Migrar os dados do `dados.db` atual](#9-migrar-os-dados-do-dadosdb-atual)
10. [Distribuir o app atualizado para as máquinas da rede](#10-distribuir-o-app-atualizado-para-as-máquinas-da-rede)
11. [Checklist final](#11-checklist-final)

---

## 1. Pré-requisitos

- Acesso de administrador ao servidor Windows onde o Postgres vai rodar.
- Saber o range de IP da rede local da empresa (ex.: `192.168.1.0/24`), para liberar o acesso só pra rede interna.
- Ter uma cópia de backup do `dados.db` atual antes de começar (ele não é alterado por este processo, mas backup nunca é demais).

## 2. Instalar o PostgreSQL no servidor Windows

1. No servidor, baixe o instalador oficial em:
   ```
   https://www.postgresql.org/download/windows/
   ```
2. Execute o instalador (EnterpriseDB) e siga o wizard:
   - **Senha do superusuário `postgres`**: defina uma senha forte e anote em local seguro — vai ser necessária para os próximos passos.
   - **Porta**: mantenha o padrão `5432`, salvo se já houver algo usando essa porta no servidor.
   - **Stack Builder**: pode desmarcar no final, não é necessário para este projeto.
3. Ao final, o serviço do Windows `postgresql-x64-<versão>` já fica rodando automaticamente. Confirme com:
   ```powershell
   Get-Service postgresql*
   ```

## 3. Liberar acesso remoto (rede)

Por padrão, o Postgres só aceita conexões da própria máquina. Para as outras máquinas da rede conseguirem conectar, edite dois arquivos de configuração (ficam em algo como `C:\Program Files\PostgreSQL\17\data\`, ajuste o número da versão conforme o que foi instalado):

**a) `postgresql.conf`** — encontre a linha `listen_addresses` e altere para:
```
listen_addresses = '*'
```

**b) `pg_hba.conf`** — adicione uma linha no final liberando a rede local (troque `192.168.1.0/24` pelo range real da sua rede):
```
host    all             all             192.168.1.0/24            scram-sha-256
```

**c) Reinicie o serviço** para aplicar as mudanças:
```powershell
Restart-Service postgresql-x64-17
```
> O nome exato do serviço pode variar — confirme com `Get-Service postgresql*` antes de reiniciar.

## 4. Liberar a porta no Firewall do Windows

No servidor, como Administrador, rode no PowerShell:
```powershell
New-NetFirewallRule -DisplayName "PostgreSQL" -Direction Inbound -Protocol TCP -LocalPort 5432 -Action Allow
```

## 5. Criar o banco de dados e o usuário do app

Abra o **SQL Shell (psql)** instalado junto com o Postgres (ou o **pgAdmin**, também incluso), conecte como usuário `postgres` com a senha definida no passo 2, e rode:

```sql
CREATE DATABASE analista_palmaseg;
CREATE USER palmaseg_app WITH PASSWORD 'coloque-uma-senha-forte-aqui';
GRANT ALL PRIVILEGES ON DATABASE analista_palmaseg TO palmaseg_app;
```

Não recomendamos usar o superusuário `postgres` diretamente no app — crie um usuário dedicado (`palmaseg_app`, como acima) com permissão só nesse banco.

## 6. Testar o acesso remoto

De uma máquina qualquer da rede (não o servidor), teste se a porta está acessível:
```powershell
Test-NetConnection -ComputerName <IP-do-servidor> -Port 5432
```
Se retornar `TcpTestSucceeded: True`, a rede está liberada corretamente.

## 7. Configurar o `appsettings.json` do app

Abra `src/AnalistaPalmaseg.App/appsettings.json` e substitua a connection string pelos dados reais:

```json
{
  "ConnectionStrings": {
    "Default": "Host=<IP-do-servidor>;Port=5432;Database=analista_palmaseg;Username=palmaseg_app;Password=<senha-definida-no-passo-5>"
  }
}
```

Esse mesmo arquivo (já compilado dentro da pasta de output do app) é o que deve ser copiado para todas as máquinas — assim todas apontam pro mesmo servidor central.

## 8. Criar as tabelas no banco (aplicar as migrations)

Numa máquina com o repositório e o .NET SDK instalados (não precisa ser o servidor), com o `appsettings.json` já configurado, rode a partir da raiz do projeto:

```bash
dotnet ef database update --project src/AnalistaPalmaseg.Core --startup-project src/AnalistaPalmaseg.Core
```

Isso cria todas as tabelas no banco `analista_palmaseg`, seguindo a migration `InitialCreate` do projeto.

> Se o comando `dotnet ef` não for reconhecido, instale a ferramenta uma vez com:
> ```bash
> dotnet tool install --global dotnet-ef
> ```

## 9. Migrar os dados do `dados.db` atual

Com o schema criado, rode a ferramenta de migração de dados (`AnalistaPalmaseg.MigrationTool`) passando o caminho do `dados.db` de produção e a connection string do Postgres:

```bash
dotnet run --project src/AnalistaPalmaseg.MigrationTool -- "C:\caminho\para\dados.db" "Host=<IP-do-servidor>;Port=5432;Database=analista_palmaseg;Username=palmaseg_app;Password=<senha>"
```

A ferramenta copia todas as tabelas na ordem correta (respeitando as dependências entre elas) e ajusta a numeração dos IDs no final. Ela pode ser rodada mais de uma vez com segurança — cada execução limpa e recopia as tabelas de destino a partir do `dados.db` de origem.

## 10. Distribuir o app atualizado para as máquinas da rede

1. Compile o app (`dotnet publish src/AnalistaPalmaseg.App`) ou copie a pasta de output já compilada.
2. Garanta que o `appsettings.json` publicado em cada máquina tem a mesma connection string do passo 7, apontando pro servidor central.
3. Distribua essa mesma pasta/instalação para todas as máquinas, criando os atalhos normalmente — como agora todas usam o mesmo banco Postgres, o uso simultâneo em várias máquinas passa a funcionar corretamente, sem risco de corrupção ou conflito de lock que existia com o SQLite local.

## 11. Checklist final

- [ ] PostgreSQL instalado e rodando no servidor Windows
- [ ] `listen_addresses` e `pg_hba.conf` configurados para aceitar conexões da rede local
- [ ] Porta 5432 liberada no Firewall do Windows
- [ ] Banco `analista_palmaseg` e usuário `palmaseg_app` criados
- [ ] Teste de conexão remota (`Test-NetConnection`) bem-sucedido
- [ ] `appsettings.json` do app com a connection string real
- [ ] `dotnet ef database update` executado sem erros
- [ ] Dados migrados do `dados.db` via `AnalistaPalmaseg.MigrationTool`
- [ ] App distribuído e testado em pelo menos duas máquinas simultaneamente
