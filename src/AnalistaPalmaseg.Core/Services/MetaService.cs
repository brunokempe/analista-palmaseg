using AnalistaPalmaseg.Core.Data;
using AnalistaPalmaseg.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace AnalistaPalmaseg.Core.Services;

public class MetaService(AppDbContext context)
{
    // ── Seguradoras ───────────────────────────────────────────────────────────

    public Task<List<Seguradora>> GetSeguradorasAsync(bool soAtivas = false) =>
        (soAtivas
            ? context.Seguradoras.Where(s => s.Ativo)
            : context.Seguradoras)
        .AsNoTracking()
        .OrderBy(s => !s.IsParceira).ThenBy(s => s.Nome)
        .ToListAsync();

    public async Task<Seguradora> SalvarSeguradoraAsync(Seguradora s)
    {
        if (s.Id == 0) context.Seguradoras.Add(s);
        else context.Seguradoras.Update(s);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();
        return s;
    }

    public async Task ExcluirSeguradoraAsync(int id)
    {
        var s = await context.Seguradoras.FindAsync(id)
            ?? throw new InvalidOperationException("Seguradora não encontrada.");
        context.Seguradoras.Remove(s);
        await context.SaveChangesAsync();
    }

    // ── Metas por seguradora ──────────────────────────────────────────────────

    public Task<List<MetaSeguradora>> GetMetasAsync(int mes, int ano) =>
        context.MetasSeguradoras
            .AsNoTracking()
            .Include(m => m.Seguradora)
            .Where(m => m.Mes == mes && m.Ano == ano)
            .OrderBy(m => m.Seguradora!.IsParceira ? 0 : 1)
            .ThenBy(m => m.Seguradora!.Nome)
            .ToListAsync();

    public async Task SalvarMetasAsync(List<MetaSeguradora> metas)
    {
        foreach (var meta in metas)
        {
            var existing = await context.MetasSeguradoras
                .FirstOrDefaultAsync(m => m.SeguradoraId == meta.SeguradoraId
                                       && m.Mes == meta.Mes
                                       && m.Ano == meta.Ano);
            if (existing == null)
                context.MetasSeguradoras.Add(meta);
            else
                existing.MetaPremio = meta.MetaPremio;
        }
        await context.SaveChangesAsync();
    }

    // ── Premiação ─────────────────────────────────────────────────────────────

    public Task<List<MetaPremiacao>> GetPremiacaoAsync() =>
        context.MetasPremiacao.AsNoTracking().OrderBy(p => p.Ordem).ToListAsync();

    public async Task SalvarPremiacaoAsync(List<MetaPremiacao> premiacoes)
    {
        var existentes = await context.MetasPremiacao.ToListAsync();
        context.MetasPremiacao.RemoveRange(existentes);

        for (var i = 0; i < premiacoes.Count; i++)
        {
            premiacoes[i].Id    = 0;
            premiacoes[i].Ordem = i + 1;
            context.MetasPremiacao.Add(premiacoes[i]);
        }

        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();
    }

    // ── Crescimento ───────────────────────────────────────────────────────────

    public Task<List<MetaCrescimento>> GetCrescimentoAsync() =>
        context.MetasCrescimento
            .AsNoTracking()
            .OrderBy(c => c.Tipo)
            .ThenBy(c => c.PercentualMeta)
            .ToListAsync();

    public async Task SalvarCrescimentoAsync(List<MetaCrescimento> crescimentos)
    {
        foreach (var c in crescimentos)
        {
            if (c.Id == 0) context.MetasCrescimento.Add(c);
            else context.MetasCrescimento.Update(c);
        }
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();
    }

    // ── Valores de referência ─────────────────────────────────────────────────

    public Task<ValorReferencia?> GetValorReferenciaAsync(int mes, int ano, string colaborador) =>
        context.ValoresReferencia.AsNoTracking()
            .FirstOrDefaultAsync(v => v.Colaborador == colaborador && v.Mes == mes && v.Ano == ano);

