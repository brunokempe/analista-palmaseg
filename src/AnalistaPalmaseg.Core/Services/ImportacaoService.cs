using System.Data;
using System.Text.RegularExpressions;
using AnalistaPalmaseg.Core.Data;
using AnalistaPalmaseg.Core.Models;
using ExcelDataReader;

namespace AnalistaPalmaseg.Core.Services;

public class ImportacaoService(AppDbContext context)
{
    private readonly LibreOfficeDecryptorService _decryptor = new();

    public async Task<Importacao> ImportarAsync(string caminhoArquivo, string? senhaArquivo = null)
    {
        System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);

        // Passo 1: descriptografar se necessário
        string arquivoParaLer = caminhoArquivo;
        string? tempDir = null;
        if (!string.IsNullOrEmpty(senhaArquivo) && LibreOfficeDecryptorService.IsEncryptedOds(caminhoArquivo))
        {
            arquivoParaLer = await _decryptor.DecryptToXlsxAsync(caminhoArquivo, senhaArquivo);
            tempDir = Path.GetDirectoryName(arquivoParaLer);
        }

        // Passo 2: ler todas as abas em memória
        DataSet? dataSet = null;
        try
        {
            using var stream = File.Open(arquivoParaLer, FileMode.Open, FileAccess.Read);
            var reader = ExcelReaderFactory.CreateReader(stream, new ExcelReaderConfiguration());
            dataSet = reader.AsDataSet(new ExcelDataSetConfiguration
            {
                ConfigureDataTable = _ => new ExcelDataTableConfiguration { UseHeaderRow = false }
            });
            reader.Dispose();
        }
        finally
        {
            if (tempDir != null)
                LibreOfficeDecryptorService.DeleteTempDirectory(arquivoParaLer);
        }

        System.Diagnostics.Debug.WriteLine(
            $"[Import] Abas encontradas: {string.Join(", ", dataSet!.Tables.Cast<DataTable>().Select(t => $"'{t.TableName}'"))}");

        // Passo 3: identificar nome/período — prefere dados da aba Participação; usa nome do arquivo como fallback
        var (produtor, mes, ano) = ExtrairMetadados(Path.GetFileNameWithoutExtension(caminhoArquivo));
        foreach (DataTable table in dataSet.Tables)
        {
            var sn = table.TableName.ToLower().Trim();
            if (sn is "resultado" or "resultados" or "result" or "results" || sn.Contains("particip"))
            {
                var (nomeSheet, mesSheet, anoSheet) = ExtrairMetadadosParticipacao(table);
                if (!string.IsNullOrWhiteSpace(nomeSheet)) produtor = nomeSheet;
                if (mesSheet > 0) mes = mesSheet;
                if (anoSheet > 0) ano = anoSheet;
                break;
            }
        }
        System.Diagnostics.Debug.WriteLine($"[Import] Identificação: produtor='{produtor}', mes={mes}, ano={ano}");

        // Passo 4: dedup — compara nome normalizado (sem acentos, maiúsculas) para não
        // criar duplicatas quando o nome vem do arquivo vs. da planilha com grafia diferente.
        var produtorNorm = NormalizarNome(produtor);
        var existentes = context.Importacoes
            .Where(x => x.Mes == mes && x.Ano == ano)
            .AsEnumerable()
            .Where(x => NormalizarNome(x.Produtor) == produtorNorm)
            .ToList();
        if (existentes.Count > 0)
        {
            var ids = existentes.Select(e => e.Id).ToList();
            context.Renovacoes.RemoveRange(context.Renovacoes.Where(r => ids.Contains(r.ImportacaoId)));
            context.NovosNegocios.RemoveRange(context.NovosNegocios.Where(n => ids.Contains(n.ImportacaoId)));
            context.Resultados.RemoveRange(context.Resultados.Where(r => ids.Contains(r.ImportacaoId)));
            context.FuncionariosResultados.RemoveRange(context.FuncionariosResultados.Where(f => ids.Contains(f.ImportacaoId)));
            context.Importacoes.RemoveRange(existentes);
            await context.SaveChangesAsync();
        }

