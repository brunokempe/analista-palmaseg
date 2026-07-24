namespace AnalistaPalmaseg.Core.Models;

public class FuncionarioResultado
{
    public int Id { get; set; }
    public int ImportacaoId { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string Seguradora { get; set; } = string.Empty;
    public decimal Premio { get; set; }
    public decimal Meta { get; set; }
    public decimal Comissao { get; set; }
    public decimal PercentualComissao { get; set; }

    public decimal PercentualAtingimento => Meta > 0 ? Math.Round(Premio / Meta * 100m, 1) : 0;
}
