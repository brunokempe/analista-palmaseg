namespace AnalistaPalmaseg.Core.Models;

public class NovoNegocio
{
    public int Id { get; set; }
    public int ImportacaoId { get; set; }
    public Importacao Importacao { get; set; } = null!;

    public DateOnly Vigencia { get; set; }
    public string Segurado { get; set; } = string.Empty;
    public string Cia { get; set; } = string.Empty;
    public string Segmento { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public decimal Pl { get; set; }
    public decimal Fator { get; set; }
    public decimal Comissao { get; set; }
    public string? Observacao { get; set; }
    public string? EmitidoPor { get; set; }

    public bool IsNovo => Status.Equals("novo", StringComparison.OrdinalIgnoreCase);
    public bool IsRenovacao => Status.Equals("renovação", StringComparison.OrdinalIgnoreCase);
    public bool IsProspeccao => Status.Equals("prospecção", StringComparison.OrdinalIgnoreCase);
}
