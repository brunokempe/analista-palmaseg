using AnalistaPalmaseg.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace AnalistaPalmaseg.Core.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Importacao> Importacoes { get; set; }
    public DbSet<Renovacao> Renovacoes { get; set; }
    public DbSet<NovoNegocio> NovosNegocios { get; set; }

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
    }
}
