using AnalistaPalmaseg.Core.Models;
using Microsoft.EntityFrameworkCore;

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
            e.HasIndex(x => x.Proposta).IsUnique();
            e.Ignore(x => x.DiaDaSemana);
            e.Ignore(x => x.IsChecked);
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
    }
}