    public async Task<ValorReferencia> SalvarValorReferenciaAsync(ValorReferencia v)
    {
        var existing = await context.ValoresReferencia
            .FirstOrDefaultAsync(x => x.Colaborador == v.Colaborador && x.Mes == v.Mes && x.Ano == v.Ano);
        if (existing == null)
        {
            context.ValoresReferencia.Add(v);
            await context.SaveChangesAsync();
            return v;
        }
        existing.PremioTotal = v.PremioTotal;
        existing.ComissaoTotal = v.ComissaoTotal;
        await context.SaveChangesAsync();
        return existing;
    }

    // ── Colaboradores e posição atual (via RelatorioRenovacoes — Ren. Palma) ──

    public Task<List<string>> GetColaboradoresAsync(int mes, int ano) =>
        context.Usuarios
            .AsNoTracking()
            .Where(u => u.Ativo)
            .OrderBy(u => u.Login)
            .Select(u => u.Login)
            .ToListAsync();

    public async Task<(decimal PremioRen, decimal PremioNovos, decimal ComissaoCorretora)> GetPosicaoDetalhadaAsync(int mes, int ano, string? colaborador)
    {
        var inicio = new DateTime(ano, mes, 1);
        var fim    = inicio.AddMonths(1);

        var queryRen = context.RelatorioRenovacoes
            .AsNoTracking()
            .Where(r => r.SituacaoAcompanhamento == "Ren. Palma"
                     && r.VigenciaFinal.HasValue
                     && r.VigenciaFinal.Value.Month == mes
                     && r.VigenciaFinal.Value.Year  == ano);

        if (!string.IsNullOrWhiteSpace(colaborador))
            queryRen = queryRen.Where(r => r.NovoProdutor == colaborador);

        var resultadosRen = await queryRen.ToListAsync();

        var queryNovos = context.SeguroNovos
            .AsNoTracking()
            .Where(s => (s.Vigencia != null && s.Vigencia >= inicio && s.Vigencia < fim)
                     || (s.Vigencia == null  && s.CriadoEm >= inicio && s.CriadoEm < fim));

        if (!string.IsNullOrWhiteSpace(colaborador))
            queryNovos = queryNovos.Where(s => s.CriadoPor == colaborador);

        var resultadosNovos = await queryNovos.ToListAsync();

        var premioRen   = resultadosRen.Sum(r => r.FechamentoPremioLiquido ?? 0);
        var premioNovos = resultadosNovos.Sum(s => s.Valor ?? 0);
        var comissaoRen = resultadosRen.Sum(r =>
            Math.Round((r.FechamentoPremioLiquido ?? 0) * (r.FechamentoComissao ?? 0) / 100m, 2));
        var comissaoNovos = resultadosNovos.Sum(s =>
            Math.Round((s.Valor ?? 0) * (s.Fator ?? 0) / 100m, 2));

        return (premioRen, premioNovos, comissaoRen + comissaoNovos);
    }