        // Passo 5: criar registro de importação
        var importacao = new Importacao
        {
            Produtor = produtor,
            Mes = mes,
            Ano = ano,
            ImportadoEm = DateTime.Now,
            ArquivoOrigem = Path.GetFileName(caminhoArquivo)
        };
        context.Importacoes.Add(importacao);
        await context.SaveChangesAsync();

        // Passo 6: processar abas
        foreach (DataTable table in dataSet.Tables)
        {
            var sheetName = table.TableName.ToLower().Trim();
            System.Diagnostics.Debug.WriteLine($"[Import] Processando aba: '{table.TableName}' ({table.Rows.Count} linhas, {table.Columns.Count} colunas)");

            if (sheetName == "ren")
                ParseRenovacoes(table, importacao);
            else if (sheetName == "novos")
                ParseNovosNegocios(table, importacao);
            else if (sheetName is "resultado" or "resultados" or "result" or "results" || sheetName.Contains("particip"))
            {
                System.Diagnostics.Debug.WriteLine($"[Import] → Aba Participação detectada.");
                ParseResultados(table, importacao);
                System.Diagnostics.Debug.WriteLine($"[Import] → Resultados: {importacao.Resultados.Count} registros.");
                ParseFuncionarios(table, importacao);
                System.Diagnostics.Debug.WriteLine($"[Import] → Funcionários: {importacao.FuncionariosResultados.Count} registros.");
            }
            else
                System.Diagnostics.Debug.WriteLine($"[Import] → Aba ignorada.");
        }

