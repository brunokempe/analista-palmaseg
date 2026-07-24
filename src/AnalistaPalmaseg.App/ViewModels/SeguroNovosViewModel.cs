using System.Collections.ObjectModel;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using AnalistaPalmaseg.Core.Models;
using AnalistaPalmaseg.Core.Services;

namespace AnalistaPalmaseg.App.ViewModels;

public partial class SeguroNovosViewModel : ObservableObject
{
    private readonly SeguroNovoService _service;

    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private SeguroNovo? _registroSelecionado;

    // Campos do formulário
    [ObservableProperty] private int _editandoId;
    [ObservableProperty] private DateTime? _editandoVigencia;
    [ObservableProperty] private string _editandoSegurado = string.Empty;
    [ObservableProperty] private string _editandoCia = string.Empty;
    [ObservableProperty] private string _editandoSegmento = string.Empty;
    [ObservableProperty] private string _editandoStatus = string.Empty;
    [ObservableProperty] private string _editandoFinanceiro = string.Empty;
    [ObservableProperty] private decimal? _editandoPl;
    [ObservableProperty] private decimal? _editandoFator;
    [ObservableProperty] private decimal? _editandoValor;
    [ObservableProperty] private string _editandoObservacao = string.Empty;

    public ObservableCollection<SeguroNovo> Registros { get; } = [];

    public static string[] Segmentos { get; } =
    [
        "Auto", "Resid", "Empresa", "Resp. Civil", "Seguro Viagem",
        "Vida individual", "Vida empresarial", "Transporte (em parceria)",
        "Transporte", "Demais Seguros", "Cartão de crédito", "Financeiro/Outros"
    ];

    public static string[] StatusOpcoes { get; } =
    [
        "Endosso", "Mensal", "Mercado", "Novo", "Prospecção", "Renovação"
    ];

    public static string[] Seguradoras { get; } =
    [
        "Allianz", "AXA", "Bradesco Seguros", "Chubb", "Excelsior",
        "Generali", "HDI Seguros", "Liberty Seguros", "Mapfre Seguros",
        "Pottencial", "Porto Seguro", "Sompo", "SulAmérica", "Tokio Marine",
        "Zurich", "Outras"
    ];

    public SeguroNovosViewModel(SeguroNovoService service)
    {
        _service = service;
    }

    partial void OnRegistroSelecionadoChanged(SeguroNovo? value)
    {
        if (value == null) return;
        PopularFormulario(value);
    }

    private void PopularFormulario(SeguroNovo r)
    {
        EditandoId         = r.Id;
        EditandoVigencia   = r.Vigencia;
        EditandoSegurado   = r.Segurado;
        EditandoCia        = r.Cia;
        EditandoSegmento   = r.Segmento;
        EditandoStatus     = r.Status;
        EditandoFinanceiro = r.Financeiro;
        EditandoPl         = r.Pl;
        EditandoFator      = r.Fator;
        EditandoValor      = r.Valor;
        EditandoObservacao = r.Observacao;
    }

    private void LimparFormulario()
    {
        EditandoId         = 0;
        EditandoVigencia   = null;
        EditandoSegurado   = string.Empty;
        EditandoCia        = string.Empty;
        EditandoSegmento   = string.Empty;
        EditandoStatus     = string.Empty;
        EditandoFinanceiro = string.Empty;
        EditandoPl         = null;
        EditandoFator      = null;
        EditandoValor      = null;
        EditandoObservacao = string.Empty;
        RegistroSelecionado = null;
    }

    public async Task CarregarAsync()
    {
        IsLoading = true;
        try
        {
            var lista = await _service.GetTodosAsync();
            Registros.Clear();
            foreach (var r in lista)
                Registros.Add(r);
        }
        finally { IsLoading = false; }
    }

    [RelayCommand]
    private void Novo() => LimparFormulario();

    [RelayCommand]
    private async Task SalvarAsync()
    {
        if (string.IsNullOrWhiteSpace(EditandoSegurado))
        {
            MessageBox.Show("Informe o nome do segurado.", "Campo obrigatório",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        IsLoading = true;
        try
        {
            var entidade = new SeguroNovo
            {
                Id         = EditandoId,
                Vigencia   = EditandoVigencia,
                Segurado   = EditandoSegurado.Trim(),
                Cia        = EditandoCia.Trim(),
                Segmento   = EditandoSegmento,
                Status     = EditandoStatus,
                Financeiro = EditandoFinanceiro.Trim(),
                Pl         = EditandoPl,
                Fator      = EditandoFator,
                Valor      = EditandoValor,
                Observacao = EditandoObservacao.Trim(),
                CriadoEm   = EditandoId == 0 ? DateTime.Now : DateTime.Now
            };

            var salvo = await _service.SalvarAsync(entidade);

            // Atualiza ou insere na lista local
            var existente = Registros.FirstOrDefault(r => r.Id == salvo.Id);
            if (existente != null)
            {
                var idx = Registros.IndexOf(existente);
                Registros[idx] = salvo;
            }
            else
            {
                Registros.Insert(0, salvo);
            }

            RegistroSelecionado = salvo;
            EditandoId = salvo.Id;
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Erro ao salvar",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally { IsLoading = false; }
    }

    [RelayCommand]
    private async Task ExcluirAsync()
    {
        if (EditandoId == 0) return;

        var confirmacao = MessageBox.Show(
            $"Excluir o registro de \"{EditandoSegurado}\"?",
            "Confirmar exclusão",
            MessageBoxButton.YesNo, MessageBoxImage.Question);

        if (confirmacao != MessageBoxResult.Yes) return;

        IsLoading = true;
        try
        {
            await _service.ExcluirAsync(EditandoId);
            var existente = Registros.FirstOrDefault(r => r.Id == EditandoId);
            if (existente != null)
                Registros.Remove(existente);
            LimparFormulario();
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Erro ao excluir",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally { IsLoading = false; }
    }

    [RelayCommand]
    private void Cancelar() => LimparFormulario();
}
