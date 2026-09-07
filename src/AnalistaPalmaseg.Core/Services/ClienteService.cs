using AnalistaPalmaseg.Core.Data;
using AnalistaPalmaseg.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace AnalistaPalmaseg.Core.Services;

public class ClienteService(IDbContextFactory<AppDbContext> contextFactory)
{
    public async Task<List<Cliente>> GetTodosAsync()
    {
        await using var context = await contextFactory.CreateDbContextAsync();
        return await context.Clientes
            .AsNoTracking()
            .OrderBy(c => c.Nome)
            .ToListAsync();
    }

    public async Task<List<RelatorioRenovacao>> GetSegurosDoClienteAsync(string cpf)
    {
        await using var context = await contextFactory.CreateDbContextAsync();
        return await context.RelatorioRenovacoes
            .AsNoTracking()
            .Where(r => r.DocumentoPrincipal == cpf)
            .OrderByDescending(r => r.VigenciaFinal)
            .ToListAsync();
    }

    public async Task<Cliente> SalvarAsync(Cliente cliente)
    {
        await using var context = await contextFactory.CreateDbContextAsync();

        if (cliente.Id == 0)
        {
            cliente.CriadoEm = DateTime.Now;
            context.Clientes.Add(cliente);
        }
        else
        {
            cliente.AtualizadoEm = DateTime.Now;
            context.Attach(cliente);
            context.Entry(cliente).State = EntityState.Modified;
        }

        await context.SaveChangesAsync();
        return cliente;
    }

    public async Task ExcluirAsync(int id)
    {
        await using var context = await contextFactory.CreateDbContextAsync();
        await context.Database.ExecuteSqlRawAsync(
            "DELETE FROM \"Clientes\" WHERE \"Id\" = {0}", id);
    }

    public async Task SincronizarClientesAsync(IEnumerable<RelatorioRenovacao> registros)
    {
        var validos = registros
            .Where(r => !string.IsNullOrWhiteSpace(r.DocumentoPrincipal) && !string.IsNullOrWhiteSpace(r.NomeCliente))
            .GroupBy(r => r.DocumentoPrincipal!)
            .Select(g => g.OrderByDescending(r => r.ImportadoEm).First())
            .ToList();

        if (validos.Count == 0) return;

        await using var context = await contextFactory.CreateDbContextAsync();

        var cpfs = validos.Select(r => r.DocumentoPrincipal!).ToList();
        var existentes = await context.Clientes
            .Where(c => cpfs.Contains(c.Cpf))
            .ToDictionaryAsync(c => c.Cpf);

        var agora = DateTime.Now;
        foreach (var reg in validos)
        {
            var cpf = reg.DocumentoPrincipal!;
            if (existentes.TryGetValue(cpf, out var existente))
            {
                // Atualiza dados básicos vindos da planilha; preserva Observacoes e Historico
                AtualizarDadosDaRenovacao(existente, reg);
                existente.AtualizadoEm = agora;
            }
            else
            {
                var novo = new Cliente { Cpf = cpf, CriadoEm = agora };
                AtualizarDadosDaRenovacao(novo, reg);
                context.Clientes.Add(novo);
            }
        }

        await context.SaveChangesAsync();
    }

    private static void AtualizarDadosDaRenovacao(Cliente c, RelatorioRenovacao reg)
    {
        c.Nome         = reg.NomeCliente ?? c.Nome;
        c.Nascimento   = reg.Nascimento  ?? c.Nascimento;
        c.Sexo         = reg.Sexo        ?? c.Sexo;
        c.EstadoCivil  = reg.EstadoCivil ?? c.EstadoCivil;
        c.Profissao    = reg.Profissao   ?? c.Profissao;
        c.ClienteDesde = reg.ClienteDesde ?? c.ClienteDesde;
        c.Prefixo1     = reg.Prefixo1    ?? c.Prefixo1;
        c.Telefone1    = reg.Telefone1   ?? c.Telefone1;
        c.Prefixo2     = reg.Prefixo2    ?? c.Prefixo2;
        c.Telefone2    = reg.Telefone2   ?? c.Telefone2;
        c.Prefixo3     = reg.Prefixo3    ?? c.Prefixo3;
        c.Telefone3    = reg.Telefone3   ?? c.Telefone3;
        c.Email1       = reg.Email1      ?? c.Email1;
        c.Email2       = reg.Email2      ?? c.Email2;
        c.Cep          = reg.Cep         ?? c.Cep;
        c.Endereco     = reg.Endereco    ?? c.Endereco;
        c.NumeroEndereco = reg.NumeroEndereco ?? c.NumeroEndereco;
        c.Complemento  = reg.Complemento ?? c.Complemento;
        c.Bairro       = reg.Bairro      ?? c.Bairro;
        c.Cidade       = reg.Cidade      ?? c.Cidade;
        c.Estado       = reg.Estado      ?? c.Estado;
    }
}
