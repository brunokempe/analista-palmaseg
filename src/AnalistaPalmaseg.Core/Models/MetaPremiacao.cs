namespace AnalistaPalmaseg.Core.Models;

public class MetaPremiacao
{
    public int Id { get; set; }
    public int? QuantidadeMinima { get; set; }
    public bool EhTodas { get; set; }
    public decimal ValorBonus { get; set; }
    public int Ordem { get; set; }
}