    public async Task<(decimal RenPalma, decimal SegNovos)> GetComissaoColaboradorAsync(int mes, int ano, string? colaborador)
    {
        var inicio = new DateTime(ano, mes, 1);
        var fim    = inicio.AddMonths(1);

        var seguradoras     = await GetSeguradorasAsync(soAtivas: false);
        var metas           = await GetMetasAsync(mes, ano);
        var mapaMetasPremio = metas.ToDictionary(m => m.SeguradoraId, m => m.MetaPremio);

        var nomeToSeg = new Dictionary<string, Seguradora?>(StringComparer.OrdinalIgnoreCase);
        Seguradora? Resolver(string nome)
        {
            if (nomeToSeg.TryGetValue(nome, out var cached)) return cached;
            return nomeToSeg[nome] = seguradoras.FirstOrDefault(s =>
                s.Nome.Contains(nome, StringComparison.OrdinalIgnoreCase) ||
                nome.Contains(s.Nome, StringComparison.OrdinalIgnoreCase));
        }

        var queryRen = context.RelatorioRenovacoes
            .AsNoTracking()
            .Where(r => r.SituacaoAcompanhamento == "Ren. Palma"
                     && r.VigenciaFinal.HasValue
                     && r.VigenciaFinal.Value.Month == mes
                     && r.VigenciaFinal.Value.Year  == ano);

        if (!string.IsNullOrWhiteSpace(colaborador))
            queryRen = queryRen.Where(r => r.NovoProdutor == colaborador);

        var renPalmaList = await queryRen.ToListAsync();

        var queryNovos = context.SeguroNovos
            .AsNoTracking()
            .Where(s => (s.Vigencia != null && s.Vigencia >= inicio && s.Vigencia < fim)
                     || (s.Vigencia == null  && s.CriadoEm >= inicio && s.CriadoEm < fim));

        if (!string.IsNullOrWhiteSpace(colaborador))
            queryNovos = queryNovos.Where(s => s.CriadoPor == colaborador);

        var novosList = await queryNovos.ToListAsync();

        // Endosso segue a mesma regra de renovação (3-6% por parceira + meta)
        var endossos = novosList.Where(s => s.Status == "Endosso").ToList();
        var simples  = novosList.Where(s => s.Status != "Endosso").ToList();

        // Prêmio acumulado por (colab, seguradoraId): conta Ren. Palma + Endosso
        var premiosPorColabSeg = new Dictionary<(string, int), decimal>();
        foreach (var r in renPalmaList)
        {
            var seg = Resolver((r.FechamentoSeguradora ?? r.Seguradora ?? "").Trim());
            if (seg == null) continue;
            var k = (r.NovoProdutor ?? "", seg.Id);
            premiosPorColabSeg[k] = premiosPorColabSeg.GetValueOrDefault(k) + (r.FechamentoPremioLiquido ?? 0);
        }
        foreach (var s in endossos)
        {
            var seg = Resolver(s.Cia.Trim());
            if (seg == null) continue;
            var k = (s.CriadoPor ?? "", seg.Id);
            premiosPorColabSeg[k] = premiosPorColabSeg.GetValueOrDefault(k) + (s.Valor ?? 0);
        }

        // Helper: aplica regra parceira + meta
        decimal PctRenovacao(string colab, Seguradora? seg)
        {
            bool isParceira  = seg?.IsParceira ?? false;
            bool atingiuMeta = false;
            if (seg != null && mapaMetasPremio.TryGetValue(seg.Id, out var meta) && meta > 0)
                atingiuMeta = premiosPorColabSeg.GetValueOrDefault((colab, seg.Id)) >= meta;
            return (isParceira, atingiuMeta) switch
            {
                (true,  true)  => 6m,
                (true,  false) => 4m,
                (false, true)  => 4m,
                _              => 3m
            };
        }

        // Ren. Palma
        decimal totalRenPalma = 0m;
        foreach (var r in renPalmaList)
        {
            var seg = Resolver((r.FechamentoSeguradora ?? r.Seguradora ?? "").Trim());
            var comissaoCorretora = Math.Round(
                (r.FechamentoPremioLiquido ?? 0) * (r.FechamentoComissao ?? 0) / 100m, 2);
            totalRenPalma += Math.Round(comissaoCorretora * PctRenovacao(r.NovoProdutor ?? "", seg) / 100m, 2);
        }

        // Endosso: mesma regra de renovação
        decimal totalSegNovos = 0m;
        foreach (var s in endossos)
        {
            var seg = Resolver(s.Cia.Trim());
            var comissaoCorretora = Math.Round((s.Valor ?? 0) * (s.Fator ?? 0) / 100m, 2);
            totalSegNovos += Math.Round(comissaoCorretora * PctRenovacao(s.CriadoPor ?? "", seg) / 100m, 2);
        }

        // Demais status: Prospecção=15%, outros (Novo, Mensal, Mercado, Renovação)=10%
        totalSegNovos += simples.Sum(s =>
        {
            var comissaoCorretora = Math.Round((s.Valor ?? 0) * (s.Fator ?? 0) / 100m, 2);
            decimal pct = s.Status == "Prospecção" ? 15m : 10m;
            return Math.Round(comissaoCorretora * pct / 100m, 2);
        });

        return (totalRenPalma, totalSegNovos);
    }

