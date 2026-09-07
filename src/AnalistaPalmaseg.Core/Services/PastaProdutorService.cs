using AnalistaPalmaseg.Core.Data;
using AnalistaPalmaseg.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace AnalistaPalmaseg.Core.Services;

public class PastaProdutorService(IDbContextFactory<AppDbContext> contextFactory)
{
    public async Task<List<Usuario>> GetProdutoresAsync()
    {
        await using var context = await contextFactory.CreateDbContextAsync();
        return await context.Usuarios
            .AsNoTracking()
            .OrderBy(u => u.Login)
            .ToListAsync();
    }

    public async Task<List<PastaProdutor>> GetDiretoriosAsync(int usuarioId)
    {
        await using var context = await contextFactory.CreateDbContextAsync();
        return await context.PastasProdutor
            .AsNoTracking()
            .Where(p => p.UsuarioId == usuarioId)
            .OrderBy(p => p.Caminho)
            .ToListAsync();
    }

    public async Task<List<PastaProdutor>> GetDiretoriosPorLoginAsync(string? login)
    {
        if (string.IsNullOrWhiteSpace(login)) return [];

        await using var context = await contextFactory.CreateDbContextAsync();
        return await context.PastasProdutor
            .AsNoTracking()
            .Where(p => p.Usuario!.Login == login)
            .OrderBy(p => p.Caminho)
            .ToListAsync();
    }

    public async Task<PastaProdutor> AdicionarDiretorioAsync(int usuarioId, string caminho)
    {
        await using var context = await contextFactory.CreateDbContextAsync();

        var existente = await context.PastasProdutor
            .FirstOrDefaultAsync(p => p.UsuarioId == usuarioId && p.Caminho == caminho);
        if (existente != null) return existente;

        var pasta = new PastaProdutor { UsuarioId = usuarioId, Caminho = caminho, CriadoEm = DateTime.Now };
        context.PastasProdutor.Add(pasta);
        await context.SaveChangesAsync();
        return pasta;
    }

    public async Task RemoverDiretorioAsync(int id)
    {
        await using var context = await contextFactory.CreateDbContextAsync();
        await context.Database.ExecuteSqlRawAsync(
            "DELETE FROM \"PastasProdutor\" WHERE \"Id\" = {0}", id);
    }
}
