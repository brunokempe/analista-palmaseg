using System.Security.Cryptography;
using System.Text;
using AnalistaPalmaseg.Core.Data;
using AnalistaPalmaseg.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace AnalistaPalmaseg.Core.Services;

public class UsuarioService(IDbContextFactory<AppDbContext> contextFactory)
{
    public static string HashSenha(string senha) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(senha)));

    public async Task<Usuario?> AutenticarAsync(string login, string senha)
    {
        await using var context = await contextFactory.CreateDbContextAsync();
        var hash = HashSenha(senha);
        return await context.Usuarios
            .FirstOrDefaultAsync(u => u.Login == login && u.SenhaHash == hash && u.Ativo);
    }

    public async Task<List<Usuario>> ListarAsync()
    {
        await using var context = await contextFactory.CreateDbContextAsync();
        return await context.Usuarios.OrderBy(u => u.Login).ToListAsync();
    }

    public async Task<bool> LoginExisteAsync(string login)
    {
        await using var context = await contextFactory.CreateDbContextAsync();
        return await context.Usuarios.AnyAsync(u => u.Login == login);
    }

    public async Task AdicionarAsync(string login, string senha, TipoAcesso tipo)
    {
        await using var context = await contextFactory.CreateDbContextAsync();
        context.Usuarios.Add(new Usuario
        {
            Login = login,
            SenhaHash = HashSenha(senha),
            TipoAcesso = tipo,
            Ativo = true
        });
        await context.SaveChangesAsync();
    }

    public async Task AlterarSenhaAsync(int id, string novaSenha)
    {
        await using var context = await contextFactory.CreateDbContextAsync();
        var u = await context.Usuarios.FindAsync(id)
            ?? throw new InvalidOperationException("Usuário não encontrado.");
        u.SenhaHash = HashSenha(novaSenha);
        await context.SaveChangesAsync();
    }

    public async Task ToggleAtivoAsync(int id)
    {
        await using var context = await contextFactory.CreateDbContextAsync();
        var u = await context.Usuarios.FindAsync(id)
            ?? throw new InvalidOperationException("Usuário não encontrado.");
        u.Ativo = !u.Ativo;
        await context.SaveChangesAsync();
    }

    public async Task RemoverAsync(int id)
    {
        await using var context = await contextFactory.CreateDbContextAsync();
        var u = await context.Usuarios.FindAsync(id);
        if (u != null)
        {
            context.Usuarios.Remove(u);
            await context.SaveChangesAsync();
        }
    }
}
