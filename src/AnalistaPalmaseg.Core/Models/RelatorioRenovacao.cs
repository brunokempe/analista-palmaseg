using System.ComponentModel;
using System.ComponentModel.DataAnnotations.Schema;

namespace AnalistaPalmaseg.Core.Models;

public class RelatorioRenovacao : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    private bool _isChecked;
    [NotMapped]
    public bool IsChecked
    {
        get => _isChecked;
        set { if (_isChecked == value) return; _isChecked = value; PropertyChanged?.Invoke(this, new(nameof(IsChecked))); }
    }
    public int Id { get; set; }

    // Identificação
    public string? Proposta { get; set; }
    public string? Apolice { get; set; }
    public string? PedidoEndosso { get; set; }
    public string? Endosso { get; set; }
    public string? Status { get; set; }

    // Datas
    public DateTime? Emissao { get; set; }
    public DateTime? VigenciaInicial { get; set; }
    public DateTime? VigenciaFinal { get; set; }
    public DateTime? Transmissao { get; set; }
    public DateTime? DataControle { get; set; }

    // Financeiro
    public string? TipoRecebimento { get; set; }
    public decimal Comissao { get; set; }
    public decimal ComissaoGerada { get; set; }
    public decimal LiquidoFatura { get; set; }
    public decimal PremioLiquido { get; set; }
    public decimal PremioTotal { get; set; }
    public decimal TotalFatura { get; set; }
    public decimal ComissaoAdicional { get; set; }
    public decimal PremioAdicional { get; set; }
    public decimal PremioCusto { get; set; }
    public decimal Iof { get; set; }
    public int NumeroParcelas { get; set; }
    public decimal ValorParcelas { get; set; }
    public string? FormaPagamento { get; set; }

    // Seguro
    public string? Seguradora { get; set; }
    public string? Ramo { get; set; }
    public string? SeguradoraAnterior { get; set; }
    public string? NegocioCorretora { get; set; }
    public string? CodigoDocumento { get; set; }
    public string? VendedorPrincipal { get; set; }
    public string? Produto { get; set; }
    public int QuantidadeSinistros { get; set; }
    public decimal FranquiaApolice { get; set; }
    public int QuantidadeEndossos { get; set; }
    public string? ObservacaoDocumento { get; set; }

    // Cliente
    public string? NomeCliente { get; set; }
    public DateTime? Nascimento { get; set; }
    public string? Sexo { get; set; }
    public string? EstadoCivil { get; set; }
    public string? DocumentoPrincipal { get; set; }
    public DateTime? ClienteDesde { get; set; }
    public string? Profissao { get; set; }
    public string? Prefixo1 { get; set; }
    public string? Telefone1 { get; set; }
    public string? Prefixo2 { get; set; }
    public string? Telefone2 { get; set; }
    public string? Prefixo3 { get; set; }
    public string? Telefone3 { get; set; }
    public string? Email1 { get; set; }
    public string? Email2 { get; set; }

    // Endereço
    public string? Cep { get; set; }
    public string? Endereco { get; set; }
    public string? NumeroEndereco { get; set; }
    public string? Complemento { get; set; }
    public string? Bairro { get; set; }
    public string? Cidade { get; set; }
    public string? Estado { get; set; }

    // Banco
    public string? Banco { get; set; }
    public string? Agencia { get; set; }
    public string? Conta { get; set; }
    public string? Falecido { get; set; }
    public string? Observacao { get; set; }
    public string? Pasta { get; set; }

    // Veículo
    public string? DescricaoItem { get; set; }
    public string? StatusItem { get; set; }
    public string? CodigoFipe { get; set; }
    public string? Combustivel { get; set; }
    public string? Modelo { get; set; }
    public string? Fabricante { get; set; }
    public string? Categoria { get; set; }
    public string? Chassi { get; set; }
    public string? Placa { get; set; }
    public int? AnoFabricacao { get; set; }
    public int? AnoModelo { get; set; }
    public string? Renavam { get; set; }
    public string? Cor { get; set; }
    public string? Bonus { get; set; }
    public decimal? ValorDeterminado { get; set; }
    public string? CepPernoite { get; set; }
    public string? Financiado { get; set; }
    public string? ZeroKm { get; set; }

    // Coberturas
    public decimal DanosMateriasPremio { get; set; }
    public decimal DanosMaterialLmi { get; set; }
    public decimal DanosMaterialFranquia { get; set; }
    public decimal DanosMoraisPremio { get; set; }
    public decimal DanosMoraisLmi { get; set; }
    public decimal DanosMoraisFranquia { get; set; }
    public decimal DanosCorporaisPremio { get; set; }
    public decimal DanosCorporaisLmi { get; set; }
    public decimal DanosCorporaisFranquia { get; set; }
    public decimal AcidentesPassageiroPremio { get; set; }
    public decimal AcidentesPassageiroLmi { get; set; }
    public decimal AcidentesPassageiroFranquia { get; set; }

    // Campos editáveis manualmente (preservados no re-import)
    public string? NovoProdutor { get; set; }
    public string? MotivoSituacao { get; set; }

    // Campos de fechamento (preenchidos no popup ao definir Ren. Palma)
    public string? FechamentoSeguradora { get; set; }
    public decimal? FechamentoPremioLiquido { get; set; }
    public string? FechamentoFormaPagamento { get; set; }
    public decimal? FechamentoComissao { get; set; }
    public string? FechamentoParcelamento { get; set; }
    public string? FechamentoAssinatura { get; set; }

    // Acompanhamento administrativo pós-fechamento
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

    public string? EmitidoPor { get; set; }
    public int BoletosGerados { get; set; }

    private string _situacaoAcompanhamento = "À Renovar";
    public string SituacaoAcompanhamento
    {
        get => _situacaoAcompanhamento;
        set
        {
            if (_situacaoAcompanhamento == value) return;
            _situacaoAcompanhamento = value;
            PropertyChanged?.Invoke(this, new(nameof(SituacaoAcompanhamento)));
            PropertyChanged?.Invoke(this, new(nameof(RenovacaoRealizada)));
        }
    }

    [NotMapped]
    public bool RenovacaoRealizada =>
        SituacaoAcompanhamento is "Emitido" or "Ren. Palma" or "Ren. Outro";

    // Metadados de importação
    public DateTime ImportadoEm { get; set; }
    public string? ArquivoOrigem { get; set; }

    [NotMapped]
    public decimal? ComissaoValor =>
        FechamentoPremioLiquido.HasValue && FechamentoComissao.HasValue
            ? Math.Round(FechamentoPremioLiquido.Value * FechamentoComissao.Value / 100m, 2)
            : null;

    [NotMapped]
    public decimal PercentualComissaoColab { get; set; }

    // Dados do cadastro de clientes — populados em memória após o carregamento
    [NotMapped] public string? ClienteObservacoes { get; set; }
    [NotMapped] public string? ClienteHistorico { get; set; }
    [NotMapped] public bool TemClienteObservacoes => !string.IsNullOrWhiteSpace(ClienteObservacoes);
    [NotMapped] public bool TemClienteHistorico   => !string.IsNullOrWhiteSpace(ClienteHistorico);
    [NotMapped] public bool NaoTemInfoCliente      => !TemClienteObservacoes && !TemClienteHistorico;

    [NotMapped]
    public decimal ComissaoColab =>
        ComissaoValor.HasValue && PercentualComissaoColab > 0
            ? Math.Round(ComissaoValor.Value * PercentualComissaoColab / 100m, 2)
            : 0m;

    // Calculado — não gravado no banco
    private static readonly System.Globalization.CultureInfo PtBr = new("pt-BR");
    public string DiaDaSemana
    {
        get
        {
            if (!VigenciaFinal.HasValue) return string.Empty;
            var d = VigenciaFinal.Value.ToString("dddd", PtBr);
            return d.Length > 0 ? char.ToUpperInvariant(d[0]) + d[1..] : d;
        }
    }
}
