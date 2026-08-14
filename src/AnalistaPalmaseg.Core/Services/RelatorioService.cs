using AnalistaPalmaseg.Core.Data;
using AnalistaPalmaseg.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace AnalistaPalmaseg.Core.Services;

public record ResumoImportacao(
    Importacao Importacao,
    int TotalVencidas,
    int RenovadasPalma,
    int Pendentes,
    int NaoRenovado,
    decimal PlBase,
    decimal PlRenovado,
    int NovosQtd,
    decimal NovosPl,
    decimal Participacao,
    decimal Retencao,
    decimal ComissaoRenovacoes
)
{
    public decimal TotalPl => PlRenovado + NovosPl;
    public decimal TotalComissao => ComissaoRenovacoes + Participacao;
};

public record ParticipacaoSeguradora(string Cia, decimal PlRenovado, decimal Comissao, decimal Percentual);

public class RelatorioService(AppDbContext context)
{
    public async Task<List<ResumoImportacao>> GetResumoAsync()
    {
        var importacoes = await context.Importacoes
            .Include(i => i.Renovacoes)
            .Include(i => i.NovosNegocios)
            .OrderByDescending(i => i.Ano).ThenByDescending(i => i.Mes)
            .ToListAsync();

        return importacoes.Select(CalcularResumo).ToList();
    }

    public async Task<ResumoImportacao?> GetResumoAsync(int importacaoId)
    {
        var importacao = await context.Importacoes
            .Include(i => i.Renovacoes)
            .Include(i => i.NovosNegocios)
            .FirstOrDefaultAsync(i => i.Id == importacaoId);

        return importacao == null ? null : CalcularResumo(importacao);
    }

    public async Task<List<Renovacao>> GetRenovacoesAsync(int importacaoId)
    {
        return await context.Renovacoes
            .Where(r => r.ImportacaoId == importacaoId)
            .OrderBy(r => r.Vigencia)
            .ToListAsync();
    }

    public async Task<List<Renovacao>> GetPendentesAsync(int importacaoId)
    {
        return await context.Renovacoes
            .Where(r => r.ImportacaoId == importacaoId && (r.Status == "Procurado" || r.Status == "Pendente" || r.Status == "Agendado"))
            .OrderBy(r => r.Vigencia)
            .ToListAsync();
    }

    public async Task<List<NovoNegocio>> GetNovosNegociosAsync(int importacaoId)
    {
        return await context.NovosNegocios
            .Where(n => n.ImportacaoId == importacaoId)
            .OrderBy(n => n.Vigencia)
            .ToListAsync();
    }

    public async Task<List<ParticipacaoSeguradora>> GetParticipacaoAsync(int importacaoId)
    {
        var renovacoes = await context.Renovacoes
            .Where(r => r.ImportacaoId == importacaoId && r.NovoPl.HasValue)
            .ToListAsync();

        var novos = await context.NovosNegocios
            .Where(n => n.ImportacaoId == importacaoId)
            .ToListAsync();

        // Combine renewals and new business by insurer
        var renPorCia = renovacoes
            .GroupBy(r => r.CiaRenovada ?? r.Cia)
            .Select(g => (Cia: g.Key, Pl: g.Sum(r => r.NovoPl ?? 0), Com: g.Sum(r => r.NovaComissao ?? 0)));

        var novosPorCia = novos
            .GroupBy(n => n.Cia)
            .Select(g => (Cia: g.Key, Pl: g.Sum(n => n.Pl), Com: g.Sum(n => n.Comissao)));

        var combined = renPorCia.Concat(novosPorCia)
            .GroupBy(x => x.Cia)
            .Select(g => (Cia: g.Key, Pl: g.Sum(x => x.Pl), Com: g.Sum(x => x.Com)))
            .ToList();

        var total = combined.Sum(x => x.Pl);

        return combined
            .Select(x => new ParticipacaoSeguradora(
                x.Cia,
                x.Pl,
                x.Com,
                total > 0 ? x.Pl / total * 100m : 0))
            .OrderByDescending(p => p.PlRenovado)
            .ToList();
    }

    public async Task<List<FuncionarioResultado>> GetFuncionariosAsync(int importacaoId)
    {
        var lista = await context.FuncionariosResultados
            .Where(f => f.ImportacaoId == importacaoId)
            .ToListAsync();
        return [.. lista.OrderByDescending(f => f.Premio)];
    }

    public async Task<List<string>> GetFuncionariosNomesAsync()
    {
        return await context.FuncionariosResultados
            .Select(f => f.Nome)
            .Distinct()
            .OrderBy(n => n)
            .ToListAsync();
    }

