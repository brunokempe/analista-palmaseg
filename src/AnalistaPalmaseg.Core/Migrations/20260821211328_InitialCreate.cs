using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace AnalistaPalmaseg.Core.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Anexos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    RelatorioRenovacaoId = table.Column<int>(type: "integer", nullable: false),
                    NomeArquivo = table.Column<string>(type: "text", nullable: false),
                    CaminhoArquivo = table.Column<string>(type: "text", nullable: false),
                    TamanhoBytes = table.Column<long>(type: "bigint", nullable: false),
                    AdicionadoEm = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Anexos", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Clientes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Cpf = table.Column<string>(type: "text", nullable: false),
                    Nome = table.Column<string>(type: "text", nullable: false),
                    Nascimento = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    Sexo = table.Column<string>(type: "text", nullable: true),
                    EstadoCivil = table.Column<string>(type: "text", nullable: true),
                    Profissao = table.Column<string>(type: "text", nullable: true),
                    ClienteDesde = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    Prefixo1 = table.Column<string>(type: "text", nullable: true),
                    Telefone1 = table.Column<string>(type: "text", nullable: true),
                    Prefixo2 = table.Column<string>(type: "text", nullable: true),
                    Telefone2 = table.Column<string>(type: "text", nullable: true),
                    Prefixo3 = table.Column<string>(type: "text", nullable: true),
                    Telefone3 = table.Column<string>(type: "text", nullable: true),
                    Email1 = table.Column<string>(type: "text", nullable: true),
                    Email2 = table.Column<string>(type: "text", nullable: true),
                    Cep = table.Column<string>(type: "text", nullable: true),
                    Endereco = table.Column<string>(type: "text", nullable: true),
                    NumeroEndereco = table.Column<string>(type: "text", nullable: true),
                    Complemento = table.Column<string>(type: "text", nullable: true),
                    Bairro = table.Column<string>(type: "text", nullable: true),
                    Cidade = table.Column<string>(type: "text", nullable: true),
                    Estado = table.Column<string>(type: "text", nullable: true),
                    Observacoes = table.Column<string>(type: "text", nullable: true),
                    Historico = table.Column<string>(type: "text", nullable: true),
                    CriadoEm = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    AtualizadoEm = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Clientes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DistribuicaoReferencias",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Ano = table.Column<int>(type: "integer", nullable: false),
                    PremioLiquidoRef = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    ComissaoRef = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    QtdApolicesRef = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DistribuicaoReferencias", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Importacoes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Produtor = table.Column<string>(type: "text", nullable: false),
                    Mes = table.Column<int>(type: "integer", nullable: false),
                    Ano = table.Column<int>(type: "integer", nullable: false),
                    ImportadoEm = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    ArquivoOrigem = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Importacoes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ImportacoesApolice",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ImportadoEm = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    ArquivoOrigem = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ImportacoesApolice", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Leads",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Segurado = table.Column<string>(type: "text", nullable: false),
                    Produtor = table.Column<string>(type: "text", nullable: false),
                    CriadoEm = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    Indicacao = table.Column<string>(type: "text", nullable: true),
                    Observacao = table.Column<string>(type: "text", nullable: true),
                    SeguroGerado = table.Column<bool>(type: "boolean", nullable: false),
                    Fechou = table.Column<bool>(type: "boolean", nullable: false),
                    FechouEm = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    SeguroNovoId = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Leads", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MetasCrescimento",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Tipo = table.Column<string>(type: "text", nullable: false),
                    PercentualMeta = table.Column<decimal>(type: "numeric(10,4)", nullable: false),
                    ValorBonus = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    EhEquipe = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MetasCrescimento", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MetasPremiacao",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    QuantidadeMinima = table.Column<int>(type: "integer", nullable: true),
                    EhTodas = table.Column<bool>(type: "boolean", nullable: false),
                    ValorBonus = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    Ordem = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MetasPremiacao", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PastasSalvarPropostas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Caminho = table.Column<string>(type: "text", nullable: false),
                    CriadoEm = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PastasSalvarPropostas", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RelatorioRenovacoes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Proposta = table.Column<string>(type: "text", nullable: true),
                    Apolice = table.Column<string>(type: "text", nullable: true),
                    PedidoEndosso = table.Column<string>(type: "text", nullable: true),
                    Endosso = table.Column<string>(type: "text", nullable: true),
                    Status = table.Column<string>(type: "text", nullable: true),
                    Emissao = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    VigenciaInicial = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    VigenciaFinal = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    Transmissao = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    DataControle = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    TipoRecebimento = table.Column<string>(type: "text", nullable: true),
                    Comissao = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    ComissaoGerada = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    LiquidoFatura = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    PremioLiquido = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    PremioTotal = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    TotalFatura = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    ComissaoAdicional = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    PremioAdicional = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    PremioCusto = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    Iof = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    NumeroParcelas = table.Column<int>(type: "integer", nullable: false),
                    ValorParcelas = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    FormaPagamento = table.Column<string>(type: "text", nullable: true),
                    Seguradora = table.Column<string>(type: "text", nullable: true),
                    Ramo = table.Column<string>(type: "text", nullable: true),
                    SeguradoraAnterior = table.Column<string>(type: "text", nullable: true),
                    NegocioCorretora = table.Column<string>(type: "text", nullable: true),
                    CodigoDocumento = table.Column<string>(type: "text", nullable: true),
                    VendedorPrincipal = table.Column<string>(type: "text", nullable: true),
                    Produto = table.Column<string>(type: "text", nullable: true),
                    QuantidadeSinistros = table.Column<int>(type: "integer", nullable: false),
                    FranquiaApolice = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    QuantidadeEndossos = table.Column<int>(type: "integer", nullable: false),
                    ObservacaoDocumento = table.Column<string>(type: "text", nullable: true),
                    NomeCliente = table.Column<string>(type: "text", nullable: true),
                    Nascimento = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    Sexo = table.Column<string>(type: "text", nullable: true),
                    EstadoCivil = table.Column<string>(type: "text", nullable: true),
                    DocumentoPrincipal = table.Column<string>(type: "text", nullable: true),
                    ClienteDesde = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    Profissao = table.Column<string>(type: "text", nullable: true),
                    Prefixo1 = table.Column<string>(type: "text", nullable: true),
                    Telefone1 = table.Column<string>(type: "text", nullable: true),
                    Prefixo2 = table.Column<string>(type: "text", nullable: true),
                    Telefone2 = table.Column<string>(type: "text", nullable: true),
                    Prefixo3 = table.Column<string>(type: "text", nullable: true),
                    Telefone3 = table.Column<string>(type: "text", nullable: true),
                    Email1 = table.Column<string>(type: "text", nullable: true),
                    Email2 = table.Column<string>(type: "text", nullable: true),
                    Cep = table.Column<string>(type: "text", nullable: true),
                    Endereco = table.Column<string>(type: "text", nullable: true),
                    NumeroEndereco = table.Column<string>(type: "text", nullable: true),
                    Complemento = table.Column<string>(type: "text", nullable: true),
                    Bairro = table.Column<string>(type: "text", nullable: true),
                    Cidade = table.Column<string>(type: "text", nullable: true),
                    Estado = table.Column<string>(type: "text", nullable: true),
                    Banco = table.Column<string>(type: "text", nullable: true),
                    Agencia = table.Column<string>(type: "text", nullable: true),
                    Conta = table.Column<string>(type: "text", nullable: true),
                    Falecido = table.Column<string>(type: "text", nullable: true),
                    Observacao = table.Column<string>(type: "text", nullable: true),
                    Pasta = table.Column<string>(type: "text", nullable: true),
                    DescricaoItem = table.Column<string>(type: "text", nullable: true),
                    StatusItem = table.Column<string>(type: "text", nullable: true),
                    CodigoFipe = table.Column<string>(type: "text", nullable: true),
                    Combustivel = table.Column<string>(type: "text", nullable: true),
                    Modelo = table.Column<string>(type: "text", nullable: true),
                    Fabricante = table.Column<string>(type: "text", nullable: true),
                    Categoria = table.Column<string>(type: "text", nullable: true),
                    Chassi = table.Column<string>(type: "text", nullable: true),
                    Placa = table.Column<string>(type: "text", nullable: true),
                    AnoFabricacao = table.Column<int>(type: "integer", nullable: true),
                    AnoModelo = table.Column<int>(type: "integer", nullable: true),
                    Renavam = table.Column<string>(type: "text", nullable: true),
                    Cor = table.Column<string>(type: "text", nullable: true),
                    Bonus = table.Column<string>(type: "text", nullable: true),
                    ValorDeterminado = table.Column<decimal>(type: "numeric(18,2)", nullable: true),
                    CepPernoite = table.Column<string>(type: "text", nullable: true),
                    Financiado = table.Column<string>(type: "text", nullable: true),
                    ZeroKm = table.Column<string>(type: "text", nullable: true),
                    DanosMateriasPremio = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    DanosMaterialLmi = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    DanosMaterialFranquia = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    DanosMoraisPremio = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    DanosMoraisLmi = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    DanosMoraisFranquia = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    DanosCorporaisPremio = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    DanosCorporaisLmi = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    DanosCorporaisFranquia = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    AcidentesPassageiroPremio = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    AcidentesPassageiroLmi = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    AcidentesPassageiroFranquia = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    NovoProdutor = table.Column<string>(type: "text", nullable: true),
                    MotivoSituacao = table.Column<string>(type: "text", nullable: true),
                    PercentualComissaoMinimo = table.Column<decimal>(type: "numeric(18,2)", nullable: true),
                    FechamentoSeguradora = table.Column<string>(type: "text", nullable: true),
                    FechamentoPremioLiquido = table.Column<decimal>(type: "numeric(18,2)", nullable: true),
                    FechamentoFormaPagamento = table.Column<string>(type: "text", nullable: true),
                    FechamentoComissao = table.Column<decimal>(type: "numeric(18,2)", nullable: true),
                    FechamentoParcelamento = table.Column<string>(type: "text", nullable: true),
                    FechamentoAssinatura = table.Column<string>(type: "text", nullable: true),
                    AssinaturaFeita = table.Column<bool>(type: "boolean", nullable: false),
                    SeguroEmitido = table.Column<bool>(type: "boolean", nullable: false),
                    EmitidoPor = table.Column<string>(type: "text", nullable: true),
                    BoletosGerados = table.Column<int>(type: "integer", nullable: false),
                    SituacaoAcompanhamento = table.Column<string>(type: "text", nullable: false),
                    ImportadoEm = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    ArquivoOrigem = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RelatorioRenovacoes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Seguradoras",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Nome = table.Column<string>(type: "text", nullable: false),
                    IsParceira = table.Column<bool>(type: "boolean", nullable: false),
                    Ativo = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Seguradoras", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SeguroNovos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Vigencia = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    Segurado = table.Column<string>(type: "text", nullable: false),
                    Cia = table.Column<string>(type: "text", nullable: false),
                    Segmento = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    Financeiro = table.Column<string>(type: "text", nullable: false),
                    Pl = table.Column<decimal>(type: "numeric(18,2)", nullable: true),
                    Fator = table.Column<decimal>(type: "numeric(18,2)", nullable: true),
                    Valor = table.Column<decimal>(type: "numeric(18,2)", nullable: true),
                    FormaPagamento = table.Column<string>(type: "text", nullable: false),
                    Parcelas = table.Column<int>(type: "integer", nullable: true),
                    AssinaturaFeita = table.Column<bool>(type: "boolean", nullable: false),
                    SeguroEmitido = table.Column<bool>(type: "boolean", nullable: false),
                    BoletosGerados = table.Column<int>(type: "integer", nullable: false),
                    Observacao = table.Column<string>(type: "text", nullable: false),
                    CriadoEm = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    CriadoPor = table.Column<string>(type: "text", nullable: true),
                    EmitidoPor = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SeguroNovos", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Usuarios",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Login = table.Column<string>(type: "text", nullable: false),
                    SenhaHash = table.Column<string>(type: "text", nullable: false),
                    TipoAcesso = table.Column<int>(type: "integer", nullable: false),
                    Ativo = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Usuarios", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ValoresReferencia",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Colaborador = table.Column<string>(type: "text", nullable: false),
                    Mes = table.Column<int>(type: "integer", nullable: false),
                    Ano = table.Column<int>(type: "integer", nullable: false),
                    PremioTotal = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    ComissaoTotal = table.Column<decimal>(type: "numeric(18,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ValoresReferencia", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FuncionariosResultados",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ImportacaoId = table.Column<int>(type: "integer", nullable: false),
                    Nome = table.Column<string>(type: "text", nullable: false),
                    Seguradora = table.Column<string>(type: "text", nullable: false),
                    Premio = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    Meta = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    Comissao = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    PercentualComissao = table.Column<decimal>(type: "numeric(10,4)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FuncionariosResultados", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FuncionariosResultados_Importacoes_ImportacaoId",
                        column: x => x.ImportacaoId,
                        principalTable: "Importacoes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "NovosNegocios",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ImportacaoId = table.Column<int>(type: "integer", nullable: false),
                    Vigencia = table.Column<DateOnly>(type: "date", nullable: false),
                    Segurado = table.Column<string>(type: "text", nullable: false),
                    Cia = table.Column<string>(type: "text", nullable: false),
                    Segmento = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    Pl = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    Fator = table.Column<decimal>(type: "numeric(10,4)", nullable: false),
                    Comissao = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    Observacao = table.Column<string>(type: "text", nullable: true),
                    EmitidoPor = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NovosNegocios", x => x.Id);
                    table.ForeignKey(
                        name: "FK_NovosNegocios_Importacoes_ImportacaoId",
                        column: x => x.ImportacaoId,
                        principalTable: "Importacoes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Renovacoes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ImportacaoId = table.Column<int>(type: "integer", nullable: false),
                    Vigencia = table.Column<DateOnly>(type: "date", nullable: false),
                    Segurado = table.Column<string>(type: "text", nullable: false),
                    Cia = table.Column<string>(type: "text", nullable: false),
                    Ramo = table.Column<string>(type: "text", nullable: false),
                    PlBase = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    Fator = table.Column<decimal>(type: "numeric(10,4)", nullable: false),
                    Comissao = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    CiaRenovada = table.Column<string>(type: "text", nullable: true),
                    NovoPl = table.Column<decimal>(type: "numeric(18,2)", nullable: true),
                    NovaComissao = table.Column<decimal>(type: "numeric(18,2)", nullable: true),
                    SaldoPl = table.Column<decimal>(type: "numeric(18,2)", nullable: true),
                    EmitidoPor = table.Column<string>(type: "text", nullable: true),
                    Observacao = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Renovacoes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Renovacoes_Importacoes_ImportacaoId",
                        column: x => x.ImportacaoId,
                        principalTable: "Importacoes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Resultados",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ImportacaoId = table.Column<int>(type: "integer", nullable: false),
                    Funcionario = table.Column<string>(type: "text", nullable: false),
                    Meta = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    Realizado = table.Column<decimal>(type: "numeric(18,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Resultados", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Resultados_Importacoes_ImportacaoId",
                        column: x => x.ImportacaoId,
                        principalTable: "Importacoes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Apolices",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ImportacaoApoliceId = table.Column<int>(type: "integer", nullable: false),
                    NumeroApolice = table.Column<string>(type: "text", nullable: false),
                    Segurado = table.Column<string>(type: "text", nullable: false),
                    Seguradora = table.Column<string>(type: "text", nullable: false),
                    Ramo = table.Column<string>(type: "text", nullable: false),
                    DataVencimentoPagamento = table.Column<DateOnly>(type: "date", nullable: false),
                    DataInicioVigencia = table.Column<DateOnly>(type: "date", nullable: true),
                    DataFimVigencia = table.Column<DateOnly>(type: "date", nullable: true),
                    Premio = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    Observacao = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Apolices", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Apolices_ImportacoesApolice_ImportacaoApoliceId",
                        column: x => x.ImportacaoApoliceId,
                        principalTable: "ImportacoesApolice",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MetasSeguradoras",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    SeguradoraId = table.Column<int>(type: "integer", nullable: false),
                    Mes = table.Column<int>(type: "integer", nullable: false),
                    Ano = table.Column<int>(type: "integer", nullable: false),
                    MetaPremio = table.Column<decimal>(type: "numeric(18,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MetasSeguradoras", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MetasSeguradoras_Seguradoras_SeguradoraId",
                        column: x => x.SeguradoraId,
                        principalTable: "Seguradoras",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Anexos_RelatorioRenovacaoId",
                table: "Anexos",
                column: "RelatorioRenovacaoId");

            migrationBuilder.CreateIndex(
                name: "IX_Apolices_ImportacaoApoliceId",
                table: "Apolices",
                column: "ImportacaoApoliceId");

            migrationBuilder.CreateIndex(
                name: "IX_Clientes_Cpf",
                table: "Clientes",
                column: "Cpf",
                unique: true,
                filter: "\"Cpf\" != ''");

            migrationBuilder.CreateIndex(
                name: "IX_FuncionariosResultados_ImportacaoId",
                table: "FuncionariosResultados",
                column: "ImportacaoId");

            migrationBuilder.CreateIndex(
                name: "IX_Importacoes_Produtor_Mes_Ano",
                table: "Importacoes",
                columns: new[] { "Produtor", "Mes", "Ano" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MetasSeguradoras_SeguradoraId_Mes_Ano",
                table: "MetasSeguradoras",
                columns: new[] { "SeguradoraId", "Mes", "Ano" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_NovosNegocios_ImportacaoId",
                table: "NovosNegocios",
                column: "ImportacaoId");

            migrationBuilder.CreateIndex(
                name: "IX_RelatorioRenovacoes_Proposta",
                table: "RelatorioRenovacoes",
                column: "Proposta",
                unique: true,
                filter: "\"Proposta\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Renovacoes_ImportacaoId",
                table: "Renovacoes",
                column: "ImportacaoId");

            migrationBuilder.CreateIndex(
                name: "IX_Resultados_ImportacaoId",
                table: "Resultados",
                column: "ImportacaoId");

            migrationBuilder.CreateIndex(
                name: "IX_ValoresReferencia_Colaborador_Mes_Ano",
                table: "ValoresReferencia",
                columns: new[] { "Colaborador", "Mes", "Ano" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Anexos");

            migrationBuilder.DropTable(
                name: "Apolices");

            migrationBuilder.DropTable(
                name: "Clientes");

            migrationBuilder.DropTable(
                name: "DistribuicaoReferencias");

            migrationBuilder.DropTable(
                name: "FuncionariosResultados");

            migrationBuilder.DropTable(
                name: "Leads");

            migrationBuilder.DropTable(
                name: "MetasCrescimento");

            migrationBuilder.DropTable(
                name: "MetasPremiacao");

            migrationBuilder.DropTable(
                name: "MetasSeguradoras");

            migrationBuilder.DropTable(
                name: "NovosNegocios");

            migrationBuilder.DropTable(
                name: "PastasSalvarPropostas");

            migrationBuilder.DropTable(
                name: "RelatorioRenovacoes");

            migrationBuilder.DropTable(
                name: "Renovacoes");

            migrationBuilder.DropTable(
                name: "Resultados");

            migrationBuilder.DropTable(
                name: "SeguroNovos");

            migrationBuilder.DropTable(
                name: "Usuarios");

            migrationBuilder.DropTable(
                name: "ValoresReferencia");

            migrationBuilder.DropTable(
                name: "ImportacoesApolice");

            migrationBuilder.DropTable(
                name: "Seguradoras");

            migrationBuilder.DropTable(
                name: "Importacoes");
        }
    }
}