        await context.SaveChangesAsync();
        return importacao;
    }

    private void ParseRenovacoes(DataTable table, Importacao importacao)
    {
        // Data starts at row index 5 (row 6 in Excel, 0-based = 5)
        // Headers are at row 4 (index 4): Vigência|Segurado|Cia|Ramo|PL|Fator|Com|Com|Status|Renovado|Novo PL|...
        for (int i = 5; i < table.Rows.Count; i++)
        {
            var row = table.Rows[i];
            var segurado = GetString(row, 1);
            if (string.IsNullOrWhiteSpace(segurado)) continue;

            var vigencia = ParseDate(GetString(row, 0));
            if (vigencia == null) continue;

            var renovacao = new Renovacao
            {
                ImportacaoId = importacao.Id,
                Vigencia = vigencia.Value,
                Segurado = segurado,
                Cia = GetString(row, 2),
                Ramo = GetString(row, 3),
                PlBase = ParseDecimal(GetString(row, 4)),
                Fator = ParsePercent(GetString(row, 5)),
                Comissao = ParseDecimal(GetString(row, 6)),
                Status = GetString(row, 8),
                CiaRenovada = GetString(row, 9),
                NovoPl = ParseDecimalNullable(GetString(row, 10)),
                NovaComissao = ParseDecimalNullable(GetString(row, 12)),
                SaldoPl = ParseDecimalNullable(GetString(row, 13)),
                EmitidoPor = GetString(row, 15),
                Observacao = GetString(row, 16)
            };

            importacao.Renovacoes.Add(renovacao);
        }
    }

    private void ParseNovosNegocios(DataTable table, Importacao importacao)
    {
        // Data starts at row index 5 (row 6 in Excel)
        // Headers at row 4: Vigência|Segurado|Cia|Segmento|Status|Financeiro|PL|Fator|Valor|Observações...
        for (int i = 5; i < table.Rows.Count; i++)
        {
            var row = table.Rows[i];
            var segurado = GetString(row, 1);
            if (string.IsNullOrWhiteSpace(segurado)) continue;

            var vigencia = ParseDate(GetString(row, 0));
            if (vigencia == null) continue;

            var status = GetString(row, 4);
            if (string.IsNullOrWhiteSpace(status)) continue;

            var negocio = new NovoNegocio
            {
                ImportacaoId = importacao.Id,
                Vigencia = vigencia.Value,
                Segurado = segurado,
                Cia = GetString(row, 2),
                Segmento = GetString(row, 3),
                Status = status,
                Pl = ParseDecimal(GetString(row, 6)),
                Fator = ParsePercent(GetString(row, 7)),
                Comissao = ParseDecimal(GetString(row, 8)),
                Observacao = GetString(row, 9),
                EmitidoPor = GetString(row, 13)
            };

            importacao.NovosNegocios.Add(negocio);
        }
    }

    private void ParseResultados(DataTable table, Importacao importacao)
    {
        // Aba "Participação": seguro por coluna (col 3..N), linhas chave detectadas dinamicamente.
        // Procura a linha "PL Vendido" (Realizado) e "PL META" (Meta) na coluna 2.
        int rowVendido = -1, rowMeta = -1;
        for (int r = 0; r < table.Rows.Count; r++)
        {
            var raw = GetString(table.Rows[r], 2);
            var label = NormalizeText(raw);
            if (!string.IsNullOrWhiteSpace(raw))
                System.Diagnostics.Debug.WriteLine($"[ParseResultados] Linha {r} col2: '{raw}' → normalizado: '{label}'");
            if (rowVendido < 0 && label.Contains("vendido") && !label.Contains("meta"))
                rowVendido = r;
            if (rowMeta < 0 && label.Contains("meta") && (label.Contains("pl") || label.Contains("vendido")))
                rowMeta = r;
        }

        System.Diagnostics.Debug.WriteLine($"[ParseResultados] rowVendido={rowVendido}, rowMeta={rowMeta}");
        if (rowVendido < 0 || rowMeta < 0) return;

        // Cabeçalhos das seguradoras: linha 4 (+ linha 5 para sublinhas como "Itaú" / "Yelum")
        // Colunas de dados começam na coluna 3 e vão até a última não vazia
        int maxCol = table.Columns.Count - 1;

        for (int col = 3; col <= maxCol; col++)
        {
            // Monta o nome da seguradora combinando as linhas de cabeçalho
            var partes = new List<string>();
            for (int headerRow = 4; headerRow <= 6 && headerRow < table.Rows.Count; headerRow++)
            {
                var parte = GetString(table.Rows[headerRow], col).Trim();
                if (!string.IsNullOrWhiteSpace(parte) && parte != "---")
                    partes.Add(parte);
            }
            if (partes.Count == 0) continue;
            var seguradora = string.Join("/", partes);

            if (seguradora.Equals("TOTAIS", StringComparison.OrdinalIgnoreCase)) continue;

            var realizado = ParseDecimal(GetString(table.Rows[rowVendido], col));
            var meta      = ParseDecimal(GetString(table.Rows[rowMeta],    col));

            if (meta == 0 && realizado == 0) continue;

            importacao.Resultados.Add(new ResultadoMeta
            {
                ImportacaoId = importacao.Id,
                Funcionario  = seguradora,
                Meta         = meta,
                Realizado    = realizado
            });
        }
    }

    private void ParseFuncionarios(DataTable table, Importacao importacao)
    {
        // Aba Participação: mesma estrutura de colunas do ParseResultados.
        // Col 2 = rótulo (PL Vendido, Participação, etc.)
        // Col 3+ = dados por seguradora (Porto/Itaú, Unimed, …, TOTAIS)
        // Linhas 4-6 (0-indexed): cabeçalhos das seguradoras
        const int colLabel = 2, colSegStart = 3;

        if (table.Columns.Count <= colSegStart) return;
        int maxCol = table.Columns.Count - 1;

        // Monta mapa col → nome da seguradora (idêntico ao ParseResultados)
        var seguradoras = new Dictionary<int, string>();
        for (int col = colSegStart; col <= maxCol; col++)
        {
            var partes = new List<string>();
            for (int hr = 4; hr <= 6 && hr < table.Rows.Count; hr++)
            {
                var parte = GetString(table.Rows[hr], col).Trim();
                if (!string.IsNullOrWhiteSpace(parte) && parte != "---" && parte != "0")
                    partes.Add(parte);
            }
            if (partes.Count > 0)
                seguradoras[col] = string.Join("/", partes);
        }

        System.Diagnostics.Debug.WriteLine($"[ParseFuncionarios] {seguradoras.Count} seguradoras: {string.Join(", ", seguradoras.Values)}");
        if (seguradoras.Count == 0) return;

        // Localiza linhas-chave pelo rótulo na coluna 2
        int rowPremio = -1, rowMeta = -1, rowComissao = -1, rowMedia = -1;
        for (int r = 0; r < table.Rows.Count; r++)
        {
            var l = NormalizeText(GetString(table.Rows[r], colLabel));
            if (string.IsNullOrWhiteSpace(l)) continue;
            System.Diagnostics.Debug.WriteLine($"[ParseFuncionarios] Linha {r} col{colLabel}: '{GetString(table.Rows[r], colLabel)}' → norm: '{l}'");

            // Captura a primeira linha "Média" antes do bloco de dados — contém a taxa de comissão
            // como fração decimal (ex: 0,1864 = 18,64%) armazenada na planilha
            if (rowPremio < 0 && rowMedia < 0 && l.Contains("media"))
                rowMedia = r;

            if (rowPremio < 0 && l.Contains("vendido") && !l.Contains("meta"))
                rowPremio = r;
            else if (rowPremio >= 0 && rowMeta < 0 && l.Contains("meta") && l.Contains("pl"))
                rowMeta = r;
            else if (rowPremio >= 0 && rowComissao < 0 && l.Contains("participac"))
                rowComissao = r;
        }

        System.Diagnostics.Debug.WriteLine($"[ParseFuncionarios] rowPremio={rowPremio} rowMeta={rowMeta} rowComissao={rowComissao} rowMedia={rowMedia}");
        if (rowPremio < 0 || rowComissao < 0) return;

        var nome = importacao.Produtor;

        foreach (var (col, seguradora) in seguradoras)
        {
            var premio   = ParseDecimal(GetString(table.Rows[rowPremio],   col));
            var meta     = rowMeta >= 0 ? ParseDecimal(GetString(table.Rows[rowMeta], col)) : 0;
            var comissao = ParseDecimal(GetString(table.Rows[rowComissao], col));

            // % comissão: lê da linha "Média" (rowMedia) que armazena a taxa como fração decimal
            // (ex: 0,1864 = 18,64%). Fallback: calcula a partir de comissão/prêmio.
            decimal percentualComissao;
            if (rowMedia >= 0)
                percentualComissao = Math.Round(ParseDecimal(GetString(table.Rows[rowMedia], col)) * 100m, 2);
            else
                percentualComissao = premio > 0 ? Math.Round(comissao / premio * 100m, 2) : 0;

            System.Diagnostics.Debug.WriteLine($"[ParseFuncionarios] {seguradora} col{col}: premio={premio} meta={meta} comissao={comissao} %com={percentualComissao}");

            if (premio == 0 && comissao == 0 && meta == 0) continue;

            importacao.FuncionariosResultados.Add(new FuncionarioResultado
            {
                ImportacaoId       = importacao.Id,
                Nome               = nome,
                Seguradora         = seguradora,
                Premio             = premio,
                Meta               = meta,
                Comissao           = comissao,
                PercentualComissao = percentualComissao
            });
        }

        System.Diagnostics.Debug.WriteLine($"[ParseFuncionarios] Total inseridos: {importacao.FuncionariosResultados.Count}");
    }

    private static string NormalizarNome(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) return string.Empty;
        var normalized = value.Normalize(System.Text.NormalizationForm.FormD);
        var sb = new System.Text.StringBuilder();
        foreach (var c in normalized)
            if (System.Globalization.CharUnicodeInfo.GetUnicodeCategory(c) != System.Globalization.UnicodeCategory.NonSpacingMark)
                sb.Append(c);
        return sb.ToString().ToUpperInvariant().Trim();
    }

    private static string NormalizeText(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) return string.Empty;
        // Remove diacríticos e converte para minúsculas para comparação robusta
        var normalized = value.Normalize(System.Text.NormalizationForm.FormD);
        var sb = new System.Text.StringBuilder();
        foreach (var c in normalized)
            if (System.Globalization.CharUnicodeInfo.GetUnicodeCategory(c) != System.Globalization.UnicodeCategory.NonSpacingMark)
                sb.Append(c);
        return sb.ToString().ToLowerInvariant();
    }

    private static (string produtor, int mes, int ano) ExtrairMetadados(string nomeArquivo)
    {
        // Expected format: "2026-05 NomeProdutor" or "2026-05 Nome Produtor"
        var match = Regex.Match(nomeArquivo, @"(\d{4})-(\d{2})\s+(.+)");
        if (match.Success)
        {
            var ano = int.Parse(match.Groups[1].Value);
            var mes = int.Parse(match.Groups[2].Value);
            var produtor = match.Groups[3].Value.Trim();
            return (produtor, mes, ano);
        }
        return (nomeArquivo, DateTime.Now.Month, DateTime.Now.Year);
    }

    // Lê nome do funcionário (linha 0, col 1) e data (linha 0, col 3) da aba Participação.
    // Formato de data esperado: "01/05/2026 00:00:00 -03:00" → mes=5, ano=2026.
    private static (string nome, int mes, int ano) ExtrairMetadadosParticipacao(DataTable table)
    {
        if (table.Rows.Count == 0) return (string.Empty, 0, 0);

        var nome = GetString(table.Rows[0], 1).Trim();
        var dataStr = GetString(table.Rows[0], 3);
        var match = Regex.Match(dataStr, @"(\d{2})/(\d{2})/(\d{4})");

        if (!match.Success || string.IsNullOrWhiteSpace(nome))
            return (nome, 0, 0);

        var mes = int.Parse(match.Groups[2].Value);
        var ano = int.Parse(match.Groups[3].Value);
        return (nome, mes, ano);
    }

    private static string GetString(DataRow row, int col)
    {
        if (col >= row.Table.Columns.Count) return string.Empty;
        var val = row[col];
        return val == null || val == DBNull.Value ? string.Empty : val.ToString()?.Trim() ?? string.Empty;
    }

    private static decimal ParseDecimal(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) return 0;
        var clean = Regex.Replace(value, @"[R$\s]", "").Replace(".", "").Replace(",", ".");
        return decimal.TryParse(clean, System.Globalization.NumberStyles.Any,
            System.Globalization.CultureInfo.InvariantCulture, out var result) ? result : 0;
    }

    private static decimal? ParseDecimalNullable(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        var result = ParseDecimal(value);
        return result == 0 ? null : result;
    }

    private static decimal ParsePercent(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) return 0;
        var clean = value.Replace("%", "").Replace(",", ".").Trim();
        if (!decimal.TryParse(clean, System.Globalization.NumberStyles.Any,
            System.Globalization.CultureInfo.InvariantCulture, out var result)) return 0;
        // If the spreadsheet stores the raw fraction (e.g. 0.03 for 3%), convert to percentage value.
        // Values > 1 are already percentages (e.g. "3" for 3%), keep as-is.
        if (result > 0 && result <= 1) return result * 100m;
        return result;
    }

    private static DateOnly? ParseDate(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        // Formats: "02/05/26 sáb", "02/05/2026", "27/05/26"
        var match = Regex.Match(value, @"(\d{2})/(\d{2})/(\d{2,4})");
        if (!match.Success) return null;
        var dia = int.Parse(match.Groups[1].Value);
        var mes = int.Parse(match.Groups[2].Value);
        var anoStr = match.Groups[3].Value;
        var ano = anoStr.Length == 2 ? 2000 + int.Parse(anoStr) : int.Parse(anoStr);
        return new DateOnly(ano, mes, dia);
    }
}