    public async Task<List<(Importacao Importacao, List<FuncionarioResultado> Resultados)>> GetTimelineFuncionarioAsync(string nome)
    {
        var importacoes = await context.Importacoes
            .Where(i => i.Produtor == nome)
            .Include(i => i.FuncionariosResultados)
            .OrderByDescending(i => i.Ano).ThenByDescending(i => i.Mes)
            .ToListAsync();

        return importacoes
            .Where(i => i.FuncionariosResultados.Count > 0)
            .Select(i => (i, i.FuncionariosResultados.OrderByDescending(f => f.Premio).ToList()))
            .ToList();
    }

    public async Task<List<ResultadoMeta>> GetResultadosAsync(int importacaoId)
    {
        var lista = await context.Resultados
            .Where(r => r.ImportacaoId == importacaoId && r.Funcionario != "TOTAIS")
            .ToListAsync();

        return [.. lista.OrderByDescending(r => r.PercentualAtingimento)];
    }

    public async Task<List<(string Periodo, decimal Retencao)>> GetEvolucaoRetencaoAsync(string produtor)
    {
        var importacoes = await context.Importacoes
            .Include(i => i.Renovacoes)
            .Where(i => i.Produtor == produtor)
            .OrderBy(i => i.Ano).ThenBy(i => i.Mes)
            .ToListAsync();

        return importacoes
            .Select(i =>
            {
                var resumo = CalcularResumo(i);
                return (i.Periodo, resumo.Retencao);
            })
            .ToList();
    }

    public async Task<List<string>> GetProdutoresAsync()
    {
        return await context.Importacoes
            .Select(i => i.Produtor)
            .Distinct()
            .OrderBy(p => p)
            .ToListAsync();
    }

    public async Task<List<(ResumoImportacao Resumo, List<Renovacao> Renovacoes)>> GetTimelineRenovacoesAsync(string produtor)
    {
        var importacoes = await context.Importacoes
            .Where(i => i.Produtor == produtor)
            .Include(i => i.Renovacoes)
            .Include(i => i.NovosNegocios)
            .OrderByDescending(i => i.Ano).ThenByDescending(i => i.Mes)
            .ToListAsync();

        return importacoes
            .Select(i => (CalcularResumo(i), i.Renovacoes.OrderBy(r => r.Vigencia).ToList()))
            .ToList();
    }

    public async Task<List<(ResumoImportacao Resumo, List<NovoNegocio> Negocios)>> GetTimelineNegociosAsync(string produtor)
    {
        var importacoes = await context.Importacoes
            .Where(i => i.Produtor == produtor)
            .Include(i => i.Renovacoes)
            .Include(i => i.NovosNegocios)
            .OrderByDescending(i => i.Ano).ThenByDescending(i => i.Mes)
            .ToListAsync();

        return importacoes
            .Select(i => (CalcularResumo(i), i.NovosNegocios.OrderBy(n => n.Vigencia).ToList()))
            .ToList();
    }

    public async Task<List<(ResumoImportacao Resumo, List<Renovacao> Pendentes)>> GetTimelinePendentesAsync(string produtor)
    {
        var importacoes = await context.Importacoes
            .Where(i => i.Produtor == produtor)
            .Include(i => i.Renovacoes)
            .Include(i => i.NovosNegocios)
            .OrderByDescending(i => i.Ano).ThenByDescending(i => i.Mes)
            .ToListAsync();

        return importacoes
            .Select(i => (
                CalcularResumo(i),
                i.Renovacoes
                    .Where(r => r.Status == "Procurado" || r.Status == "Pendente" || r.Status == "Agendado")
                    .OrderBy(r => r.Vigencia)
                    .ToList()))
            .ToList();
    }

    private static ResumoImportacao CalcularResumo(Importacao importacao)
    {
        var renovacoes = importacao.Renovacoes.ToList();
        var novos = importacao.NovosNegocios.ToList();

        var totalVencidas = renovacoes.Count;
        var renovadasPalma = renovacoes.Count(r => r.Status == "Ren.Palma");
        var pendentes = renovacoes.Count(r => r.Status is "Procurado" or "Pendente" or "Agendado");
        var naoRenovado = renovacoes.Count(r => r.Status == "Não renov" || r.Status == "Não renovado");

        var plBase = renovacoes.Sum(r => r.PlBase);
        var plRenovado = renovacoes.Where(r => r.NovoPl.HasValue).Sum(r => r.NovoPl ?? 0);
        var comissaoRenovacoes = renovacoes.Where(r => r.NovoPl.HasValue).Sum(r => r.NovaComissao ?? 0);

        var novosApenas = novos.Where(n => n.IsNovo).ToList();
        var novosQtd = novosApenas.Count;
        var novosPl = novos.Sum(n => n.Pl);

        var participacao = novos.Sum(n => n.Comissao);

        var retencao = totalVencidas > 0 ? (decimal)renovadasPalma / totalVencidas * 100m : 0;

        return new ResumoImportacao(
            importacao,
            totalVencidas,
            renovadasPalma,
            pendentes,
            naoRenovado,
            plBase,
            plRenovado,
            novosQtd,
            novosPl,
            participacao,
            retencao,
            comissaoRenovacoes
        );
    }
}
