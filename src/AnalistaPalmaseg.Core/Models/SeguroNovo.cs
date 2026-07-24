namespace AnalistaPalmaseg.Core.Models;

public class SeguroNovo
{
    public int Id { get; set; }
    public DateTime? Vigencia { get; set; }
    public string Segurado { get; set; } = string.Empty;
    public string Cia { get; set; } = string.Empty;
    public string Segmento { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string Financeiro { get; set; } = string.Empty;
    public decimal? Pl { get; set; }
    public decimal? Fator { get; set; }
    public decimal? Valor { get; set; }
    public string Observacao { get; set; } = string.Empty;
    public DateTime CriadoEm { get; set; } = DateTime.Now;
}
