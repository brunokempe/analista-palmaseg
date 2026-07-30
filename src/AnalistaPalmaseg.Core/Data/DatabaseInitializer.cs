using AnalistaPalmaseg.Core.Services;
using Microsoft.EntityFrameworkCore;

namespace AnalistaPalmaseg.Core.Data;

public class DatabaseInitializer(AppDbContext context)
{
    public void Initialize()
    {
        context.Database.EnsureCreated();

        // Idempotent upgrades for tables added after the initial EnsureCreated
        context.Database.ExecuteSqlRaw("""
            CREATE TABLE IF NOT EXISTS "Resultados" (
                "Id"            INTEGER NOT NULL CONSTRAINT "PK_Resultados" PRIMARY KEY AUTOINCREMENT,
                "ImportacaoId"  INTEGER NOT NULL,
                "Funcionario"   TEXT    NOT NULL,
                "Meta"          TEXT    NOT NULL,
                "Realizado"     TEXT    NOT NULL,
                CONSTRAINT "FK_Resultados_Importacoes_ImportacaoId"
                    FOREIGN KEY ("ImportacaoId") REFERENCES "Importacoes" ("Id") ON DELETE CASCADE
            )
            """);

        context.Database.ExecuteSqlRaw("""
            CREATE INDEX IF NOT EXISTS "IX_Resultados_ImportacaoId"
            ON "Resultados" ("ImportacaoId")
            """);

        context.Database.ExecuteSqlRaw("""
            CREATE TABLE IF NOT EXISTS "ImportacoesApolice" (
                "Id"             INTEGER NOT NULL CONSTRAINT "PK_ImportacoesApolice" PRIMARY KEY AUTOINCREMENT,
                "ImportadoEm"    TEXT    NOT NULL,
                "ArquivoOrigem"  TEXT    NOT NULL
            )
            """);

        context.Database.ExecuteSqlRaw("""
            CREATE TABLE IF NOT EXISTS "Apolices" (
                "Id"                         INTEGER NOT NULL CONSTRAINT "PK_Apolices" PRIMARY KEY AUTOINCREMENT,
                "ImportacaoApoliceId"         INTEGER NOT NULL,
                "NumeroApolice"              TEXT    NOT NULL,
                "Segurado"                   TEXT    NOT NULL,
                "Seguradora"                 TEXT    NOT NULL,
                "Ramo"                       TEXT    NOT NULL,
                "DataVencimentoPagamento"    TEXT    NOT NULL,
                "DataInicioVigencia"         TEXT,
                "DataFimVigencia"            TEXT,
                "Premio"                     TEXT    NOT NULL,
                "Observacao"                 TEXT,
                CONSTRAINT "FK_Apolices_ImportacoesApolice_ImportacaoApoliceId"
                    FOREIGN KEY ("ImportacaoApoliceId") REFERENCES "ImportacoesApolice" ("Id") ON DELETE CASCADE
            )
            """);

        context.Database.ExecuteSqlRaw("""
            CREATE INDEX IF NOT EXISTS "IX_Apolices_ImportacaoApoliceId"
            ON "Apolices" ("ImportacaoApoliceId")
            """);

        // Recria FuncionariosResultados com schema completo se faltar alguma coluna nova
        var cols = context.Database
            .SqlQueryRaw<string>("SELECT name FROM pragma_table_info('FuncionariosResultados')")
            .ToList();

        if (cols.Count > 0 && (!cols.Contains("Seguradora") || !cols.Contains("Meta")))
        {
            context.Database.ExecuteSqlRaw("DROP TABLE IF EXISTS \"FuncionariosResultados\"");
            context.Database.ExecuteSqlRaw("DROP INDEX  IF EXISTS \"IX_FuncionariosResultados_ImportacaoId\"");
        }

        context.Database.ExecuteSqlRaw("""
            CREATE TABLE IF NOT EXISTS "FuncionariosResultados" (
                "Id"                  INTEGER NOT NULL CONSTRAINT "PK_FuncionariosResultados" PRIMARY KEY AUTOINCREMENT,
                "ImportacaoId"        INTEGER NOT NULL,
                "Nome"                TEXT    NOT NULL,
                "Seguradora"          TEXT    NOT NULL DEFAULT '',
                "Premio"              TEXT    NOT NULL,
                "Meta"                TEXT    NOT NULL DEFAULT '0',
                "Comissao"            TEXT    NOT NULL,
                "PercentualComissao"  TEXT    NOT NULL,
                CONSTRAINT "FK_FuncionariosResultados_Importacoes_ImportacaoId"
                    FOREIGN KEY ("ImportacaoId") REFERENCES "Importacoes" ("Id") ON DELETE CASCADE
            )
            """);

        context.Database.ExecuteSqlRaw("""
            CREATE INDEX IF NOT EXISTS "IX_FuncionariosResultados_ImportacaoId"
            ON "FuncionariosResultados" ("ImportacaoId")
            """);

        // Tabela de usuários do sistema
        context.Database.ExecuteSqlRaw("""
            CREATE TABLE IF NOT EXISTS "Usuarios" (
                "Id"          INTEGER NOT NULL CONSTRAINT "PK_Usuarios" PRIMARY KEY AUTOINCREMENT,
                "Login"       TEXT    NOT NULL,
                "SenhaHash"   TEXT    NOT NULL,
                "TipoAcesso"  INTEGER NOT NULL DEFAULT 0,
                "Ativo"       INTEGER NOT NULL DEFAULT 1
            )
            """);

        context.Database.ExecuteSqlRaw("""
            CREATE UNIQUE INDEX IF NOT EXISTS "IX_Usuarios_Login" ON "Usuarios" ("Login")
            """);

        // Tabela de relatório de renovações (importação do relatório gerencial)
        context.Database.ExecuteSqlRaw("""
            CREATE TABLE IF NOT EXISTS "RelatorioRenovacoes" (
                "Id"                        INTEGER NOT NULL CONSTRAINT "PK_RelatorioRenovacoes" PRIMARY KEY AUTOINCREMENT,
                "Proposta"                  TEXT,
                "Apolice"                   TEXT,
                "PedidoEndosso"             TEXT,
                "Endosso"                   TEXT,
                "Status"                    TEXT,
                "Emissao"                   TEXT,
                "VigenciaInicial"           TEXT,
                "VigenciaFinal"             TEXT,
                "Transmissao"               TEXT,
                "DataControle"              TEXT,
                "TipoRecebimento"           TEXT,
                "Comissao"                  REAL NOT NULL DEFAULT 0,
                "ComissaoGerada"            REAL NOT NULL DEFAULT 0,
                "LiquidoFatura"             REAL NOT NULL DEFAULT 0,
                "PremioLiquido"             REAL NOT NULL DEFAULT 0,
                "PremioTotal"               REAL NOT NULL DEFAULT 0,
                "TotalFatura"               REAL NOT NULL DEFAULT 0,
                "ComissaoAdicional"         REAL NOT NULL DEFAULT 0,
                "PremioAdicional"           REAL NOT NULL DEFAULT 0,
                "PremioCusto"               REAL NOT NULL DEFAULT 0,
                "Iof"                       REAL NOT NULL DEFAULT 0,
                "NumeroParcelas"            INTEGER NOT NULL DEFAULT 0,
                "ValorParcelas"             REAL NOT NULL DEFAULT 0,
                "FormaPagamento"            TEXT,
                "Seguradora"                TEXT,
                "Ramo"                      TEXT,
                "SeguradoraAnterior"        TEXT,
                "NegocioCorretora"          TEXT,
                "CodigoDocumento"           TEXT,
                "VendedorPrincipal"         TEXT,
                "Produto"                   TEXT,
                "QuantidadeSinistros"       INTEGER NOT NULL DEFAULT 0,
                "FranquiaApolice"           REAL NOT NULL DEFAULT 0,
                "QuantidadeEndossos"        INTEGER NOT NULL DEFAULT 0,
                "ObservacaoDocumento"       TEXT,
                "NomeCliente"               TEXT,
                "Nascimento"                TEXT,
                "Sexo"                      TEXT,
                "EstadoCivil"               TEXT,
                "DocumentoPrincipal"        TEXT,
                "ClienteDesde"              TEXT,
                "Profissao"                 TEXT,
                "Prefixo1"                  TEXT,
                "Telefone1"                 TEXT,
                "Prefixo2"                  TEXT,
                "Telefone2"                 TEXT,
                "Prefixo3"                  TEXT,
                "Telefone3"                 TEXT,
                "Email1"                    TEXT,
                "Email2"                    TEXT,
                "Cep"                       TEXT,
                "Endereco"                  TEXT,
                "NumeroEndereco"            TEXT,
                "Complemento"               TEXT,
                "Bairro"                    TEXT,
                "Cidade"                    TEXT,
                "Estado"                    TEXT,
                "Banco"                     TEXT,
                "Agencia"                   TEXT,
                "Conta"                     TEXT,
                "Falecido"                  TEXT,
                "Observacao"                TEXT,
                "Pasta"                     TEXT,
                "DescricaoItem"             TEXT,
                "StatusItem"                TEXT,
                "CodigoFipe"                TEXT,
                "Combustivel"               TEXT,
                "Modelo"                    TEXT,
                "Fabricante"                TEXT,
                "Categoria"                 TEXT,
                "Chassi"                    TEXT,
                "Placa"                     TEXT,
                "AnoFabricacao"             INTEGER,
                "AnoModelo"                 INTEGER,
                "Renavam"                   TEXT,
                "Cor"                       TEXT,
                "Bonus"                     TEXT,
                "ValorDeterminado"          REAL,
                "CepPernoite"               TEXT,
                "Financiado"                TEXT,
                "ZeroKm"                    TEXT,
                "DanosMateriasPremio"       REAL NOT NULL DEFAULT 0,
                "DanosMaterialLmi"          REAL NOT NULL DEFAULT 0,
                "DanosMaterialFranquia"     REAL NOT NULL DEFAULT 0,
                "DanosMoraisPremio"         REAL NOT NULL DEFAULT 0,
                "DanosMoraisLmi"            REAL NOT NULL DEFAULT 0,
                "DanosMoraisFranquia"       REAL NOT NULL DEFAULT 0,
                "DanosCorporaisPremio"      REAL NOT NULL DEFAULT 0,
                "DanosCorporaisLmi"         REAL NOT NULL DEFAULT 0,
                "DanosCorporaisFranquia"    REAL NOT NULL DEFAULT 0,
                "AcidentesPassageiroPremio" REAL NOT NULL DEFAULT 0,
                "AcidentesPassageiroLmi"    REAL NOT NULL DEFAULT 0,
                "AcidentesPassageiroFranquia" REAL NOT NULL DEFAULT 0,
                "ImportadoEm"               TEXT NOT NULL,
                "ArquivoOrigem"             TEXT
            )
            """);

        context.Database.ExecuteSqlRaw("""
            CREATE UNIQUE INDEX IF NOT EXISTS "IX_RelatorioRenovacoes_Proposta"
            ON "RelatorioRenovacoes" ("Proposta")
            WHERE "Proposta" IS NOT NULL
            """);

        // Colunas adicionadas após a criação inicial da tabela
        var colsRenovacoes = context.Database
            .SqlQueryRaw<string>("SELECT name FROM pragma_table_info('RelatorioRenovacoes')")
            .ToList();

        if (!colsRenovacoes.Contains("NovoProdutor"))
            context.Database.ExecuteSqlRaw(
                "ALTER TABLE \"RelatorioRenovacoes\" ADD COLUMN \"NovoProdutor\" TEXT");

        if (!colsRenovacoes.Contains("RenovacaoRealizada"))
            context.Database.ExecuteSqlRaw(
                "ALTER TABLE \"RelatorioRenovacoes\" ADD COLUMN \"RenovacaoRealizada\" INTEGER NOT NULL DEFAULT 0");

        if (!colsRenovacoes.Contains("SituacaoAcompanhamento"))
        {
            context.Database.ExecuteSqlRaw(
                "ALTER TABLE \"RelatorioRenovacoes\" ADD COLUMN \"SituacaoAcompanhamento\" TEXT NOT NULL DEFAULT 'À Renovar'");
            // Migra dados existentes: RenovacaoRealizada=1 → Emitido, else → À Renovar
            context.Database.ExecuteSqlRaw(
                "UPDATE \"RelatorioRenovacoes\" SET \"SituacaoAcompanhamento\" = 'Emitido' WHERE \"RenovacaoRealizada\" = 1");
        }

        // Colunas de fechamento (popup Ren. Palma)
        foreach (var textCol in new[] { "FechamentoSeguradora", "FechamentoFormaPagamento", "FechamentoParcelamento", "FechamentoAssinatura" })
            if (!colsRenovacoes.Contains(textCol))
                context.Database.ExecuteSqlRaw(
                    $"ALTER TABLE \"RelatorioRenovacoes\" ADD COLUMN \"{textCol}\" TEXT");

        if (!colsRenovacoes.Contains("FechamentoPremioLiquido"))
            context.Database.ExecuteSqlRaw(
                "ALTER TABLE \"RelatorioRenovacoes\" ADD COLUMN \"FechamentoPremioLiquido\" REAL");

        if (!colsRenovacoes.Contains("FechamentoComissao"))
            context.Database.ExecuteSqlRaw(
                "ALTER TABLE \"RelatorioRenovacoes\" ADD COLUMN \"FechamentoComissao\" REAL");

        if (!colsRenovacoes.Contains("AssinaturaFeita"))
            context.Database.ExecuteSqlRaw(
                "ALTER TABLE \"RelatorioRenovacoes\" ADD COLUMN \"AssinaturaFeita\" INTEGER NOT NULL DEFAULT 0");

        if (!colsRenovacoes.Contains("SeguroEmitido"))
            context.Database.ExecuteSqlRaw(
                "ALTER TABLE \"RelatorioRenovacoes\" ADD COLUMN \"SeguroEmitido\" INTEGER NOT NULL DEFAULT 0");

        if (!colsRenovacoes.Contains("EmitidoPor"))
            context.Database.ExecuteSqlRaw(
                "ALTER TABLE \"RelatorioRenovacoes\" ADD COLUMN \"EmitidoPor\" TEXT");

        if (!colsRenovacoes.Contains("MotivoSituacao"))
            context.Database.ExecuteSqlRaw(
                "ALTER TABLE \"RelatorioRenovacoes\" ADD COLUMN \"MotivoSituacao\" TEXT");

        if (!colsRenovacoes.Contains("BoletosGerados"))
            context.Database.ExecuteSqlRaw(
                "ALTER TABLE \"RelatorioRenovacoes\" ADD COLUMN \"BoletosGerados\" INTEGER NOT NULL DEFAULT 0");

        // Tabela de anexos por registro de renovação
        context.Database.ExecuteSqlRaw("""
            CREATE TABLE IF NOT EXISTS "Anexos" (
                "Id"                    INTEGER NOT NULL CONSTRAINT "PK_Anexos" PRIMARY KEY AUTOINCREMENT,
                "RelatorioRenovacaoId"  INTEGER NOT NULL,
                "NomeArquivo"           TEXT    NOT NULL,
                "CaminhoArquivo"        TEXT    NOT NULL,
                "TamanhoBytes"          INTEGER NOT NULL DEFAULT 0,
                "AdicionadoEm"          TEXT    NOT NULL
            )
            """);

        context.Database.ExecuteSqlRaw("""
            CREATE INDEX IF NOT EXISTS "IX_Anexos_RelatorioRenovacaoId"
            ON "Anexos" ("RelatorioRenovacaoId")
            """);

        // Tabela de seguros novos (cadastro manual)
        context.Database.ExecuteSqlRaw("""
            CREATE TABLE IF NOT EXISTS "SeguroNovos" (
                "Id"         INTEGER NOT NULL CONSTRAINT "PK_SeguroNovos" PRIMARY KEY AUTOINCREMENT,
                "Vigencia"   TEXT,
                "Segurado"   TEXT NOT NULL DEFAULT '',
                "Cia"        TEXT NOT NULL DEFAULT '',
                "Segmento"   TEXT NOT NULL DEFAULT '',
                "Status"     TEXT NOT NULL DEFAULT '',
                "Financeiro" TEXT NOT NULL DEFAULT '',
                "Pl"         REAL,
                "Fator"      REAL,
                "Valor"      REAL,
                "Observacao" TEXT NOT NULL DEFAULT '',
                "CriadoEm"   TEXT NOT NULL
            )
            """);

        var colsSeguroNovos = context.Database
            .SqlQueryRaw<string>("SELECT name FROM pragma_table_info('SeguroNovos')")
            .ToList();

        if (!colsSeguroNovos.Contains("CriadoPor"))
            context.Database.ExecuteSqlRaw(
                "ALTER TABLE \"SeguroNovos\" ADD COLUMN \"CriadoPor\" TEXT");

        if (!colsSeguroNovos.Contains("EmitidoPor"))
            context.Database.ExecuteSqlRaw(
                "ALTER TABLE \"SeguroNovos\" ADD COLUMN \"EmitidoPor\" TEXT");

        if (!colsSeguroNovos.Contains("FormaPagamento"))
            context.Database.ExecuteSqlRaw(
                "ALTER TABLE \"SeguroNovos\" ADD COLUMN \"FormaPagamento\" TEXT NOT NULL DEFAULT ''");

        if (!colsSeguroNovos.Contains("Parcelas"))
            context.Database.ExecuteSqlRaw(
                "ALTER TABLE \"SeguroNovos\" ADD COLUMN \"Parcelas\" INTEGER");

        if (!colsSeguroNovos.Contains("AssinaturaFeita"))
            context.Database.ExecuteSqlRaw(
                "ALTER TABLE \"SeguroNovos\" ADD COLUMN \"AssinaturaFeita\" INTEGER NOT NULL DEFAULT 0");

        if (!colsSeguroNovos.Contains("BoletosGerados"))
            context.Database.ExecuteSqlRaw(
                "ALTER TABLE \"SeguroNovos\" ADD COLUMN \"BoletosGerados\" INTEGER NOT NULL DEFAULT 0");

        // ── Seguradoras ───────────────────────────────────────────
        context.Database.ExecuteSqlRaw("""
            CREATE TABLE IF NOT EXISTS "Seguradoras" (
                "Id"         INTEGER NOT NULL CONSTRAINT "PK_Seguradoras" PRIMARY KEY AUTOINCREMENT,
                "Nome"       TEXT    NOT NULL,
                "IsParceira" INTEGER NOT NULL DEFAULT 0,
                "Ativo"      INTEGER NOT NULL DEFAULT 1
            )
            """);

        context.Database.ExecuteSqlRaw("""
            CREATE UNIQUE INDEX IF NOT EXISTS "IX_Seguradoras_Nome" ON "Seguradoras" ("Nome")
            """);

        // ── Metas por seguradora ──────────────────────────────────
        context.Database.ExecuteSqlRaw("""
            CREATE TABLE IF NOT EXISTS "MetasSeguradoras" (
                "Id"           INTEGER NOT NULL CONSTRAINT "PK_MetasSeguradoras" PRIMARY KEY AUTOINCREMENT,
                "SeguradoraId" INTEGER NOT NULL,
                "Mes"          INTEGER NOT NULL,
                "Ano"          INTEGER NOT NULL,
                "MetaPremio"   TEXT    NOT NULL DEFAULT '0',
                CONSTRAINT "FK_MetasSeguradoras_Seguradoras_SeguradoraId"
                    FOREIGN KEY ("SeguradoraId") REFERENCES "Seguradoras" ("Id") ON DELETE CASCADE
            )
            """);

        context.Database.ExecuteSqlRaw("""
            CREATE UNIQUE INDEX IF NOT EXISTS "IX_MetasSeguradoras_Seg_Mes_Ano"
            ON "MetasSeguradoras" ("SeguradoraId", "Mes", "Ano")
            """);

        // ── Premiação por seguradoras ──────────────────────────────
        context.Database.ExecuteSqlRaw("""
            CREATE TABLE IF NOT EXISTS "MetasPremiacao" (
                "Id"               INTEGER NOT NULL CONSTRAINT "PK_MetasPremiacao" PRIMARY KEY AUTOINCREMENT,
                "QuantidadeMinima" INTEGER,
                "EhTodas"          INTEGER NOT NULL DEFAULT 0,
                "ValorBonus"       TEXT    NOT NULL DEFAULT '0',
                "Ordem"            INTEGER NOT NULL DEFAULT 0
            )
            """);

        // ── Metas de crescimento ──────────────────────────────────
        context.Database.ExecuteSqlRaw("""
            CREATE TABLE IF NOT EXISTS "MetasCrescimento" (
                "Id"             INTEGER NOT NULL CONSTRAINT "PK_MetasCrescimento" PRIMARY KEY AUTOINCREMENT,
                "Tipo"           TEXT    NOT NULL,
                "PercentualMeta" TEXT    NOT NULL DEFAULT '0',
                "ValorBonus"     TEXT    NOT NULL DEFAULT '0',
                "EhEquipe"       INTEGER NOT NULL DEFAULT 0
            )
            """);

        // ── Valores de referência (ano anterior) ──────────────────
        context.Database.ExecuteSqlRaw("""
            CREATE TABLE IF NOT EXISTS "ValoresReferencia" (
                "Id"           INTEGER NOT NULL CONSTRAINT "PK_ValoresReferencia" PRIMARY KEY AUTOINCREMENT,
                "Mes"          INTEGER NOT NULL,
                "Ano"          INTEGER NOT NULL,
                "PremioTotal"  TEXT    NOT NULL DEFAULT '0',
                "ComissaoTotal" TEXT   NOT NULL DEFAULT '0'
            )
            """);

        // Migração: adiciona coluna Colaborador e recria o índice único com ela
        var colsValRef = context.Database
            .SqlQueryRaw<string>("SELECT name FROM pragma_table_info('ValoresReferencia')")
            .ToList();

        if (!colsValRef.Contains("Colaborador"))
            context.Database.ExecuteSqlRaw(
                "ALTER TABLE \"ValoresReferencia\" ADD COLUMN \"Colaborador\" TEXT NOT NULL DEFAULT ''");

        // Sempre garante: índice antigo removido e novo (com Colaborador) criado
        context.Database.ExecuteSqlRaw("DROP INDEX IF EXISTS \"IX_ValoresReferencia_Mes_Ano\"");
        context.Database.ExecuteSqlRaw("""
            CREATE UNIQUE INDEX IF NOT EXISTS "IX_ValoresReferencia_Colaborador_Mes_Ano"
            ON "ValoresReferencia" ("Colaborador", "Mes", "Ano")
            """);

        // ── Clientes ──────────────────────────────────────────────
        context.Database.ExecuteSqlRaw("""
            CREATE TABLE IF NOT EXISTS "Clientes" (
                "Id"             INTEGER NOT NULL CONSTRAINT "PK_Clientes" PRIMARY KEY AUTOINCREMENT,
                "Cpf"            TEXT    NOT NULL DEFAULT '',
                "Nome"           TEXT    NOT NULL DEFAULT '',
                "Nascimento"     TEXT,
                "Sexo"           TEXT,
                "EstadoCivil"    TEXT,
                "Profissao"      TEXT,
                "ClienteDesde"   TEXT,
                "Prefixo1"       TEXT,
                "Telefone1"      TEXT,
                "Prefixo2"       TEXT,
                "Telefone2"      TEXT,
                "Prefixo3"       TEXT,
                "Telefone3"      TEXT,
                "Email1"         TEXT,
                "Email2"         TEXT,
                "Cep"            TEXT,
                "Endereco"       TEXT,
                "NumeroEndereco" TEXT,
                "Complemento"    TEXT,
                "Bairro"         TEXT,
                "Cidade"         TEXT,
                "Estado"         TEXT,
                "Observacoes"    TEXT,
                "Historico"      TEXT,
                "CriadoEm"       TEXT    NOT NULL,
                "AtualizadoEm"   TEXT
            )
            """);

        context.Database.ExecuteSqlRaw("""
            CREATE UNIQUE INDEX IF NOT EXISTS "IX_Clientes_Cpf"
            ON "Clientes" ("Cpf")
            WHERE "Cpf" != ''
            """);

        // Migração: adiciona colunas ao schema expandido de Clientes
        var colsClientes = context.Database
            .SqlQueryRaw<string>("SELECT name FROM pragma_table_info('Clientes')")
            .ToList();

        foreach (var col in new[] { "Nascimento", "Sexo", "EstadoCivil", "Profissao", "ClienteDesde",
                                    "Prefixo1", "Prefixo2", "Prefixo3",
                                    "Telefone1", "Telefone2", "Telefone3",
                                    "Email1", "Email2",
                                    "Cep", "Endereco", "NumeroEndereco", "Complemento",
                                    "Bairro", "Cidade", "Estado" })
            if (!colsClientes.Contains(col))
                context.Database.ExecuteSqlRaw($"ALTER TABLE \"Clientes\" ADD COLUMN \"{col}\" TEXT");

        // Remove colunas obsoletas do schema antigo (Telefone, Email) se existirem
        // SQLite não suporta DROP COLUMN antes do 3.35 — os campos são ignorados pelo EF

        // ── Leads ─────────────────────────────────────────────────
        context.Database.ExecuteSqlRaw("""
            CREATE TABLE IF NOT EXISTS "Leads" (
                "Id"           INTEGER NOT NULL CONSTRAINT "PK_Leads" PRIMARY KEY AUTOINCREMENT,
                "Segurado"     TEXT    NOT NULL DEFAULT '',
                "Produtor"     TEXT    NOT NULL DEFAULT '',
                "CriadoEm"     TEXT    NOT NULL,
                "Indicacao"    TEXT,
                "Observacao"   TEXT,
                "SeguroGerado" INTEGER NOT NULL DEFAULT 0,
                "Fechou"       INTEGER NOT NULL DEFAULT 0,
                "FechouEm"     TEXT,
                "SeguroNovoId" INTEGER
            )
            """);

        // Seed: seguradoras padrão (parceiras e demais)
        if (!context.Seguradoras.Any())
        {
            var parceiras = new[] { "Porto", "Unimed", "Hdi", "Allianz", "Tokio", "Zurich", "Bradesco", "Azul" };
            foreach (var nome in parceiras)
                context.Seguradoras.Add(new Models.Seguradora { Nome = nome, IsParceira = true, Ativo = true });
            context.Seguradoras.Add(new Models.Seguradora { Nome = "Demais", IsParceira = false, Ativo = true });
            context.SaveChanges();
        }

        // Seed: premiação por seguradoras atingidas
        if (!context.MetasPremiacao.Any())
        {
            context.MetasPremiacao.AddRange(
                new Models.MetaPremiacao { QuantidadeMinima = 3,  EhTodas = false, ValorBonus = 100m, Ordem = 1 },
                new Models.MetaPremiacao { QuantidadeMinima = 6,  EhTodas = false, ValorBonus = 100m, Ordem = 2 },
                new Models.MetaPremiacao { QuantidadeMinima = null, EhTodas = true, ValorBonus = 100m, Ordem = 3 }
            );
            context.SaveChanges();
        }

        // Migração: corrige threshold inicial de 4 → 3 (banco existente)
        context.Database.ExecuteSqlRaw(
            "UPDATE \"MetasPremiacao\" SET \"QuantidadeMinima\" = 3 WHERE \"QuantidadeMinima\" = 4 AND \"EhTodas\" = 0");

        // Seed: metas de crescimento
        if (!context.MetasCrescimento.Any())
        {
            context.MetasCrescimento.AddRange(
                new Models.MetaCrescimento { Tipo = "Premio",   PercentualMeta = 0.10m, ValorBonus = 100m,  EhEquipe = true  },
                new Models.MetaCrescimento { Tipo = "Premio",   PercentualMeta = 0.15m, ValorBonus = 300m,  EhEquipe = true  },
                new Models.MetaCrescimento { Tipo = "Comissao", PercentualMeta = 0.15m, ValorBonus = 100m,  EhEquipe = false },
                new Models.MetaCrescimento { Tipo = "Comissao", PercentualMeta = 0.20m, ValorBonus = 200m,  EhEquipe = false }
            );
            context.SaveChanges();
        }

        // Seed: cria admin padrão (admin / admin123) se não houver usuários
        if (!context.Usuarios.Any())
        {
            context.Usuarios.Add(new Models.Usuario
            {
                Login = "admin",
                SenhaHash = UsuarioService.HashSenha("admin123"),
                TipoAcesso = Models.TipoAcesso.Administrador,
                Ativo = true
            });
            context.SaveChanges();
        }
    }
}
