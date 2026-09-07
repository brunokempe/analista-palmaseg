using AnalistaPalmaseg.Core.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace AnalistaPalmaseg.Core.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Usuario> Usuarios { get; set; }
    public DbSet<Importacao> Importacoes { get; set; }
    public DbSet<Renovacao> Renovacoes { get; set; }
    public DbSet<NovoNegocio> NovosNegocios { get; set; }
    public DbSet<ResultadoMeta> Resultados { get; set; }
    public DbSet<FuncionarioResultado> FuncionariosResultados { get; set; }
    public DbSet<ImportacaoApolice> ImportacoesApolice { get; set; }
    public DbSet<Apolice> Apolices { get; set; }
    public DbSet<RelatorioRenovacao> RelatorioRenovacoes { get; set; }
    public DbSet<Anexo> Anexos { get; set; }
    public DbSet<SeguroNovo> SeguroNovos { get; set; }
    public DbSet<Seguradora> Seguradoras { get; set; }
    public DbSet<MetaSeguradora> MetasSeguradoras { get; set; }
    public DbSet<MetaPremiacao> MetasPremiacao { get; set; }
    public DbSet<MetaCrescimento> MetasCrescimento { get; set; }
    public DbSet<ValorReferencia> ValoresReferencia { get; set; }
    public DbSet<Cliente>                Clientes                { get; set; }
    public DbSet<Lead>                   Leads                   { get; set; }
    public DbSet<DistribuicaoReferencia> DistribuicaoReferencias { get; set; }
    public DbSet<PastaProdutor>           PastasProdutor          { get; set; }
    public DbSet<FavoritoMenu>            FavoritosMenu           { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Importacao>(e =>
        {
            e.HasIndex(x => new { x.Produtor, x.Mes, x.Ano }).IsUnique();
        });

        modelBuilder.Entity<Renovacao>(e =>
        {
            e.Property(x => x.PlBase).HasColumnType("decimal(18,2)");
            e.Property(x => x.Fator).HasColumnType("decimal(10,4)");
            e.Property(x => x.Comissao).HasColumnType("decimal(18,2)");
            e.Property(x => x.NovoPl).HasColumnType("decimal(18,2)");
            e.Property(x => x.NovaComissao).HasColumnType("decimal(18,2)");
            e.Property(x => x.SaldoPl).HasColumnType("decimal(18,2)");
        });

        modelBuilder.Entity<NovoNegocio>(e =>
        {
            e.Property(x => x.Pl).HasColumnType("decimal(18,2)");
            e.Property(x => x.Fator).HasColumnType("decimal(10,4)");
            e.Property(x => x.Comissao).HasColumnType("decimal(18,2)");
        });

        modelBuilder.Entity<ResultadoMeta>(e =>
        {
            e.Property(x => x.Meta).HasColumnType("decimal(18,2)");
            e.Property(x => x.Realizado).HasColumnType("decimal(18,2)");
        });

        modelBuilder.Entity<FuncionarioResultado>(e =>
        {
            e.Property(x => x.Premio).HasColumnType("decimal(18,2)");
            e.Property(x => x.Meta).HasColumnType("decimal(18,2)");
            e.Property(x => x.Comissao).HasColumnType("decimal(18,2)");
            e.Property(x => x.PercentualComissao).HasColumnType("decimal(10,4)");
            e.Ignore(x => x.PercentualAtingimento);
        });

        modelBuilder.Entity<Apolice>(e =>
        {
            e.Property(x => x.Premio).HasColumnType("decimal(18,2)");
            e.Ignore(x => x.DiasParaVencimento);
            e.Ignore(x => x.StatusLabel);
            e.Ignore(x => x.DiasLabel);
        });

        modelBuilder.Entity<RelatorioRenovacao>(e =>
        {
            // CódigoDocumento (não Proposta) é a chave natural: seguradoras às vezes reaproveitam o
            // mesmo número de proposta/apólice em registros diferentes (ex.: renovação de outro item,
            // ou apólices distintas do mesmo cliente), mas o código do documento nunca se repete.
            e.HasIndex(x => x.CodigoDocumento).IsUnique().HasFilter("\"CodigoDocumento\" IS NOT NULL");
            e.Ignore(x => x.DiaDaSemana);
            e.Ignore(x => x.IsChecked);

            foreach (var propertyName in new[]
            {
                nameof(RelatorioRenovacao.Comissao), nameof(RelatorioRenovacao.ComissaoGerada),
                nameof(RelatorioRenovacao.LiquidoFatura), nameof(RelatorioRenovacao.PremioLiquido),
                nameof(RelatorioRenovacao.PremioTotal), nameof(RelatorioRenovacao.TotalFatura),
                nameof(RelatorioRenovacao.ComissaoAdicional), nameof(RelatorioRenovacao.PremioAdicional),
                nameof(RelatorioRenovacao.PremioCusto), nameof(RelatorioRenovacao.Iof),
                nameof(RelatorioRenovacao.ValorParcelas), nameof(RelatorioRenovacao.FranquiaApolice),
                nameof(RelatorioRenovacao.ValorDeterminado), nameof(RelatorioRenovacao.PercentualComissaoMinimo),
                nameof(RelatorioRenovacao.FechamentoPremioLiquido), nameof(RelatorioRenovacao.FechamentoComissao),
                nameof(RelatorioRenovacao.DanosMateriasPremio), nameof(RelatorioRenovacao.DanosMaterialLmi),
                nameof(RelatorioRenovacao.DanosMaterialFranquia), nameof(RelatorioRenovacao.DanosMoraisPremio),
                nameof(RelatorioRenovacao.DanosMoraisLmi), nameof(RelatorioRenovacao.DanosMoraisFranquia),
                nameof(RelatorioRenovacao.DanosCorporaisPremio), nameof(RelatorioRenovacao.DanosCorporaisLmi),
                nameof(RelatorioRenovacao.DanosCorporaisFranquia), nameof(RelatorioRenovacao.AcidentesPassageiroPremio),
                nameof(RelatorioRenovacao.AcidentesPassageiroLmi), nameof(RelatorioRenovacao.AcidentesPassageiroFranquia),
            })
                e.Property(propertyName).HasColumnType("numeric(18,2)");
        });

        modelBuilder.Entity<Anexo>(e =>
        {
            e.HasIndex(x => x.RelatorioRenovacaoId);
        });

        modelBuilder.Entity<SeguroNovo>(e =>
        {
            e.Property(x => x.Pl).HasColumnType("decimal(18,2)");
            e.Property(x => x.Fator).HasColumnType("decimal(18,2)");
            e.Property(x => x.Valor).HasColumnType("decimal(18,2)");
        });

        modelBuilder.Entity<MetaSeguradora>(e =>
        {
            e.HasIndex(x => new { x.SeguradoraId, x.Mes, x.Ano }).IsUnique();
            e.Property(x => x.MetaPremio).HasColumnType("decimal(18,2)");
            e.HasOne(x => x.Seguradora).WithMany().HasForeignKey(x => x.SeguradoraId);
        });

        modelBuilder.Entity<MetaPremiacao>(e =>
        {
            e.Property(x => x.ValorBonus).HasColumnType("decimal(18,2)");
        });

        modelBuilder.Entity<MetaCrescimento>(e =>
        {
            e.Property(x => x.PercentualMeta).HasColumnType("decimal(10,4)");
            e.Property(x => x.ValorBonus).HasColumnType("decimal(18,2)");
        });

        modelBuilder.Entity<ValorReferencia>(e =>
        {
            e.HasIndex(x => new { x.Colaborador, x.Mes, x.Ano }).IsUnique();
            e.Property(x => x.PremioTotal).HasColumnType("decimal(18,2)");
            e.Property(x => x.ComissaoTotal).HasColumnType("decimal(18,2)");
        });

        modelBuilder.Entity<Cliente>(e =>
        {
            e.HasIndex(x => x.Cpf).IsUnique().HasFilter("\"Cpf\" != ''");
        });

        modelBuilder.Entity<DistribuicaoReferencia>(e =>
        {
            e.Property(x => x.PremioLiquidoRef).HasColumnType("decimal(18,2)");
            e.Property(x => x.ComissaoRef).HasColumnType("decimal(18,2)");
        });

        modelBuilder.Entity<PastaProdutor>(e =>
        {
            e.HasIndex(x => new { x.UsuarioId, x.Caminho }).IsUnique();
            e.HasOne(x => x.Usuario).WithMany().HasForeignKey(x => x.UsuarioId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<FavoritoMenu>(e =>
        {
            e.HasIndex(x => new { x.UsuarioId, x.MenuKey }).IsUnique();
            e.HasOne(x => x.Usuario).WithMany().HasForeignKey(x => x.UsuarioId).OnDelete(DeleteBehavior.Cascade);
        });

        // O app só lida com datas locais (sem fuso horário) — mapeia todas as colunas
        // DateTime para "timestamp without time zone" e normaliza o Kind para Unspecified,
        // já que o Npgsql rejeita DateTime.Now (Kind=Local) em colunas com fuso horário.
        var dateTimeConverter = new ValueConverter<DateTime, DateTime>(
            v => DateTime.SpecifyKind(v, DateTimeKind.Unspecified),
            v => DateTime.SpecifyKind(v, DateTimeKind.Unspecified));

        var nullableDateTimeConverter = new ValueConverter<DateTime?, DateTime?>(
            v => v.HasValue ? DateTime.SpecifyKind(v.Value, DateTimeKind.Unspecified) : v,
            v => v.HasValue ? DateTime.SpecifyKind(v.Value, DateTimeKind.Unspecified) : v);

        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            foreach (var property in entityType.GetProperties())
            {
                if (property.ClrType == typeof(DateTime))
                {
                    property.SetValueConverter(dateTimeConverter);
                    property.SetColumnType("timestamp without time zone");
                }
                else if (property.ClrType == typeof(DateTime?))
                {
                    property.SetValueConverter(nullableDateTimeConverter);
                    property.SetColumnType("timestamp without time zone");
                }
            }
        }
    }
}
