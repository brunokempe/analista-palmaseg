using System.Data;
using AnalistaPalmaseg.Core.Data;
using AnalistaPalmaseg.Core.Models;
using ExcelDataReader;
using Microsoft.EntityFrameworkCore;

namespace AnalistaPalmaseg.Core.Services;

public class RelatorioRenovacaoService(AppDbContext context)
{
    public async Task<int> ImportarAsync(string caminhoArquivo)
    {
        System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);

        DataSet dataSet;
        using (var stream = File.Open(caminhoArquivo, FileMode.Open, FileAccess.Read, FileShare.Read))
        {
            var reader = ExcelReaderFactory.CreateReader(stream);
            dataSet = reader.AsDataSet(new ExcelDataSetConfiguration
            {
                ConfigureDataTable = _ => new ExcelDataTableConfiguration { UseHeaderRow = false }
            });
            reader.Dispose();
        }

        var table = dataSet.Tables[0];
        if (table.Rows.Count < 2) return 0;

        // Mapeia nomes de coluna -> índice a partir da linha de cabeçalho (linha 0)
        var colIdx = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        for (int c = 0; c < table.Columns.Count; c++)
        {
            var nome = table.Rows[0][c]?.ToString()?.Trim() ?? string.Empty;
            if (!string.IsNullOrEmpty(nome) && !colIdx.ContainsKey(nome))
                colIdx[nome] = c;
        }

        var agora = DateTime.Now;
        var origem = Path.GetFileName(caminhoArquivo);
        var registros = new List<RelatorioRenovacao>();

