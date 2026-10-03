using System.Globalization;
using System.Text;

namespace AnalistaPalmaseg.Core.Services;

/// <summary>Alíquota de IOF por ramo e conversões Prêmio Líquido (PL) ↔ Prêmio Total (PT).</summary>
public static class IofHelper
{
    public const decimal AliquotaReduzida = 0.38m;
    public const decimal AliquotaPadrao = 7.38m;
    // Trechos (sem acento/caixa) que identificam ramos de 0,38%; tolera variações de grafia da planilha.
    private static readonly string[] _trechosReduzidos =
    [
        "acidentes pessoais", "acid. pessoais", "acid pessoais", "capitaliza", "consorcio", "previdencia",
        "saude", "viagem", "vg/apc", "vg apc", "vg-apc", "apc", "vida"
    ];


    private static string Normalizar(string? s)
    {
        if (string.IsNullOrWhiteSpace(s)) return string.Empty;
        var sb = new StringBuilder();
        foreach (var c in s.Trim().Normalize(NormalizationForm.FormD))
            if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                sb.Append(char.ToLowerInvariant(c));
        return sb.ToString();
    }

    /// <summary>Alíquota em % (0,38 ou 7,38). Ramos não listados usam 7,38%.</summary>
    public static decimal ObterAliquota(string? ramo) =>
        ObterAliquotaNormalizada(Normalizar(ramo));

    private static decimal ObterAliquotaNormalizada(string ramo) =>
        _trechosReduzidos.Any(ramo.Contains) ? AliquotaReduzida : AliquotaPadrao;

    public static decimal PlParaPt(decimal pl, decimal aliquota) =>
        Math.Round(pl * (1 + aliquota / 100m), 2);

    public static decimal PtParaPl(decimal pt, decimal aliquota) =>
        Math.Round(pt / (1 + aliquota / 100m), 2);
}
