using System.Data;
using System.Text.RegularExpressions;
using AnalistaPalmaseg.Core.Data;
using AnalistaPalmaseg.Core.Models;
using ExcelDataReader;
using Microsoft.EntityFrameworkCore;

namespace AnalistaPalmaseg.Core.Services;

public class ApoliceService(AppDbContext context)
{
    public async Task<ImportacaoApolice> ImportarAsync(string caminhoArquivo)
    {
        System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);

        // Replace all existing apólices on each import
        var antigas = await context.ImportacoesApolice.ToListAsync();
        if (antigas.Count > 0)
        {
            context.ImportacoesApolice.RemoveRange(antigas);
            await context.SaveChangesAsync();
        }

        var importacao = new ImportacaoApolice
        {
            ImportadoEm = DateTime.Now,
            ArquivoOrigem = Path.GetFileName(caminhoArquivo)
        };
        context.ImportacoesApolice.Add(importacao);
        await context.SaveChangesAsync();

        using var stream = File.Open(caminhoArquivo, FileMode.Open, FileAccess.Read);
        var reader = ExcelReaderFactory.CreateReader(stream);
        var dataSet = reader.AsDataSet(new ExcelDataSetConfiguration
        {
            ConfigureDataTable = _ => new ExcelDataTableConfiguration { UseHeaderRow = false }
        });
        reader.Dispose();

        if (dataSet.Tables.Count > 0)
            ParseApolices(dataSet.Tables[0], importacao);

        await context.SaveChangesAsync();
        return importacao;
    }

    public async Task<List<Apolice>> GetTodasAsync()
    {
        return await context.Apolices
            .OrderBy(a => a.DataVencimentoPagamento)
            .ToListAsync();
    }

    public async Task<ImportacaoApolice?> GetUltimaImportacaoAsync()
    {
        return await context.ImportacoesApolice
            .OrderByDescending(i => i.ImportadoEm)
            .FirstOrDefaultAsync();
    }

    private static void ParseApolices(DataTable table, ImportacaoApolice importacao)
    {
        // Auto-detect header row (first row containing keywords)
        var keywords = new[] { "segurado", "apolice", "apólice", "vencimento", "seguradora", "cliente" };
        int headerRow = -1;
        var colMap = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        for (int i = 0; i < Math.Min(10, table.Rows.Count); i++)
        {
            var row = table.Rows[i];
            bool found = false;
            for (int j = 0; j < row.Table.Columns.Count; j++)
            {
                var cell = NormalizeText(row[j]?.ToString() ?? "");
                if (keywords.Any(k => cell.Contains(k)))
                {
                    found = true;
                    break;
                }
            }
            if (found)
            {
                headerRow = i;
                for (int j = 0; j < row.Table.Columns.Count; j++)
                {
                    var header = row[j]?.ToString()?.Trim() ?? "";
                    if (!string.IsNullOrEmpty(header))
                        colMap[header] = j;
                }
                break;
            }
        }

        if (headerRow < 0) return;

        int colNumero    = FindCol(colMap, "apólice", "apolice", "número", "numero", "nº", "n°", "nro");
        int colSegurado  = FindCol(colMap, "segurado", "cliente", "nome", "tomador");
        int colSeguradora = FindCol(colMap, "seguradora", "cia", "empresa", "companhia");
        int colRamo      = FindCol(colMap, "ramo", "produto", "modalidade", "tipo");
        int colVencPgto  = FindCol(colMap, "vencimento pgto", "venc pgto", "vencimento pagamento",
                                           "venc pag", "pgto", "pagamento", "vencimento", "venc.");
        int colInicio    = FindCol(colMap, "início vigência", "inicio vigencia", "ini vigência",
                                           "vigência início", "início", "inicio", "vigencia ini");
        int colFim       = FindCol(colMap, "fim vigência", "fim vigencia", "vigência fim",
                                           "vigencia fim", "fim", "término", "termino");
        int colPremio    = FindCol(colMap, "prêmio", "premio", "valor prêmio", "valor premio",
                                           "valor", "pl");
        int colObs       = FindCol(colMap, "observação", "observacao", "obs", "nota");

        for (int i = headerRow + 1; i < table.Rows.Count; i++)
        {
            var row = table.Rows[i];
            var segurado = GetString(row, colSegurado);
            if (string.IsNullOrWhiteSpace(segurado)) continue;

            var vencPgto = ParseDate(GetString(row, colVencPgto));
            if (vencPgto == null) continue;

            importacao.Apolices.Add(new Apolice
            {
                ImportacaoApoliceId = importacao.Id,
                NumeroApolice = GetString(row, colNumero),
                Segurado = segurado,
                Seguradora = GetString(row, colSeguradora),
                Ramo = GetString(row, colRamo),
                DataVencimentoPagamento = vencPgto.Value,
                DataInicioVigencia = ParseDate(GetString(row, colInicio)),
                DataFimVigencia = ParseDate(GetString(row, colFim)),
                Premio = ParseDecimal(GetString(row, colPremio)),
                Observacao = GetString(row, colObs).NullIfEmpty()
            });
        }
    }

    private static int FindCol(Dictionary<string, int> map, params string[] keywords)
    {
        foreach (var kw in keywords)
            foreach (var key in map.Keys)
                if (NormalizeText(key).Contains(NormalizeText(kw)))
                    return map[key];
        return -1;
    }

    private static string NormalizeText(string s) =>
        s.ToLower()
         .Replace("á", "a").Replace("â", "a").Replace("à", "a").Replace("ã", "a")
         .Replace("é", "e").Replace("ê", "e")
         .Replace("í", "i")
         .Replace("ó", "o").Replace("ô", "o").Replace("õ", "o")
         .Replace("ú", "u").Replace("ü", "u")
         .Replace("ç", "c")
         .Trim();

    private static string GetString(DataRow row, int col)
    {
        if (col < 0 || col >= row.Table.Columns.Count) return string.Empty;
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

    private static DateOnly? ParseDate(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;

        // Try numeric OA date (Excel serial date)
        if (double.TryParse(value, System.Globalization.NumberStyles.Any,
            System.Globalization.CultureInfo.InvariantCulture, out var oaDate)
            && oaDate > 1 && oaDate < 100000)
        {
            try { return DateOnly.FromDateTime(DateTime.FromOADate(oaDate)); }
            catch { /* not a valid OA date */ }
        }

        // dd/MM/yy or dd/MM/yyyy
        var m = Regex.Match(value, @"(\d{1,2})[/\-.](\d{1,2})[/\-.](\d{2,4})");
        if (!m.Success) return null;
        var dia  = int.Parse(m.Groups[1].Value);
        var mes  = int.Parse(m.Groups[2].Value);
        var ano  = int.Parse(m.Groups[3].Value);
        if (ano < 100) ano += 2000;
        try { return new DateOnly(ano, mes, dia); }
        catch { return null; }
    }
}

internal static class StringExtensions
{
    public static string? NullIfEmpty(this string s) =>
        string.IsNullOrWhiteSpace(s) ? null : s;
}
