using AnalistaPalmaseg.Core.Models;

namespace AnalistaPalmaseg.App.ViewModels;

public class PeriodoFuncionarioVm
{
    public required Importacao Importacao { get; init; }
    public required List<FuncionarioResultado> Resultados { get; init; }

    private FuncionarioResultado? Totais =>
        Resultados.FirstOrDefault(f => f.Seguradora.Equals("TOTAIS", StringComparison.OrdinalIgnoreCase));

    public decimal TotalPremio   => Totais?.Premio   ?? Resultados.Sum(f => f.Premio);
    public decimal TotalMeta     => Totais?.Meta     ?? Resultados.Sum(f => f.Meta);
    public decimal TotalComissao => Totais?.Comissao ?? Resultados.Sum(f => f.Comissao);

    public decimal PercentualAtingimento =>
        TotalMeta > 0 ? Math.Round(TotalPremio / TotalMeta * 100m, 1) : 0;

    public IEnumerable<FuncionarioResultado> PorSeguradora =>
        Resultados.Where(f => !f.Seguradora.Equals("TOTAIS", StringComparison.OrdinalIgnoreCase));
}
