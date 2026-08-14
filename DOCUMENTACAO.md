# Documentação Técnica — AnalistaPalmaseg

> Documento gerado a partir de levantamento do código-fonte em 2026-08-14. Cobre arquitetura, telas, modelos de domínio, regras de negócio e integrações do sistema.

## Índice

1. [Visão geral e estrutura do projeto](#1-visão-geral-e-estrutura-do-projeto)
2. [Arquitetura](#2-arquitetura)
3. [Telas (Views) e ViewModels](#3-telas-views-e-viewmodels)
4. [Modelos de domínio](#4-modelos-de-domínio)
5. [Regras de negócio](#5-regras-de-negócio)
6. [Serviços e integrações externas](#6-serviços-e-integrações-externas)
7. [Configuração e build](#7-configuração-e-build)

---

## 1. Visão geral e estrutura do projeto

O repositório (`AnalistaPalmaseg.slnx`, formato moderno de solução .NET) contém **dois projetos**, ambos em `net10.0`, com separação clara entre apresentação e domínio:

### `src/AnalistaPalmaseg.App` — WPF (`net10.0-windows`, `WinExe`, assembly `AnalistaPalmaseg`)

Camada de apresentação (UI/MVVM):

- `App.xaml` / `App.xaml.cs` — bootstrap da aplicação, configuração de DI, host genérico.
- `Messages.cs` — mensagens do sistema de mensageria (`CommunityToolkit.Mvvm.Messaging`).
- `ViewModels/` — 29 ViewModels de tela + ViewModels auxiliares (`PeriodoRenovacoesVm`, `PeriodoNegociosVm`, `PeriodoPendentesVm`, `PeriodoFuncionarioVm`).
- `Views/` — janelas e páginas XAML.
- `Converters/`, `Styles/`, `Resources/` — recursos visuais (tema Material Design).

Pacotes NuGet principais:
- `CommunityToolkit.Mvvm` 8.4.2
- `LiveChartsCore.SkiaSharpView.WPF` 2.0.5 (gráficos)
- `MaterialDesignThemes` 5.3.2 (tema visual)
- `Microsoft.Extensions.Hosting` 10.0.10 (host genérico / DI)

### `src/AnalistaPalmaseg.Core` — Class Library (`net10.0`)

Camada de domínio e infraestrutura, sem dependência de WPF:

- `Models/` — 19 entidades de domínio (POCOs).
- `Data/` — `AppDbContext` (EF Core) e `DatabaseInitializer`.
- `Services/` — 14 serviços de negócio/infraestrutura (importação, cálculo de metas, relatórios, autenticação, geração de documentos, integração LibreOffice).

Pacotes NuGet principais:
- `ExcelDataReader.DataSet` 3.9.0 (leitura de planilhas)
- `Microsoft.EntityFrameworkCore.Sqlite` 10.0.10
- `Microsoft.EntityFrameworkCore.Design` 10.0.10

### Relação entre projetos

`App` → `Core` (referência unidirecional via `ProjectReference`). O Core não conhece nada de WPF; toda lógica de negócio, acesso a dados e integrações externas está isolada nele — separação limpa típica de MVVM com camada de domínio compartilhável.

O histórico de commits indica origem recente do projeto (nomeado inicialmente "Gestor Palma Seguros", depois "Analista Palma Seg"), com evolução recente incluindo suporte a arquivos ODS criptografados via LibreOffice UNO.

---

## 2. Arquitetura

### Padrão MVVM

O projeto segue rigorosamente **MVVM** usando o **CommunityToolkit.Mvvm** (source generators via atributos `[ObservableProperty]` e `[RelayCommand]`, além de `partial void On<Prop>Changed`). Todas as ViewModels herdam de `ObservableObject` e são `partial class`.

- Cada tela é um `UserControl` XAML (`XxxView.xaml`) associado a uma ViewModel via `DataContext`.
- Duas janelas reais: `LoginWindow` (modal, autenticação) e `MainWindow` (janela principal com shell/sidebar).
- Diálogos modais: `SenhaDialog`, `MotivoSituacaoDialog`, `FechamentoRenPalmaDialog`.

### Injeção de dependência (`App.xaml.cs`)

Usa `Microsoft.Extensions.Hosting` (`Host.CreateDefaultBuilder()...Build()`), host genérico configurado em `OnStartup`:

- `AppDbContext` registrado via `AddDbContext` com SQLite, arquivo `dados.db` no diretório base da aplicação (`AppDomain.CurrentDomain.BaseDirectory`).
- `SessaoService` como **Singleton** (guarda o usuário logado durante toda a sessão).
- Todos os demais serviços (`ImportacaoService`, `RelatorioService`, `ApoliceService`, etc.) e **todas** as ViewModels são registrados como **Transient**.

Fluxo de inicialização:
1. `host.StartAsync()`
2. `DatabaseInitializer.Initialize()` — cria/migra schema SQLite
3. Exibe `LoginWindow` como diálogo modal
4. Se login OK: resolve `MainWindow`/`MainViewModel`, registra `DataTemplates` (mapeamento ViewModel→View), pré-carrega dados de todas as telas de "Análise de Carteira" (`CarregarAsync()` em cascata) antes de exibir a janela principal.

`ShutdownMode` é manipulado manualmente: `OnExplicitShutdown` durante o login (para não fechar o app se a janela de login fechar), depois `OnMainWindowClose` quando a janela principal é aberta. Cultura forçada para `pt-BR` (formatação de moeda `R$` via `StringFormat=C2`).

### Navegação entre telas

Não há `NavigationService` dedicado — a navegação é feita inteiramente pela **`MainViewModel`**, que:

- Mantém referências a **todas** as ViewModels de tela como propriedades (`InicioVm`, `DashboardVm`, `RenovacoesVm`, etc. — cerca de 20 propriedades).
- Expõe uma propriedade `CurrentView` (`ObservableObject?`) atualizada por comandos `Nav*` (ex.: `NavDashboard`, `NavClientesAsync`), muitos assíncronos, que chamam `VmAlvo.CarregarAsync()` antes de trocar a view (garante dados atualizados a cada navegação).
- `DataTemplate`s registrados dinamicamente em `RegisterDataTemplates()` mapeiam cada tipo de ViewModel à sua View (`ContentControl` com `Content="{Binding CurrentView}"`).
- Sidebar colapsável com seções expansíveis (Carteira, Relatórios, Apólices, Cadastros, Leads, Gerenciador, Comparativo), cada uma com flag `IsXxxExpanded` e visibilidade calculada.

**Mensageria** (`Messages.cs`): define `DashboardRefreshMessage(int Mes, int Ano)`, enviada via `WeakReferenceMessenger.Default.Send(...)` quando uma ação em uma tela deve provocar recálculo em outra (ex.: ao fechar uma "Ren. Palma" ou salvar um Seguro Novo, `DashboardMetasViewModel` — registrado como `Register<DashboardRefreshMessage>` no construtor — recalcula métricas se o mês/ano batem).

### Banco de dados

- **SQLite** via EF Core, arquivo `dados.db` criado ao lado do executável.
- **Sem migrations formais do EF** — o schema é gerido de forma **imperativa e idempotente** em `DatabaseInitializer.Initialize()`:
  - `context.Database.EnsureCreated()` cria o schema inicial a partir do `OnModelCreating`.
  - Sequência de `ExecuteSqlRaw("CREATE TABLE IF NOT EXISTS ...")`, `CREATE INDEX IF NOT EXISTS`, `ALTER TABLE ... ADD COLUMN` (verificando `pragma_table_info` antes) garante que bancos já existentes recebam novas tabelas/colunas sem perda de dados — estratégia manual de "migração incremental" adequada a uma aplicação desktop de arquivo único.
  - **Seed data**: seguradoras parceiras padrão (Porto, Unimed, Hdi, Allianz, Tokio, Zurich, Bradesco, Azul + "Demais"), regras de premiação por quantidade de seguradoras atingidas, metas de crescimento padrão, e um usuário administrador padrão (`admin` / `admin123`, hash SHA-256) se não houver nenhum usuário.
- `OnModelCreating` define tipos de coluna decimal (`decimal(18,2)`, `decimal(10,4)`), índices únicos (ex.: `Importacao(Produtor,Mes,Ano)`, `RelatorioRenovacao(Proposta)`, `MetaSeguradora(SeguradoraId,Mes,Ano)`, `ValorReferencia(Colaborador,Mes,Ano)`) e ignora propriedades calculadas (`[NotMapped]`/`.Ignore(...)`).
- 19 `DbSet`s expostos no contexto, cobrindo toda a superfície de dados do app (ver seção 4).

---

## 3. Telas (Views) e ViewModels

A `MainViewModel` organiza as telas em grupos lógicos de sidebar: **Análise de Carteira**, **Acompanhamento de Apólices**, **Dashboard de Funcionários**, **Admin**, **Seguros Novos**, **Relatórios**, **Gerenciador (admin)**, **Controle de Boletos**, **Clientes**, **Leads**, **Metas (admin)**.

| Tela / ViewModel | Função |
|---|---|
| **LoginViewModel** (`LoginWindow`) | Autenticação. Recebe `Login`/`Senha`, chama `UsuarioService.AutenticarAsync` (hash SHA-256), popula `SessaoService.Iniciar(usuario)` em caso de sucesso e dispara evento `LoginSucesso`. Expõe `MensagemErro` e `IsProcessando`. |
| **InicioViewModel** (`InicioView`) | Tela "Home" com cards de resumo agregando: contadores por `SituacaoAcompanhamento` das renovações (À Renovar, Ren. Palma, Emitido/Ren.Outro, Outros) e estatísticas de emissão (total Ren. Palma, prêmio total, assinaturas/emissões pendentes), via `RelatorioRenovacaoService.GetContadorSituacoesAsync()` e `GetRenPalmaStatsAsync()`. |
| **DashboardViewModel** (`DashboardView`) | Dashboard de "Análise de Carteira" por produtor/período, via `RelatorioService.GetResumoAsync()` (lista de `ResumoImportacao`). Gráfico de pizza (Ren.Palma/Pendentes/Não renovado) e gráfico de barras (participação por seguradora) com LiveCharts. |
| **RenovacoesViewModel** (`RenovacoesView`) | Timeline de renovações por produtor (`GetTimelineRenovacoesAsync`), filtro por texto e status (Todos, Ren.Palma, Procurado, Pendente, Agendado, Não renovado). Usa `PeriodoRenovacoesVm` por período. |
| **NovosNegociosViewModel** (`NovosNegociosView`) | Análogo, para `NovoNegocio` (status: Novo/Renovação/Prospecção/Mercado), via `PeriodoNegociosVm`. |
| **PendentesViewModel** (`PendentesView`) | Timeline de renovações pendentes (Procurado/Pendente/Agendado) por produtor, via `PeriodoPendentesVm`. |
| **RetencaoViewModel** (`RetencaoView`) | Gráfico de linha (LiveCharts) da evolução do percentual de retenção (`renovadasPalma/totalVencidas × 100`) por período, por produtor. |
| **ComparacaoViewModel** (`ComparacaoView`) | Tabela comparativa de todos os `ResumoImportacao` (todos produtores/períodos lado a lado). |
| **ResultadosViewModel** (`ResultadosView`) | Lista `ResultadoMeta` (meta × realizado por seguradora) de uma importação selecionada, com percentual de atingimento. |
| **ApolicesDashboardViewModel** (`ApolicesDashboardView`) | Dashboard de apólices importadas, classifica cada `Apolice` por `StatusLabel` (Vencida/Próxima/Em dia, baseado em `DiasParaVencimento`). |
| **FuncionariosDashboardViewModel** (`FuncionariosDashboardView`) | Timeline por funcionário de `FuncionarioResultado` (prêmio, meta, comissão, % comissão por seguradora), via `PeriodoFuncionarioVm`. |
| **GerenciarUsuariosViewModel** (`GerenciarUsuariosView`, admin) | CRUD de `Usuario`: adicionar, alterar senha (`SenhaDialog`), ativar/desativar, remover — com proteção contra auto-desativação/remoção. |
| **AcompanhamentoRenovacoesViewModel** (`AcompanhamentoRenovacoesView`) | Tela central de trabalho sobre `RelatorioRenovacao` importado. Agrupamento por `NomeCliente`, ordenação customizada, filtro por texto/situação/produtor/mês (debounce 300ms). Ao mudar `SituacaoAcompanhamento` para "Ren. Palma" abre `FechamentoRenPalmaDialog`; para status que exigem motivo, abre `MotivoSituacaoDialog` (cancelar reverte). Gera "Folha Amarela" (`.odt`) e gerencia anexos por registro. |
| **GerenciadorRenovacoesViewModel** (`GerenciadorRenovacoesView`, admin) | Visão administrativa de todos os `RelatorioRenovacao`, filtros combináveis (status, seguradora, vendedor, produtor, mês, texto), atribuição de "novo produtor" em massa, geração de folhas amarelas em lote, anexos, edição inline. |
| **GerenciadorCotacoesViewModel** (`GerenciadorCotacoesView`) | Placeholder — "Módulo de cotações em desenvolvimento", não implementado. |
| **EmissaoDashboardViewModel** (`EmissaoDashboardView`, gerenciador/admin) | Dashboard de emissões por período, combinando `RelatorioRenovacao` (situação "Ren. Palma") e `SeguroNovo`. Cards (total, prêmio, comissão, pendências), resumo por produtor (`ProdutorEmissaoResumo`), aplica regra de comissão de colaborador (seção 5). Permite alternar `AssinaturaFeita`/`SeguroEmitido` na grade. |
| **SeguroNovosViewModel** (`SeguroNovosView`) | CRUD de `SeguroNovo` (negócios novos/endossos), formulário completo. Filtra por produtor quando não-admin. Ao salvar, envia `DashboardRefreshMessage`. |
| **RelatorioEmissaoViewModel** (`RelatorioEmissaoView`) | Relatório anual agregado (ano selecionável) de emissões por produtor×mês, unindo renovações emitidas e seguros novos. |
| **DefinicoesMetasViewModel** (`DefinicoesMetasView`, admin) | Configuração de metas: CRUD de `Seguradora` + `MetaSeguradora`, `MetaPremiacao` (bônus por N seguradoras/todas) e `MetaCrescimento` (bônus por % crescimento). |
| **DashboardMetasViewModel** (`DashboardMetasView`) | Tela mais complexa: posição de metas/bônus do colaborador (ou "Todos" para admin) em determinado mês/ano — referência do ano anterior, posição atual, percentuais de crescimento, comissão do colaborador, alcance por seguradora, bônus de premiação e de crescimento. Reage a `DashboardRefreshMessage`. |
| **ControleBoletosViewModel** (`ControleBoletosView`) | Unifica `SeguroNovo` e `RelatorioRenovacao` (Ren. Palma) com pagamento via boleto; calcula parcelas esperadas vs. `BoletosGerados`, incrementar/decrementar, filtro "apenas pendentes". |
| **ClientesViewModel** (`ClientesView`) | CRUD de `Cliente` (dados cadastrais amplos), busca (nome/CPF/cidade), histórico de seguros do cliente (via `DocumentoPrincipal`). |
| **LeadsViewModel** (`LeadsView`) | CRUD de `Lead`. Ao marcar "Fechou" sem `SeguroNovoId`, cria automaticamente um `SeguroNovo` (status "Novo") vinculado e dispara `DashboardRefreshMessage`. |
| **DistribuicaoProdutorViewModel** (`DistribuicaoProdutorView`, "Gerenciador") | Análise multidimensional de `RelatorioRenovacao` com `NovoProdutor` definido: agregações por produtor, por dia de vencimento e por ramo, com filtros e referência editável do ano anterior. |
| **MainViewModel** | Shell da aplicação — sidebar, navegação, orquestração de todas as ViewModels de tela. |

VMs auxiliares (não são telas próprias): `PeriodoRenovacoesVm`, `PeriodoNegociosVm`, `PeriodoPendentesVm`, `PeriodoFuncionarioVm` — encapsulam um `ResumoImportacao` + lista filtrável localmente, usados pelas timelines acima.

---

## 4. Modelos de domínio

Namespace `AnalistaPalmaseg.Core.Models`:

- **`Usuario`**: `Id, Login, SenhaHash (SHA-256), TipoAcesso (enum Colaborador/Administrador), Ativo`.
- **`Importacao`**: representa uma planilha ODS/XLSX de produtor importada — `Produtor, Mes, Ano, ImportadoEm, ArquivoOrigem` + coleções `Renovacoes`, `NovosNegocios`, `Resultados`, `FuncionariosResultados`. Índice único (Produtor,Mes,Ano).
- **`Renovacao`**: linha da aba "Ren" da planilha de produtor — `Vigencia, Segurado, Cia, Ramo, PlBase, Fator, Comissao, Status, CiaRenovada, NovoPl, NovaComissao, SaldoPl, EmitidoPor, Observacao`. Calculados: `IsRenovado` (Status ∈ {Ren.Palma, Ren.Outro}), `IsPendente` (Status ∈ {Procurado, Pendente, Agendado}).
- **`NovoNegocio`**: aba "Novos" — `Vigencia, Segurado, Cia, Segmento, Status, Pl, Fator, Comissao, Observacao, EmitidoPor`. `IsNovo/IsRenovacao/IsProspeccao` conforme `Status`.
- **`ResultadoMeta`**: aba "Participação/Resultado" — meta × realizado por seguradora. `BateuMeta`, `PercentualAtingimento`.
- **`FuncionarioResultado`**: por colaborador × seguradora — `Premio, Meta, Comissao, PercentualComissao`. `PercentualAtingimento` calculado (ignorado no EF).
- **`ImportacaoApolice`** / **`Apolice`**: importação separada de apólices (planilha genérica, detecção heurística de colunas). `Apolice` tem `DiasParaVencimento`, `StatusLabel` (Vencida/Próxima ≤30d/Em dia) e `DiasLabel` calculados em runtime.
- **`RelatorioRenovacao`**: **entidade mais rica do sistema (~90 campos)**, reflete o layout do relatório gerencial de renovações. Campos: proposta/apólice, datas, financeiro (comissão, prêmio líquido/total, IOF, parcelas), seguro (seguradora, ramo, vendedor), cliente completo (nome, nascimento, documentos, contato, endereço, banco), dados de veículo (auto), coberturas detalhadas (danos materiais/morais/corporais, acidentes de passageiro). Implementa `INotifyPropertyChanged` manualmente (não usa o toolkit) para grade editável de alta performance. Campos de acompanhamento administrativo adicionados incrementalmente: `NovoProdutor, MotivoSituacao, SituacaoAcompanhamento` (máquina de estados, seção 5), campos `Fechamento*`, `AssinaturaFeita, SeguroEmitido, EmitidoPor, BoletosGerados`. Calculados: `RenovacaoRealizada`, `ComissaoValor` (prêmio × comissão/100), `ComissaoColab`, `DiaDaSemana`.
- **`Anexo`**: arquivo anexado a um `RelatorioRenovacao` (nome, caminho físico, tamanho, data).
- **`SeguroNovo`**: cadastro manual de negócio novo/endosso — `Vigencia, Segurado, Cia, Segmento, Status, Financeiro, Pl, Fator, Valor, FormaPagamento, Parcelas, AssinaturaFeita, BoletosGerados, CriadoPor, EmitidoPor`. `ComissaoValor` = Valor × Fator/100; `PercentualComissaoColabEfetivo` tem fallback por Status (Prospecção=15%, Endosso=0%, demais=10%) se não sobrescrito.
- **`Seguradora`**: `Nome, IsParceira, Ativo` — parceiras têm regra de comissão diferenciada.
- **`MetaSeguradora`**: meta de prêmio mensal por seguradora×mês×ano (índice único).
- **`MetaPremiacao`**: bônus por atingir N seguradoras (`QuantidadeMinima`) ou todas (`EhTodas`) → `ValorBonus`, ordenável.
- **`MetaCrescimento`**: bônus por % de crescimento (`Tipo` Premio/Comissao, `PercentualMeta`, `ValorBonus`, `EhEquipe`).
- **`ValorReferencia`**: prêmio/comissão total do ano anterior por colaborador×mês×ano (base de comparação de crescimento).
- **`Cliente`**: cadastro amplo (identificação, contato, endereço, notas manuais `Observacoes`/`Historico`), sincronizado automaticamente a partir do `RelatorioRenovacao` importado (por CPF/`DocumentoPrincipal`).
- **`Lead`**: prospecção — `Segurado, Produtor, Indicacao, Observacao, SeguroGerado, Fechou, FechouEm, SeguroNovoId` (vínculo opcional a `SeguroNovo` gerado ao fechar).
- **`DistribuicaoReferencia`**: valores de referência do ano anterior por ano (prêmio, comissão, qtd apólices) para a tela de Distribuição por Produtor.

---

## 5. Regras de negócio

### Máquina de estados de `SituacaoAcompanhamento` (RelatorioRenovacao)

Valores possíveis: `À Renovar, Agendado, Calculado, Procurado, Ren. Palma, Ren. Outro, Não renovado, Recusado, Emitido`.

- `RenovacaoRealizada` = true quando situação ∈ {Emitido, Ren. Palma, Ren. Outro}.
- Transição para **Ren. Palma** dispara obrigatoriamente `FechamentoRenPalmaDialog` (seguradora final, prêmio líquido, forma de pagamento, comissão, parcelamento) — cancelar reverte o status.
- Transição para {Agendado, Ren. Outro, Não renovado, Recusado} exige preenchimento de `MotivoSituacao` via `MotivoSituacaoDialog` — cancelar reverte.
- Migração de bancos antigos: `DatabaseInitializer` converte `RenovacaoRealizada=1` → `SituacaoAcompanhamento='Emitido'`.

### Status de Renovação/Novo Negócio (planilha do produtor)

- `Renovacao.Status`: "Ren.Palma"/"Ren.Outro" = renovado; "Procurado"/"Pendente"/"Agendado" = pendente; "Não renov"/"Não renovado" = perdido.
- Retenção = `renovadasPalma / totalVencidas × 100` (calculado por período em `RelatorioService.CalcularResumo`).
- `NovoNegocio.Status` (case-insensitive): "novo", "renovação", "prospecção".

### Regra de comissão de colaborador

Aplicada em `MetaService`, `EmissaoDashboardViewModel` e indiretamente `DashboardMetasViewModel`. Percentual pago ao colaborador sobre a comissão da corretora em Ren. Palma / Endosso, condicionado a **seguradora parceira** × **atingimento de meta de prêmio da seguradora no mês**:

| Seguradora | Atingiu meta | % Colaborador |
|---|---|---|
| Parceira | Sim | 6% |
| Parceira | Não | 4% |
| Não-parceira | Sim | 4% |
| Não-parceira | Não | 3% |

Para Seguro Novo (não Endosso): Prospecção = 15%, demais status = 10%.

Comissão da corretora = `PremioLíquido × %Comissao / 100` (arredondado a 2 casas). Comissão do colaborador = `ComissãoCorretora × %Colab / 100`.

### Metas e bônus (DashboardMetasViewModel)

- Crescimento de prêmio/comissão comparado ao ano anterior (`ValorReferencia`): `CrescimentoPremio = (Atual - Ref) / Ref`. Faixas fixas no seed: +10%/+15% (prêmio) e +15%/+20% (comissão), cada faixa com bônus configurável (`MetaCrescimento`), aplicando sempre a **maior faixa atingida**.
- Bônus por quantidade de seguradoras parceiras "atingidas" (meta de prêmio batida): regras configuráveis (ex.: ≥3, ≥6, todas) somam-se (`BonusPremiacao`).
- `TotalGeralColab = ComissaoColabTotal + BonusTotal` (comissão + todos os bônus).
- Resolução de "seguradora" a partir de texto livre (nome digitado na planilha) usa matching parcial case-insensitive bidirecional (`Contains`) contra a tabela `Seguradora`.

### Apólices (Dashboard de Apólices)

`DiasParaVencimento = DataVencimentoPagamento - hoje`. `StatusLabel`: `< 0` → "Vencida"; `≤ 30` → "Próxima"; senão "Em dia". Nova importação **substitui totalmente** as apólices anteriores (`ApoliceService.ImportarAsync` remove `ImportacoesApolice` antigas antes de inserir).

### Deduplicação e preservação de edições manuais na reimportação

- `ImportacaoService`: ao reimportar planilha de um produtor para o mesmo mês/ano (comparação por nome normalizado sem acentos), **remove tudo** e recria (Renovacoes, NovosNegocios, Resultados, FuncionariosResultados).
- `RelatorioRenovacaoService.ImportarAsync`: dedup por `Proposta` (chave de negócio). Ao reimportar um registro já existente, **preserva campos editados manualmente** (`NovoProdutor`, `MotivoSituacao`, `Observacao`, `SituacaoAcompanhamento`, todos os campos `Fechamento*`, `AssinaturaFeita`, `SeguroEmitido`), sobrescrevendo apenas os demais campos vindos da planilha — evita perder trabalho de acompanhamento ao reimportar dados atualizados da seguradora.

### Controle de boletos

Número de parcelas esperado vem de `FechamentoParcelamento` (texto livre, extrai dígitos; "À Vista" etc. sem dígitos → 1 parcela) ou `NumeroParcelas`; `BoletosGerados` é incrementado/decrementado manualmente pelo usuário; item "completo" quando `BoletosGerados >= Parcelas`.

### Sincronização automática de Clientes

A cada importação de relatório de renovações, `ClienteService.SincronizarClientesAsync` faz upsert de `Cliente` por CPF (`DocumentoPrincipal`), atualizando dados cadastrais vindos da planilha mas **preservando** `Observacoes`/`Historico` (notas manuais).

---

## 6. Serviços e integrações externas

- **`LibreOfficeDecryptorService`**: integração com **LibreOffice** via bridge UNO + Python embutido (`C:\Program Files\LibreOffice\program\python.exe` e `soffice.exe`). Detecta ODS criptografados (checando entry `encrypted-package` no ZIP), inicia o LibreOffice em modo headless com socket UNO na porta 2002, executa script Python gerado dinamicamente que abre o arquivo com senha via UNO e reexporta como XLSX sem senha (`storeToURL`, filtro `Calc MS Excel 2007 XML`). Usa arquivo de senha temporário (evita expor senha na linha de comando) e mata processos `soffice`/`soffice.bin` residuais + lock de perfil ao final. Consumido por `ImportacaoService` ao importar `.ods` protegido.
- **`ImportacaoService`**: parser de planilhas Excel/ODS (via `ExcelDataReader`) das planilhas mensais por produtor, com parsing posicional (linhas/colunas fixas) das abas "Ren", "Novos" e "Resultado(s)/Participação", incluindo heurística para localizar linhas-chave ("PL Vendido", "PL Meta", "Média" com taxa de comissão) e extrair metadados (produtor, mês/ano) do nome do arquivo ou do conteúdo.
- **`ApoliceService`**: importador genérico de apólices com detecção automática de cabeçalho (varre as primeiras 10 linhas por palavras-chave) e mapeamento fuzzy de colunas por múltiplos sinônimos.
- **`RelatorioRenovacaoService`**: importador do relatório gerencial oficial (mapeamento de ~90 colunas nomeadas por cabeçalho exato), com parsing robusto de tipos (`Dt`, `Dec`, `Num` — `DateTime`, `double`/OADate, string).
- **`FolhaAmarelaService`**: gera documentos **ODT** (OpenDocument Text) programaticamente, montando o XML (`content.xml`, `styles.xml`, `manifest.xml`) dentro de um ZIP — usado para imprimir a "Folha Amarela" (ficha resumo do cliente/apólice para arquivamento físico). Suporta template `.odt` pré-existente (localizado em `%LocalAppData%\AnalistaPalmaseg\FolhaTemplate.odt` ou copiado automaticamente de Desktop/Downloads na primeira execução) e geração em lote com progresso reportado.
- **`AnexoService`**: gerencia anexos de arquivo por registro de renovação, copiando fisicamente para `%BaseDir%\Anexos\{relatorioId}\`.
- **Autenticação** (`UsuarioService`): hash SHA-256 simples (sem salt), autenticação local via SQLite, dois níveis de acesso (`Colaborador`/`Administrador`) controlando visibilidade de várias telas (ex.: `SeguroNovosViewModel` só mostra registros do próprio usuário se não-admin).
- **Exportação/relatórios**: não há exportação para Excel/PDF explícita; a "exportação" principal é a geração de pastas com Folha Amarela + anexos, abertas via `Process.Start` (Explorer/aplicativo padrão).
- **Diálogos de arquivo do Windows** (`Microsoft.Win32.OpenFileDialog`/`OpenFolderDialog`) usados para seleção de planilhas a importar e pasta de destino em lote.

---

## 7. Configuração e build

- **Target Framework**: `net10.0-windows` (App, WPF) e `net10.0` (Core) — requer .NET 10 SDK.
- **Nullable reference types** habilitado em ambos os projetos; `ImplicitUsings` habilitado.
- **Sem `appsettings.json`** — não há arquivo de configuração externo; a única "configuração" é o caminho fixo do banco SQLite (`dados.db` ao lado do executável) e caminhos hardcoded do LibreOffice (`C:\Program Files\LibreOffice\program\...`).
- **`.gitignore`**: ignora `bin/`, `obj/`, `*.user`, `.vs/`, `*.suo`, `%LOCALAPPDATA%/AnalistaPalmaseg/` (pasta de dados do usuário, incluindo template de Folha Amarela e anexos de Seguros Novos).
- **Build/execução**: `dotnet build` na raiz (usa `AnalistaPalmaseg.slnx`) ou abrir no Visual Studio 2022+ (há pasta `.vs/` e `PublishProfiles` em `Properties/`, indicando publish configurado).
- **Sem testes automatizados** — não foram encontrados projetos de teste na solução.
- Dependência externa opcional: **LibreOffice instalado em `C:\Program Files\LibreOffice`** é pré-requisito apenas para importar planilhas `.ods` protegidas por senha; sem ele, a importação de ODS não criptografados e XLSX funciona normalmente via `ExcelDataReader`.

```bash
dotnet build
dotnet run --project src/AnalistaPalmaseg.App
```