    public async Task<Dictionary<string, decimal>> GetParticipacaoPorSeguradoraAsync(int mes, int ano, string? colaborador)
    {
        var inicio = new DateTime(ano, mes, 1);
        var fim    = inicio.AddMonths(1);

        var seguradoras     = await GetSeguradorasAsync(soAtivas: false);
        var metas           = await GetMetasAsync(mes, ano);
        var mapaMetasPremio = metas.ToDictionary(m => m.SeguradoraId, m => m.MetaPremio);

        var nomeToSeg = new Dictionary<string, Seguradora?>(StringComparer.OrdinalIgnoreCase);
        Seguradora? Resolver(string nome)
        {
            if (nomeToSeg.TryGetValue(nome, out var cached)) return cached;
            return nomeToSeg[nome] = seguradoras.FirstOrDefault(s =>
                s.Nome.Contains(nome, StringComparison.OrdinalIgnoreCase) ||
                nome.Contains(s.Nome, StringComparison.OrdinalIgnoreCase));
        }

        var queryRen = context.RelatorioRenovacoes.AsNoTracking()
            .Where(r => r.SituacaoAcompanhamento == "Ren. Palma"
                     && r.VigenciaFinal.HasValue
                     && r.VigenciaFinal.Value.Month == mes
                     && r.VigenciaFinal.Value.Year  == ano);
        if (!string.IsNullOrWhiteSpace(colaborador))
            queryRen = queryRen.Where(r => r.NovoProdutor == colaborador);
        var renPalmaList = await queryRen.ToListAsync();

        var queryNovos = context.SeguroNovos.AsNoTracking()
            .Where(s => (s.Vigencia != null && s.Vigencia >= inicio && s.Vigencia < fim)
                     || (s.Vigencia == null  && s.CriadoEm >= inicio && s.CriadoEm < fim));
        if (!string.IsNullOrWhiteSpace(colaborador))
            queryNovos = queryNovos.Where(s => s.CriadoPor == colaborador);
        var novosList = await queryNovos.ToListAsync();

        // Prêmio acumulado por (colab, segId) para calcular se atingiu meta
        var premiosPorColabSeg = new Dictionary<(string, int), decimal>();
        foreach (var r in renPalmaList)
        {
            var seg = Resolver((r.FechamentoSeguradora ?? r.Seguradora ?? "").Trim());
            if (seg == null) continue;
            var k = (r.NovoProdutor ?? "", seg.Id);
            premiosPorColabSeg[k] = premiosPorColabSeg.GetValueOrDefault(k) + (r.FechamentoPremioLiquido ?? 0);
        }
        foreach (var s in novosList.Where(x => x.Status == "Endosso"))
        {
            var seg = Resolver(s.Cia.Trim());
            if (seg == null) continue;
            var k = (s.CriadoPor ?? "", seg.Id);
            premiosPorColabSeg[k] = premiosPorColabSeg.GetValueOrDefault(k) + (s.Valor ?? 0);
        }

        decimal PctRenovacao(string colab, Seguradora? seg)
        {
            bool isParceira  = seg?.IsParceira ?? false;
            bool atingiuMeta = seg != null
                && mapaMetasPremio.TryGetValue(seg.Id, out var meta) && meta > 0
                && premiosPorColabSeg.GetValueOrDefault((colab, seg.Id)) >= meta;
            return (isParceira, atingiuMeta) switch
            {
                (true,  true)  => 6m,
                (true,  false) => 4m,
                (false, true)  => 4m,
                _              => 3m
            };
        }

        var resultado = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);

