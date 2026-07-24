using AnalistaPalmaseg.Core.Data;
using AnalistaPalmaseg.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace AnalistaPalmaseg.Core.Services;

public class AnexoService(AppDbContext context)
{
    private static string BaseDir => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "AnalistaPalmaseg", "Anexos");

    public static string ObterPasta(int relatorioId) =>
        Path.Combine(BaseDir, relatorioId.ToString());

    public async Task<List<Anexo>> GetAnexosAsync(int relatorioId) =>
        await context.Anexos
            .Where(a => a.RelatorioRenovacaoId == relatorioId)
            .OrderBy(a => a.AdicionadoEm)
            .ToListAsync();

    public async Task<Dictionary<int, List<Anexo>>> GetAnexosParaRegistrosAsync(IList<int> ids) =>
        (await context.Anexos
            .Where(a => ids.Contains(a.RelatorioRenovacaoId))
            .ToListAsync())
            .GroupBy(a => a.RelatorioRenovacaoId)
            .ToDictionary(g => g.Key, g => g.ToList());

    public async Task<Anexo> AdicionarAsync(int relatorioId, string caminhoOrigem)
    {
        var nomeSrc = Path.GetFileName(caminhoOrigem);
        var ext = Path.GetExtension(caminhoOrigem);
        var dirDest = ObterPasta(relatorioId);
        Directory.CreateDirectory(dirDest);

        var stamp = DateTime.Now.ToString("yyyyMMddHHmmss");
        var nomeDest = $"{Path.GetFileNameWithoutExtension(nomeSrc)}_{stamp}{ext}";
        var caminhoDest = Path.Combine(dirDest, nomeDest);
        File.Copy(caminhoOrigem, caminhoDest, overwrite: false);

        var anexo = new Anexo
        {
            RelatorioRenovacaoId = relatorioId,
            NomeArquivo = nomeSrc,
            CaminhoArquivo = caminhoDest,
            TamanhoBytes = new FileInfo(caminhoOrigem).Length,
            AdicionadoEm = DateTime.Now
        };
        context.Anexos.Add(anexo);
        await context.SaveChangesAsync();
        return anexo;
    }

    public static void CopiarParaDiretorio(IEnumerable<Anexo> anexos, string diretorio)
    {
        foreach (var a in anexos)
        {
            if (!File.Exists(a.CaminhoArquivo)) continue;
            var dest = Path.Combine(diretorio, a.NomeArquivo);
            if (File.Exists(dest))
            {
                var stem = Path.GetFileNameWithoutExtension(a.NomeArquivo);
                var ext2 = Path.GetExtension(a.NomeArquivo);
                var n = 1;
                do { dest = Path.Combine(diretorio, $"{stem}_{n++}{ext2}"); }
                while (File.Exists(dest));
            }
            File.Copy(a.CaminhoArquivo, dest, overwrite: false);
        }
    }
}
