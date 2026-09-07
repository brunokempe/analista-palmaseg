using AnalistaPalmaseg.Core.Data;
using AnalistaPalmaseg.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace AnalistaPalmaseg.Core.Services;

public class SeguroNovoService(IDbContextFactory<AppDbContext> contextFactory)
{
    public async Task<List<SeguroNovo>> GetTodosAsync(string? produtor = null)
    {
        await using var context = await contextFactory.CreateDbContextAsync();
        return await context.SeguroNovos
            .AsNoTracking()
            .Where(x => produtor == null || x.CriadoPor == produtor)
            .OrderByDescending(x => x.CriadoEm)
            .ToListAsync();
    }

    public async Task<List<string>> GetProdutoresDistinctAsync()
    {
        await using var context = await contextFactory.CreateDbContextAsync();
        return await context.SeguroNovos
            .AsNoTracking()
            .Where(x => x.CriadoPor != null && x.CriadoPor != string.Empty)
            .Select(x => x.CriadoPor!)
            .Distinct()
            .OrderBy(x => x)
            .ToListAsync();
    }

    public async Task<SeguroNovo> SalvarAsync(SeguroNovo seguroNovo)
    {
        await using var context = await contextFactory.CreateDbContextAsync();

        if (seguroNovo.Id == 0)
            context.SeguroNovos.Add(seguroNovo);
        else
            context.SeguroNovos.Update(seguroNovo);

        await context.SaveChangesAsync();
        return seguroNovo;
    }

    public async Task SalvarStatusAdministrativoAsync(SeguroNovo seguroNovo)
    {
        await using var context = await contextFactory.CreateDbContextAsync();

        context.Attach(seguroNovo);
        var entry = context.Entry(seguroNovo);
        entry.Property(x => x.AssinaturaFeita).IsModified = true;
        entry.Property(x => x.SeguroEmitido).IsModified   = true;
        entry.Property(x => x.EmitidoPor).IsModified      = true;
        await context.SaveChangesAsync();
    }

    public async Task SalvarBoletosGeradosAsync(int id, int boletosGerados)
    {
        await using var context = await contextFactory.CreateDbContextAsync();

        var entidade = await context.SeguroNovos.FindAsync(id);
        if (entidade == null) return;
        entidade.BoletosGerados = boletosGerados;
        await context.SaveChangesAsync();
    }

    public async Task ExcluirAsync(int id)
    {
        await using var context = await contextFactory.CreateDbContextAsync();

        var entidade = await context.SeguroNovos.FindAsync(id);
        if (entidade != null)
        {
            context.SeguroNovos.Remove(entidade);
            await context.SaveChangesAsync();
        }
    }
}
