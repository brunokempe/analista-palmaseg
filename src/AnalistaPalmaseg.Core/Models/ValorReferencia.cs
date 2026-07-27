namespace AnalistaPalmaseg.Core.Models;

public class ValorReferencia
{
    public int Id { get; set; }
    public string Colaborador { get; set; } = string.Empty;
    public int Mes { get; set; }
    public int Ano { get; set; }
    public decimal PremioTotal { get; set; }
    public decimal ComissaoTotal { get; set; }
}
