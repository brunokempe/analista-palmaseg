using System.Security.Cryptography;
using System.Text;
using AnalistaPalmaseg.Core.Data;
using AnalistaPalmaseg.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace AnalistaPalmaseg.Core.Services;

public class UsuarioService(AppDbContext db)
{
    public static string HashSenha(string senha) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(senha)));

    public async Task<Usuario?> AutenticarAsync(string login, string senha)
    {
        var hash = HashSenha(senha);
        return await db.Usuarios
            .FirstOrDefaultAsync(u => u.Login == login && u.SenhaHash == hash && u.Ativo);
    }

    public async Task<List<Usuario>> ListarAsync() =>
        await db.Usuarios.OrderBy(u => u.Login).ToListAsync();

    public async Task<bool> LoginExisteAsync(string login) =>
        await db.Usuarios.AnyAsync(u => u.Login == login);

    public async Task AdicionarAsync(string login, string senha, TipoAcesso tipo)
    {
        db.Usuarios.Add(new Usuario
        {
            Login = login,
            SenhaHash = HashSenha(senha),
            TipoAcesso = tipo,
            Ativo = true
        });
        await db.SaveChangesAsync();
    }

    public async Task AlterarSenhaAsync(int id, string novaSenha)
    {
        var u = await db.Usuarios.FindAsync(id)
            ?? throw new InvalidOperationException("Usuário não encontrado.");
        u.SenhaHash = HashSenha(novaSenha);
        await db.SaveChangesAsync();
    }

    public async Task ToggleAtivoAsync(int id)
    {
        var u = await db.Usuarios.FindAsync(id)
            ?? throw new InvalidOperationException("Usuário não encontrado.");
        u.Ativo = !u.Ativo;
        await db.SaveChangesAsync();
    }

    public async Task RemoverAsync(int id)
    {
        var u = await db.Usuarios.FindAsync(id);
        if (u != null)
        {
            db.Usuarios.Remove(u);
            await db.SaveChangesAsync();
        }
    }
}
