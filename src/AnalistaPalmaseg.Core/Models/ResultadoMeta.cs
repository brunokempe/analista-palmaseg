namespace AnalistaPalmaseg.Core.Models;

public class ResultadoMeta
{
    public int Id { get; set; }
    public int ImportacaoId { get; set; }
    public string Funcionario { get; set; } = string.Empty;
    public decimal Meta { get; set; }
    public decimal Realizado { get; set; }

    public bool BateuMeta => Meta > 0 && Realizado >= Meta;
    public decimal PercentualAtingimento => Meta > 0 ? Math.Round(Realizado / Meta * 100m, 1) : 0m;
    public decimal Saldo => Realizado - Meta;
}
