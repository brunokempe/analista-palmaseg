using AnalistaPalmaseg.Core.Data;
using AnalistaPalmaseg.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace AnalistaPalmaseg.Core.Services;

public class DistribuicaoReferenciaService(IDbContextFactory<AppDbContext> contextFactory)
{
    public async Task<DistribuicaoReferencia?> GetByAnoAsync(int ano)
    {
        await using var context = await contextFactory.CreateDbContextAsync();
        return await context.DistribuicaoReferencias.AsNoTracking().FirstOrDefaultAsync(r => r.Ano == ano);
    }

    public async Task SalvarAsync(DistribuicaoReferencia referencia)
    {
        await using var context = await contextFactory.CreateDbContextAsync();

        var existente = await context.DistribuicaoReferencias
            .FirstOrDefaultAsync(r => r.Ano == referencia.Ano);
        if (existente != null)
            context.Entry(existente).CurrentValues.SetValues(referencia);
        else
            context.DistribuicaoReferencias.Add(referencia);
        await context.SaveChangesAsync();
    }
}