        foreach (var r in renPalmaList)
        {
            var segNome = (r.FechamentoSeguradora ?? r.Seguradora ?? "").Trim();
            if (string.IsNullOrEmpty(segNome)) continue;
            var seg = Resolver(segNome);
            var comissaoCorretora = Math.Round((r.FechamentoPremioLiquido ?? 0) * (r.FechamentoComissao ?? 0) / 100m, 2);
            resultado[segNome] = resultado.GetValueOrDefault(segNome)
                + Math.Round(comissaoCorretora * PctRenovacao(r.NovoProdutor ?? "", seg) / 100m, 2);
        }

        foreach (var s in novosList.Where(x => x.Status == "Endosso"))
        {
            var segNome = s.Cia.Trim();
            if (string.IsNullOrEmpty(segNome)) continue;
            var seg = Resolver(segNome);
            var comissaoCorretora = Math.Round((s.Valor ?? 0) * (s.Fator ?? 0) / 100m, 2);
            resultado[segNome] = resultado.GetValueOrDefault(segNome)
                + Math.Round(comissaoCorretora * PctRenovacao(s.CriadoPor ?? "", seg) / 100m, 2);
        }

        foreach (var s in novosList.Where(x => x.Status != "Endosso"))
        {
            var segNome = s.Cia.Trim();
            if (string.IsNullOrEmpty(segNome)) continue;
            var comissaoCorretora = Math.Round((s.Valor ?? 0) * (s.Fator ?? 0) / 100m, 2);
            decimal pct = s.Status == "Prospecção" ? 15m : 10m;
            resultado[segNome] = resultado.GetValueOrDefault(segNome)
                + Math.Round(comissaoCorretora * pct / 100m, 2);
        }

        return resultado;
    }

    public async Task<Dictionary<string, decimal>> GetPremiosPorSeguradoraPorColaboradorAsync(int mes, int ano, string? colaborador)
    {
        var inicio = new DateTime(ano, mes, 1);
        var fim    = inicio.AddMonths(1);

        var queryRen = context.RelatorioRenovacoes
            .AsNoTracking()
            .Where(r => r.SituacaoAcompanhamento == "Ren. Palma"
                     && r.VigenciaFinal.HasValue
                     && r.VigenciaFinal.Value.Month == mes
                     && r.VigenciaFinal.Value.Year  == ano);

        if (!string.IsNullOrWhiteSpace(colaborador))
            queryRen = queryRen.Where(r => r.NovoProdutor == colaborador);

        var resultadosRen = await queryRen.ToListAsync();

        var queryNovos = context.SeguroNovos
            .AsNoTracking()
            .Where(s => (s.Vigencia != null && s.Vigencia >= inicio && s.Vigencia < fim)
                     || (s.Vigencia == null  && s.CriadoEm >= inicio && s.CriadoEm < fim));

        if (!string.IsNullOrWhiteSpace(colaborador))
            queryNovos = queryNovos.Where(s => s.CriadoPor == colaborador);

        var resultadosNovos = await queryNovos.ToListAsync();

        var resultado = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);

        foreach (var g in resultadosRen
            .GroupBy(r => (r.FechamentoSeguradora ?? r.Seguradora ?? string.Empty).Trim(),
                     StringComparer.OrdinalIgnoreCase)
            .Where(g => !string.IsNullOrEmpty(g.Key)))
        {
            resultado[g.Key] = g.Sum(r => r.FechamentoPremioLiquido ?? 0);
        }

        foreach (var g in resultadosNovos
            .GroupBy(s => s.Cia.Trim(), StringComparer.OrdinalIgnoreCase)
            .Where(g => !string.IsNullOrEmpty(g.Key)))
        {
            var valor = g.Sum(s => s.Valor ?? 0);
            resultado[g.Key] = resultado.TryGetValue(g.Key, out var existente)
                ? existente + valor
                : valor;
        }

        return resultado;
    }
}
