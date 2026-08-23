using AnalistaPalmaseg.Core.Data;
using AnalistaPalmaseg.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace AnalistaPalmaseg.Core.Services;

public class SeguroNovoService(AppDbContext context)
{
    public async Task<List<SeguroNovo>> GetTodosAsync(string? produtor = null) =>
        await context.SeguroNovos
            .AsNoTracking()
            .Where(x => produtor == null || x.CriadoPor == produtor)
            .OrderByDescending(x => x.CriadoEm)
            .ToListAsync();

    public async Task<List<string>> GetProdutoresDistinctAsync() =>
        await context.SeguroNovos
            .AsNoTracking()
            .Where(x => x.CriadoPor != null && x.CriadoPor != string.Empty)
            .Select(x => x.CriadoPor!)
            .Distinct()
            .OrderBy(x => x)
            .ToListAsync();

    public async Task<SeguroNovo> SalvarAsync(SeguroNovo seguroNovo)
    {
        if (seguroNovo.Id == 0)
            context.SeguroNovos.Add(seguroNovo);
        else
            context.SeguroNovos.Update(seguroNovo);

        await context.SaveChangesAsync();
        context.Entry(seguroNovo).State = EntityState.Detached;
        return seguroNovo;
    }

    public async Task SalvarStatusAdministrativoAsync(SeguroNovo seguroNovo)
    {
        var entry = context.Entry(seguroNovo);
        if (entry.State == EntityState.Detached)
            context.Attach(seguroNovo);
        entry.Property(x => x.AssinaturaFeita).IsModified = true;
        entry.Property(x => x.SeguroEmitido).IsModified   = true;
        entry.Property(x => x.EmitidoPor).IsModified      = true;
        await context.SaveChangesAsync();
        entry.State = EntityState.Detached;
    }

    public async Task SalvarBoletosGeradosAsync(int id, int boletosGerados)
    {
        var entidade = await context.SeguroNovos.FindAsync(id);
        if (entidade == null) return;
        entidade.BoletosGerados = boletosGerados;
        await context.SaveChangesAsync();
        context.Entry(entidade).State = EntityState.Detached;
    }

    public async Task ExcluirAsync(int id)
    {
        var entidade = await context.SeguroNovos.FindAsync(id);
        if (entidade != null)
        {
            context.SeguroNovos.Remove(entidade);
            await context.SaveChangesAsync();
        }
    }
}
