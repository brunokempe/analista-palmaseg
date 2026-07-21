using System.Data;
using System.Text.RegularExpressions;
using AnalistaPalmaseg.Core.Data;
using AnalistaPalmaseg.Core.Models;
using ExcelDataReader;

namespace AnalistaPalmaseg.Core.Services;

public class ImportacaoService(AppDbContext context)
{
    // Status values recognized as "renewed"
    private static readonly HashSet<string> StatusRenovado = ["ren.palma", "ren.outro"];
    private static readonly HashSet<string> StatusPendente = ["procurado", "pendente", "agendado"];

    public async Task<Importacao> ImportarAsync(string caminhoArquivo, string? senhaArquivo = null)
    {
        System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);

        var nomeArquivo = Path.GetFileNameWithoutExtension(caminhoArquivo);
        var (produtor, mes, ano) = ExtrairMetadados(nomeArquivo);

        var existente = context.Importacoes
            .FirstOrDefault(x => x.Produtor == produtor && x.Mes == mes && x.Ano == ano);

        if (existente != null)
        {
            context.Renovacoes.RemoveRange(context.Renovacoes.Where(r => r.ImportacaoId == existente.Id));
            context.NovosNegocios.RemoveRange(context.NovosNegocios.Where(n => n.ImportacaoId == existente.Id));
            context.Importacoes.Remove(existente);
            await context.SaveChangesAsync();
        }

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

        using var stream = File.Open(caminhoArquivo, FileMode.Open, FileAccess.Read);

        IExcelDataReader reader;
        var config = new ExcelReaderConfiguration();
        if (!string.IsNullOrEmpty(senhaArquivo))
            config.Password = senhaArquivo;

        reader = ExcelReaderFactory.CreateReader(stream, config);

        var dataSet = reader.AsDataSet(new ExcelDataSetConfiguration
        {
            ConfigureDataTable = _ => new ExcelDataTableConfiguration { UseHeaderRow = false }
        });
        reader.Dispose();

        foreach (DataTable table in dataSet.Tables)
        {
            var sheetName = table.TableName.ToLower();
            if (sheetName == "ren")
                ParseRenovacoes(table, importacao);
            else if (sheetName == "novos")
                ParseNovosNegocios(table, importacao);
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
        return result > 1 ? result / 100m : result;
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