        for (int r = 1; r < table.Rows.Count; r++)
        {
            var row = table.Rows[r];
            var proposta = S(row, "PROPOSTA");
            if (string.IsNullOrWhiteSpace(proposta)) continue;

            registros.Add(new RelatorioRenovacao
            {
                Proposta          = proposta,
                Apolice           = S(row, "APÓLICE"),
                PedidoEndosso     = S(row, "PEDIDO ENDOSSO"),
                Endosso           = S(row, "ENDOSSO"),
                Status            = S(row, "STATUS"),
                TipoRecebimento   = S(row, "TIPO DO RECEBIMENTO"),
                Emissao           = Dt(row, "EMISSÃO"),
                VigenciaInicial   = Dt(row, "VIGÊNCIA INICIAL"),
                VigenciaFinal     = Dt(row, "VIGÊNCIA FINAL"),
                Transmissao       = Dt(row, "TRANSMISSÃO"),
                DataControle      = Dt(row, "DATA CONTROLE"),
                Comissao          = Dec(row, "COMISSÃO"),
                ComissaoGerada    = Dec(row, "COMISSÃO GERADA"),
                LiquidoFatura     = Dec(row, "LÍQUIDO FATURA"),
                PremioLiquido     = Dec(row, "PRÊMIO LÍQUIDO"),
                PremioTotal       = Dec(row, "PRÊMIO TOTAL"),
                TotalFatura       = Dec(row, "TOTAL FATURA"),
                ComissaoAdicional = Dec(row, "COMISSÃO ADICIONAL"),
                PremioAdicional   = Dec(row, "PRÊMIO ADICIONAL"),
                PremioCusto       = Dec(row, "PRÊMIO CUSTO"),
                Iof               = Dec(row, "IOF"),
                NumeroParcelas    = Num(row, "NÚMERO DE PARCELAS"),
                ValorParcelas     = Dec(row, "VALOR PARCELAS"),
                FormaPagamento    = S(row, "FORMA DE PAGAMENTO"),
                Seguradora        = S(row, "SEGURADORA"),
                Ramo              = S(row, "RAMO"),
                SeguradoraAnterior = S(row, "SEGURADORA ANTERIOR"),
                NegocioCorretora  = S(row, "NEGÓCIO CORRETORA"),
                CodigoDocumento   = S(row, "CÓDIGO DOCUMENTO"),
                VendedorPrincipal = S(row, "VENDEDOR PRINCIPAL"),
                Produto           = S(row, "PRODUTO"),
                QuantidadeSinistros = Num(row, "QUANTIDADE DE SINISTROS"),
                FranquiaApolice   = Dec(row, "FRANQUIA DA APÓLICE"),
                QuantidadeEndossos = Num(row, "QUANTIDADE DE ENDOSSOS"),
                ObservacaoDocumento = S(row, "OBSERVAÇÃO DOCUMENTO"),
                NomeCliente       = S(row, "NOME DO CLIENTE"),
                Nascimento        = Dt(row, "NASCIMENTO"),
                Sexo              = S(row, "SEXO"),
                EstadoCivil       = S(row, "ESTADO CIVIL"),
                DocumentoPrincipal = S(row, "DOCUMENTO PRINCIPAL"),
                ClienteDesde      = Dt(row, "CLIENTE DESDE"),
                Profissao         = S(row, "PROFISSÃO"),
                Prefixo1          = S(row, "PREFIXO1"),
                Telefone1         = S(row, "TELEFONE1"),
                Prefixo2          = S(row, "PREFIXO2"),
                Telefone2         = S(row, "TELEFONE2"),
                Prefixo3          = S(row, "PREFIXO3"),
                Telefone3         = S(row, "TELEFONE3"),
                Email1            = S(row, "E-MAIL1"),
                Email2            = S(row, "E-MAIL2"),
                Cep               = S(row, "CEP"),
                Endereco          = S(row, "ENDEREÇO"),
                NumeroEndereco    = S(row, "NÚMERO"),
                Complemento       = S(row, "COMPLEMENTO"),
                Bairro            = S(row, "BAIRRO"),
                Cidade            = S(row, "CIDADE"),
                Estado            = S(row, "ESTADO"),
                Banco             = S(row, "BANCO"),
                Agencia           = S(row, "AGÊNCIA"),
                Conta             = S(row, "CONTA"),
                Falecido          = S(row, "FALECIDO"),
                Observacao        = S(row, "OBSERVAÇÃO"),
                Pasta             = S(row, "PASTA"),
                DescricaoItem     = S(row, "DESCRIÇÃO DO ITEM"),
                StatusItem        = S(row, "STATUS DO ITEM"),
                CodigoFipe        = S(row, "CÓDIGO FIPE"),
                Combustivel       = S(row, "COMBUSTÍVEL"),
                Modelo            = S(row, "MODELO"),
                Fabricante        = S(row, "FABRICANTE"),
                Categoria         = S(row, "CATEGORIA"),
                Chassi            = S(row, "CHASSI"),
                Placa             = S(row, "PLACA"),
                AnoFabricacao     = NulNum(row, "ANO DE FABRICAÇÃO"),
                AnoModelo         = NulNum(row, "ANO DO MODELO"),
                Renavam           = S(row, "RENAVAM"),
                Cor               = S(row, "COR"),
                Bonus             = S(row, "BÔNUS"),
                ValorDeterminado  = NulDec(row, "VALOR DETERMINADO"),
                CepPernoite       = S(row, "CEP PERNOITE"),
                Financiado        = S(row, "FINANCIADO"),
                ZeroKm            = S(row, "ZERO KM"),
                DanosMateriasPremio     = Dec(row, "DANOS MATERIAIS PREMIO"),
                DanosMaterialLmi        = Dec(row, "DANOS MATERIAIS LMI"),
                DanosMaterialFranquia   = Dec(row, "DANOS MATERIAIS FRANQUIA"),
                DanosMoraisPremio       = Dec(row, "DANOS MORAIS PREMIO"),
                DanosMoraisLmi          = Dec(row, "DANOS MORAIS LMI"),
                DanosMoraisFranquia     = Dec(row, "DANOS MORAIS FRANQUIA"),
                DanosCorporaisPremio    = Dec(row, "DANOS CORPORAIS PREMIO"),
                DanosCorporaisLmi       = Dec(row, "DANOS CORPORAIS LMI"),
                DanosCorporaisFranquia  = Dec(row, "DANOS CORPORAIS FRANQUIA"),
                AcidentesPassageiroPremio   = Dec(row, "ACIDENTES PASSAGEIRO PREMIO"),
                AcidentesPassageiroLmi      = Dec(row, "ACIDENTES PASSAGEIRO LMI"),
                AcidentesPassageiroFranquia = Dec(row, "ACIDENTES PASSAGEIRO FRANQUIA"),
                ImportadoEm  = agora,
                ArquivoOrigem = origem
            });
        }

