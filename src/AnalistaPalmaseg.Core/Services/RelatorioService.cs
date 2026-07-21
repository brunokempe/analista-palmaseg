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
    decimal Retencao
);

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

        var total = renovacoes.Sum(r => r.NovoPl ?? 0);

        return renovacoes
            .GroupBy(r => r.CiaRenovada ?? r.Cia)
            .Select(g => new ParticipacaoSeguradora(
                g.Key,
                g.Sum(r => r.NovoPl ?? 0),
                g.Sum(r => r.NovaComissao ?? 0),
                total > 0 ? g.Sum(r => r.NovoPl ?? 0) / total * 100m : 0
            ))
            .OrderByDescending(p => p.PlRenovado)
            .ToList();
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
            retencao
        );
    }
}
