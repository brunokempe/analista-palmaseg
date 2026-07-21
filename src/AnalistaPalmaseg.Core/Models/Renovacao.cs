namespace AnalistaPalmaseg.Core.Models;

public class Renovacao
{
    public int Id { get; set; }
    public int ImportacaoId { get; set; }
    public Importacao Importacao { get; set; } = null!;

    public DateOnly Vigencia { get; set; }
    public string Segurado { get; set; } = string.Empty;
    public string Cia { get; set; } = string.Empty;
    public string Ramo { get; set; } = string.Empty;
    public decimal PlBase { get; set; }
    public decimal Fator { get; set; }
    public decimal Comissao { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? CiaRenovada { get; set; }
    public decimal? NovoPl { get; set; }
    public decimal? NovaComissao { get; set; }
    public decimal? SaldoPl { get; set; }
    public string? EmitidoPor { get; set; }
    public string? Observacao { get; set; }

    public bool IsRenovado => Status is "Ren.Palma" or "Ren.Outro";
    public bool IsPendente => Status is "Procurado" or "Pendente" or "Agendado";
}