        // Deduplica por Proposta (mantém última ocorrência), evitando violação do índice único
        // quando a planilha contém linhas repetidas com a mesma proposta.
        var registrosPorProposta = registros
            .GroupBy(r => r.Proposta)
            .Select(g => g.Last())
            .ToList();

        var propostas = registrosPorProposta
            .Select(r => r.Proposta)
            .ToList();

        var existentes = await context.RelatorioRenovacoes
            .Where(r => propostas.Contains(r.Proposta))
            .ToDictionaryAsync(r => r.Proposta!);

        int inseridos = 0;
        foreach (var reg in registrosPorProposta)
        {
            if (reg.Proposta != null && existentes.TryGetValue(reg.Proposta, out var existing))
            {
                // Preserva campos editados manualmente antes de sobrescrever com dados da planilha
                var novoProdutor             = existing.NovoProdutor;
                var observacao               = existing.Observacao;
                var situacaoAcompanhamento   = existing.SituacaoAcompanhamento;
                var fechamentoSeguradora     = existing.FechamentoSeguradora;
                var fechamentoPremioLiquido  = existing.FechamentoPremioLiquido;
                var fechamentoFormaPagamento = existing.FechamentoFormaPagamento;
                var fechamentoComissao       = existing.FechamentoComissao;
                var fechamentoParcelamento   = existing.FechamentoParcelamento;
                var fechamentoAssinatura     = existing.FechamentoAssinatura;
                var assinaturaFeita          = existing.AssinaturaFeita;
                var seguroEmitido            = existing.SeguroEmitido;
                var id                       = existing.Id;
                context.Entry(existing).CurrentValues.SetValues(reg);
                existing.Id                       = id;
                existing.NovoProdutor             = novoProdutor;
                existing.Observacao               = observacao;
                existing.SituacaoAcompanhamento   = situacaoAcompanhamento;
                existing.FechamentoSeguradora     = fechamentoSeguradora;
                existing.FechamentoPremioLiquido  = fechamentoPremioLiquido;
                existing.FechamentoFormaPagamento = fechamentoFormaPagamento;
                existing.FechamentoComissao       = fechamentoComissao;
                existing.FechamentoParcelamento   = fechamentoParcelamento;
                existing.FechamentoAssinatura     = fechamentoAssinatura;
                existing.AssinaturaFeita          = assinaturaFeita;
                existing.SeguroEmitido            = seguroEmitido;
            }
            else
            {
                context.RelatorioRenovacoes.Add(reg);
                inseridos++;
            }
        }

        await context.SaveChangesAsync();
        return inseridos;

        // ── helpers ──────────────────────────────────────────────
        string? S(DataRow row, string col)
        {
            if (!colIdx.TryGetValue(col, out var i)) return null;
            var v = row[i]?.ToString()?.Trim();
            return string.IsNullOrEmpty(v) ? null : v;
        }

        DateTime? Dt(DataRow row, string col)
        {
            if (!colIdx.TryGetValue(col, out var i)) return null;
            var val = row[i];
            if (val == null || val == DBNull.Value) return null;
            if (val is DateTime dt) return dt;
            if (val is double d && d > 0) return DateTime.FromOADate(d);
            if (DateTime.TryParse(val.ToString(), out var p)) return p;
            return null;
        }

