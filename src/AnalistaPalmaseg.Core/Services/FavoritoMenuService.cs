using AnalistaPalmaseg.Core.Data;
using AnalistaPalmaseg.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace AnalistaPalmaseg.Core.Services;

public class FavoritoMenuService(IDbContextFactory<AppDbContext> contextFactory)
{
    public async Task<List<string>> GetFavoritosAsync(int usuarioId)
    {
        await using var context = await contextFactory.CreateDbContextAsync();
        return await context.FavoritosMenu
            .AsNoTracking()
            .Where(f => f.UsuarioId == usuarioId)
            .Select(f => f.MenuKey)
            .ToListAsync();
    }

    public async Task<bool> AlternarFavoritoAsync(int usuarioId, string menuKey)
    {
        await using var context = await contextFactory.CreateDbContextAsync();

        var existente = await context.FavoritosMenu
            .FirstOrDefaultAsync(f => f.UsuarioId == usuarioId && f.MenuKey == menuKey);

        if (existente != null)
        {
            context.FavoritosMenu.Remove(existente);
            await context.SaveChangesAsync();
            return false;
        }

        context.FavoritosMenu.Add(new FavoritoMenu
        {
            UsuarioId = usuarioId,
            MenuKey = menuKey,
            CriadoEm = DateTime.Now
        });
        await context.SaveChangesAsync();
        return true;
    }
}
