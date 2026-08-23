using System.ComponentModel;
using System.ComponentModel.DataAnnotations.Schema;

namespace AnalistaPalmaseg.Core.Models;

public class SeguroNovo : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

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
    public string FormaPagamento { get; set; } = string.Empty;
    public int? Parcelas { get; set; }

    private bool _assinaturaFeita;
    public bool AssinaturaFeita
    {
        get => _assinaturaFeita;
        set { if (_assinaturaFeita == value) return; _assinaturaFeita = value; PropertyChanged?.Invoke(this, new(nameof(AssinaturaFeita))); }
    }

    private bool _seguroEmitido;
    public bool SeguroEmitido
    {
        get => _seguroEmitido;
        set { if (_seguroEmitido == value) return; _seguroEmitido = value; PropertyChanged?.Invoke(this, new(nameof(SeguroEmitido))); }
    }

    public int BoletosGerados { get; set; }
    public string Observacao { get; set; } = string.Empty;
    public DateTime CriadoEm { get; set; } = DateTime.Now;
    public string? CriadoPor { get; set; }
    public string? EmitidoPor { get; set; }

    [NotMapped]
    public decimal? ComissaoValor =>
        Valor.HasValue && Fator.HasValue
            ? Math.Round(Valor.Value * Fator.Value / 100m, 2)
            : null;

    // Endosso segue regra de renovação (3-6%) — percentual definido externamente pelo MetaService
    [NotMapped]
    public decimal PercentualComissaoColab { get; set; } = -1m;

    [NotMapped]
    public decimal PercentualComissaoColabEfetivo =>
        PercentualComissaoColab >= 0
            ? PercentualComissaoColab
            : Status switch { "Prospecção" => 15m, "Endosso" => 0m, _ => 10m };

    [NotMapped]
    public decimal ComissaoColab =>
        ComissaoValor.HasValue
            ? Math.Round(ComissaoValor.Value * PercentualComissaoColabEfetivo / 100m, 2)
            : 0m;
}