        decimal Dec(DataRow row, string col)
        {
            if (!colIdx.TryGetValue(col, out var i)) return 0;
            var val = row[i];
            if (val == null || val == DBNull.Value) return 0;
            if (val is decimal dc) return dc;
            if (val is double db) return (decimal)db;
            if (val is bool b) return b ? 1 : 0;
            if (decimal.TryParse(val.ToString(), System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture, out var p)) return p;
            return 0;
        }

        decimal? NulDec(DataRow row, string col)
        {
            var d = Dec(row, col);
            return d == 0 ? null : d;
        }

        int Num(DataRow row, string col)
        {
            if (!colIdx.TryGetValue(col, out var i)) return 0;
            var val = row[i];
            if (val == null || val == DBNull.Value) return 0;
            if (val is int n) return n;
            if (val is double d) return (int)d;
            if (int.TryParse(val.ToString(), out var p)) return p;
            return 0;
        }

        int? NulNum(DataRow row, string col)
        {
            var n = Num(row, col);
            return n == 0 ? null : n;
        }
    }

    public async Task SalvarEdicaoAsync(RelatorioRenovacao reg)
    {
        // Entidade pode estar sem rastreamento (AsNoTracking); attach + marca só as colunas editáveis
        var entry = context.Entry(reg);
        if (entry.State == EntityState.Detached)
            context.Attach(reg);

        entry.Property(x => x.NovoProdutor).IsModified = true;
        entry.Property(x => x.Observacao).IsModified = true;
        await context.SaveChangesAsync();

        // Desanexa para evitar acúmulo no change tracker (contexto é efetivamente singleton)
        entry.State = EntityState.Detached;
    }

    public async Task SalvarSituacaoAsync(RelatorioRenovacao reg)
    {
        var entry = context.Entry(reg);
        if (entry.State == EntityState.Detached)
            context.Attach(reg);

        entry.Property(x => x.SituacaoAcompanhamento).IsModified = true;
        await context.SaveChangesAsync();
        entry.State = EntityState.Detached;
    }

    public async Task SalvarStatusAdministrativoAsync(RelatorioRenovacao reg)
    {
        var entry = context.Entry(reg);
        if (entry.State == EntityState.Detached)
            context.Attach(reg);
        entry.Property(x => x.AssinaturaFeita).IsModified = true;
        entry.Property(x => x.SeguroEmitido).IsModified   = true;
        await context.SaveChangesAsync();
        entry.State = EntityState.Detached;
    }

    public async Task SalvarFechamentoAsync(RelatorioRenovacao reg)
    {
        var entry = context.Entry(reg);
        if (entry.State == EntityState.Detached)
            context.Attach(reg);

        entry.Property(x => x.SituacaoAcompanhamento).IsModified   = true;
        entry.Property(x => x.FechamentoSeguradora).IsModified      = true;
        entry.Property(x => x.FechamentoPremioLiquido).IsModified   = true;
        entry.Property(x => x.FechamentoFormaPagamento).IsModified  = true;
        entry.Property(x => x.FechamentoComissao).IsModified        = true;
        entry.Property(x => x.FechamentoParcelamento).IsModified    = true;
        entry.Property(x => x.FechamentoAssinatura).IsModified      = true;
        await context.SaveChangesAsync();
        entry.State = EntityState.Detached;
    }

    public async Task<Dictionary<string, int>> GetContadorSituacoesAsync() =>
        await context.RelatorioRenovacoes
            .AsNoTracking()
            .GroupBy(r => r.SituacaoAcompanhamento)
            .ToDictionaryAsync(g => g.Key, g => g.Count());

    public async Task<(int Total, int AssinaturaOk, int EmitidoOk, decimal PremioTotal)> GetRenPalmaStatsAsync()
    {
        var stats = await context.RelatorioRenovacoes
            .AsNoTracking()
            .Where(r => r.SituacaoAcompanhamento == "Ren. Palma")
            .Select(r => new { r.AssinaturaFeita, r.SeguroEmitido, r.FechamentoPremioLiquido })
            .ToListAsync();
        return (
            stats.Count,
            stats.Count(x => x.AssinaturaFeita),
            stats.Count(x => x.SeguroEmitido),
            stats.Sum(x => x.FechamentoPremioLiquido ?? 0));
    }

    public async Task<List<RelatorioRenovacao>> GetRenPalmaAsync() =>
        await context.RelatorioRenovacoes
            .AsNoTracking()
            .Where(r => r.SituacaoAcompanhamento == "Ren. Palma")
            .OrderBy(r => r.NovoProdutor).ThenBy(r => r.NomeCliente)
            .ToListAsync();

    public async Task<List<RelatorioRenovacao>> GetParaProdutorAsync(string login) =>
        await context.RelatorioRenovacoes
            .AsNoTracking()
            .Where(r => r.NovoProdutor == login)
            .OrderBy(r => r.VigenciaFinal)
            .ToListAsync();

    public async Task<List<RelatorioRenovacao>> GetTodosAsync() =>
        await context.RelatorioRenovacoes
            .AsNoTracking()
            .OrderBy(r => r.VigenciaFinal)
            .ToListAsync();

    public async Task<List<RelatorioRenovacao>> FiltrarAsync(
        string? status = null,
        string? seguradora = null,
        string? vendedor = null,
        string? cliente = null,
        DateTime? vigenciaInicio = null,
        DateTime? vigenciaFim = null)
    {
        var q = context.RelatorioRenovacoes.AsQueryable();

        if (!string.IsNullOrWhiteSpace(status))
            q = q.Where(r => r.Status == status);

        if (!string.IsNullOrWhiteSpace(seguradora))
            q = q.Where(r => r.Seguradora == seguradora);

        if (!string.IsNullOrWhiteSpace(vendedor))
            q = q.Where(r => r.VendedorPrincipal == vendedor);

        if (!string.IsNullOrWhiteSpace(cliente))
            q = q.Where(r => r.NomeCliente != null && r.NomeCliente.Contains(cliente));

        if (vigenciaInicio.HasValue)
            q = q.Where(r => r.VigenciaFinal >= vigenciaInicio.Value);

        if (vigenciaFim.HasValue)
            q = q.Where(r => r.VigenciaFinal <= vigenciaFim.Value);

        return await q.OrderBy(r => r.VigenciaFinal).ToListAsync();
    }

    public async Task<List<string>> GetStatusDistinctAsync() =>
        await context.RelatorioRenovacoes
            .Where(r => r.Status != null)
            .Select(r => r.Status!)
            .Distinct().OrderBy(s => s).ToListAsync();

    public async Task<List<string>> GetSeguradorasDistinctAsync() =>
        await context.RelatorioRenovacoes
            .Where(r => r.Seguradora != null)
            .Select(r => r.Seguradora!)
            .Distinct().OrderBy(s => s).ToListAsync();

    public async Task<List<string>> GetVendedoresDistinctAsync() =>
        await context.RelatorioRenovacoes
            .Where(r => r.VendedorPrincipal != null)
            .Select(r => r.VendedorPrincipal!)
            .Distinct().OrderBy(s => s).ToListAsync();

    public async Task<List<string>> GetNovoProdutorDistinctAsync() =>
        await context.RelatorioRenovacoes
            .Where(r => r.NovoProdutor != null && r.NovoProdutor != "")
            .Select(r => r.NovoProdutor!)
            .Distinct().OrderBy(s => s).ToListAsync();

    public async Task SalvarNovoProdutorEmMassaAsync(IList<RelatorioRenovacao> registros)
    {
        foreach (var reg in registros)
        {
            var entry = context.Entry(reg);
            if (entry.State == EntityState.Detached)
                context.Attach(reg);
            entry.Property(x => x.NovoProdutor).IsModified = true;
        }
        await context.SaveChangesAsync();
        foreach (var reg in registros)
            context.Entry(reg).State = EntityState.Detached;
    }
}
