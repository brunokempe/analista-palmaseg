using AnalistaPalmaseg.Core.Data;
using AnalistaPalmaseg.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace AnalistaPalmaseg.Core.Services;

public class PastaSalvarPropostaService(AppDbContext context)
{
    public async Task<List<PastaSalvarProposta>> GetTodosAsync() =>
        await context.PastasSalvarPropostas
            .AsNoTracking()
            .OrderBy(p => p.Caminho)
            .ToListAsync();

    public async Task<PastaSalvarProposta> AdicionarAsync(string caminho)
    {
        var existente = await context.PastasSalvarPropostas
            .FirstOrDefaultAsync(p => p.Caminho == caminho);
        if (existente != null) return existente;

        var pasta = new PastaSalvarProposta { Caminho = caminho, CriadoEm = DateTime.Now };
        context.PastasSalvarPropostas.Add(pasta);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();
        return pasta;
    }

    public async Task ExcluirAsync(int id) =>
        await context.Database.ExecuteSqlRawAsync(
            "DELETE FROM \"PastasSalvarPropostas\" WHERE \"Id\" = {0}", id);

    public async Task<(int Sucesso, int Erros)> SalvarArquivosAsync(IEnumerable<string> caminhosOrigem)
    {
        var pastas = await GetTodosAsync();
        int sucesso = 0, erros = 0;

        foreach (var origem in caminhosOrigem)
        {
            if (!File.Exists(origem)) { erros++; continue; }

            var falhouEmAlgumaPasta = false;
            foreach (var pasta in pastas)
            {
                try
                {
                    Directory.CreateDirectory(pasta.Caminho);
                    CopiarComRenomeSeNecessario(origem, pasta.Caminho);
                }
                catch (IOException)
                {
                    falhouEmAlgumaPasta = true;
                }
            }

            if (falhouEmAlgumaPasta) erros++;
            else sucesso++;
        }

        return (sucesso, erros);
    }

    private static void CopiarComRenomeSeNecessario(string caminhoOrigem, string diretorioDestino)
    {
        var nome = Path.GetFileName(caminhoOrigem);
        var dest = Path.Combine(diretorioDestino, nome);
        if (File.Exists(dest))
        {
            var stem = Path.GetFileNameWithoutExtension(nome);
            var ext = Path.GetExtension(nome);
            var n = 1;
            do { dest = Path.Combine(diretorioDestino, $"{stem}_{n++}{ext}"); }
            while (File.Exists(dest));
        }
        File.Copy(caminhoOrigem, dest, overwrite: false);
    }
}
