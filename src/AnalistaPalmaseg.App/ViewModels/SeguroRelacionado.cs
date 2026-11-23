using AnalistaPalmaseg.Core.Models;

namespace AnalistaPalmaseg.App.ViewModels;

// Linha da aba "Seguros Relacionados" do cadastro de clientes: une as renovações
// (RelatorioRenovacao) e os seguros novos (SeguroNovo) do cliente numa só lista.
public class SeguroRelacionado
{
    public string Tipo { get; private init; } = string.Empty;
    public string? Proposta { get; private init; }
    public string? Apolice { get; private init; }
    public string? Seguradora { get; private init; }
    public string? Ramo { get; private init; }
    public string? Produto { get; private init; }
    public DateTime? VigenciaInicial { get; private init; }
    public DateTime? VigenciaFinal { get; private init; }
    public decimal? PremioLiquido { get; private init; }
    public string? FormaPagamento { get; private init; }
    public int? NumeroParcelas { get; private init; }
    public string? Status { get; private init; }
    public string? Vendedor { get; private init; }
    public string? Placa { get; private init; }
    public string? Modelo { get; private init; }
    public string? Situacao { get; private init; }

    public RelatorioRenovacao? Renovacao { get; private init; }
    public SeguroNovo? SeguroNovo { get; private init; }

    public bool IsRenovacao => Renovacao != null;

    // Já fechado na Palma: aparece no Dashboard de Emissões (contratos Ren. Palma / seguros novos).
    public bool RenovadoNaPalma =>
        Renovacao?.SituacaoAcompanhamento == "Ren. Palma" ||
        SeguroNovo is { StatusSeguro: "Fechado na Palma" or "Emitido" } ||
        SeguroNovo is { SeguroEmitido: true };

    // Mês de referência do registro no Dashboard de Emissões.
    public DateTime? DataReferencia => Renovacao?.VigenciaFinal ?? SeguroNovo?.Vigencia ?? SeguroNovo?.CriadoEm;

    public DateTime? DataOrdenacao => VigenciaFinal ?? VigenciaInicial;

    public static SeguroRelacionado De(RelatorioRenovacao r) => new()
    {
        Tipo            = "Renovação",
        Proposta        = r.Proposta,
        Apolice         = r.Apolice,
        Seguradora      = r.Seguradora,
        Ramo            = r.Ramo,
        Produto         = r.Produto,
        VigenciaInicial = r.VigenciaInicial,
        VigenciaFinal   = r.VigenciaFinal,
        PremioLiquido   = r.PremioLiquido,
        FormaPagamento  = r.FormaPagamento,
        NumeroParcelas  = r.NumeroParcelas,
        Status          = r.Status,
        Vendedor        = r.VendedorPrincipal,
        Placa           = r.Placa,
        Modelo          = r.Modelo,
        Situacao        = r.SituacaoAcompanhamento,
        Renovacao       = r
    };

    public static SeguroRelacionado De(SeguroNovo s) => new()
    {
        Tipo            = "Seguro novo",
        Seguradora      = s.Cia,
        Ramo            = s.Segmento,
        VigenciaInicial = s.Vigencia,
        PremioLiquido   = s.Valor,
        FormaPagamento  = s.FormaPagamento,
        NumeroParcelas  = s.Parcelas,
        Status          = s.Status,
        Vendedor        = s.CriadoPor,
        Situacao        = s.StatusSeguro,
        SeguroNovo      = s
    };
}
