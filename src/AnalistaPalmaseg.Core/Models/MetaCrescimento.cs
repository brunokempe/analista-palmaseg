namespace AnalistaPalmaseg.Core.Models;

public class MetaCrescimento
{
    public int Id { get; set; }
    public string Tipo { get; set; } = string.Empty;   // "Premio" | "Comissao"
    public decimal PercentualMeta { get; set; }         // ex: 0.10, 0.15, 0.20
    public decimal ValorBonus { get; set; }
    public bool EhEquipe { get; set; }                  // false = individual
}
