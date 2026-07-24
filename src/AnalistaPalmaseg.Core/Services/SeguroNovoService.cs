using AnalistaPalmaseg.Core.Data;
using AnalistaPalmaseg.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace AnalistaPalmaseg.Core.Services;

public class SeguroNovoService(AppDbContext context)
{
    public async Task<List<SeguroNovo>> GetTodosAsync() =>
        await context.SeguroNovos
            .AsNoTracking()
            .OrderByDescending(x => x.CriadoEm)
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
