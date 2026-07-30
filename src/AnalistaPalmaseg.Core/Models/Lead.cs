namespace AnalistaPalmaseg.Core.Models;

public class Lead
{
    public int Id { get; set; }
    public string Segurado { get; set; } = string.Empty;
    public string Produtor { get; set; } = string.Empty;
    public DateTime CriadoEm { get; set; } = DateTime.Now;
    public string? Indicacao { get; set; }
    public string? Observacao { get; set; }
    public bool SeguroGerado { get; set; }
    public bool Fechou { get; set; }
    public DateTime? FechouEm { get; set; }
    public int? SeguroNovoId { get; set; }
}
