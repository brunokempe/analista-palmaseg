namespace AnalistaPalmaseg.Core.Models;

public class MetaSeguradora
{
    public int Id { get; set; }
    public int SeguradoraId { get; set; }
    public int Mes { get; set; }
    public int Ano { get; set; }
    public decimal MetaPremio { get; set; }

    public Seguradora? Seguradora { get; set; }
}
