using System.Collections.ObjectModel;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using AnalistaPalmaseg.Core.Models;
using AnalistaPalmaseg.Core.Services;

namespace AnalistaPalmaseg.App.ViewModels;

public record ProdutorEmissaoResumo(
    string Produtor,
    int Total,
    decimal PremioTotal,
    int AssinaturaOk,
    int EmitidoOk)
{
    public int Pendentes => Total - EmitidoOk;
    public string Progresso => $"{EmitidoOk}/{Total}";
}

public partial class EmissaoDashboardViewModel : ObservableObject
{
    private readonly RelatorioRenovacaoService _service;
    private List<RelatorioRenovacao> _todos = [];

    [ObservableProperty] private int _totalRenPalma;
    [ObservableProperty] private decimal _premioTotal;
    [ObservableProperty] private int _assinaturasPendentes;
    [ObservableProperty] private int _emissoesPendentes;
    [ObservableProperty] private string _filtroProdutor = string.Empty;
    [ObservableProperty] private bool _isLoading;

    public ObservableCollection<RelatorioRenovacao> Registros { get; } = [];
    public ObservableCollection<ProdutorEmissaoResumo> ResumoProdutor { get; } = [];
    public ObservableCollection<string> ProdutoresDisponiveis { get; } = [];

    public EmissaoDashboardViewModel(RelatorioRenovacaoService service)
    {
        _service = service;
    }

    public async Task CarregarAsync()
    {
        IsLoading = true;
        try
        {
            _todos = await _service.GetRenPalmaAsync();
            AtualizarCards();
            AtualizarResumoProdutor();
            AtualizarListaFiltro();
            AplicarFiltro();
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void AtualizarCards()
    {
        TotalRenPalma = _todos.Count;
        PremioTotal = _todos.Sum(r => r.FechamentoPremioLiquido ?? 0);
        AssinaturasPendentes = _todos.Count(r => !r.AssinaturaFeita);
        EmissoesPendentes = _todos.Count(r => !r.SeguroEmitido);
    }

    private void AtualizarResumoProdutor()
    {
        ResumoProdutor.Clear();
        foreach (var g in _todos
            .GroupBy(r => string.IsNullOrWhiteSpace(r.NovoProdutor) ? "(Sem produtor)" : r.NovoProdutor)
            .OrderBy(g => g.Key))
        {
            ResumoProdutor.Add(new ProdutorEmissaoResumo(
                g.Key,
                g.Count(),
                g.Sum(r => r.FechamentoPremioLiquido ?? 0),
                g.Count(r => r.AssinaturaFeita),
                g.Count(r => r.SeguroEmitido)));
        }
    }

    private void AtualizarListaFiltro()
    {
        ProdutoresDisponiveis.Clear();
        ProdutoresDisponiveis.Add(string.Empty);
        foreach (var p in _todos
            .Select(r => string.IsNullOrWhiteSpace(r.NovoProdutor) ? "(Sem produtor)" : r.NovoProdutor)
            .Distinct().OrderBy(p => p))
            ProdutoresDisponiveis.Add(p);
    }

    partial void OnFiltroProdutorChanged(string value) => AplicarFiltro();

    private void AplicarFiltro()
    {
        Registros.Clear();
        var fonte = string.IsNullOrWhiteSpace(FiltroProdutor)
            ? _todos
            : _todos.Where(r =>
                (string.IsNullOrWhiteSpace(r.NovoProdutor) ? "(Sem produtor)" : r.NovoProdutor) == FiltroProdutor);

        foreach (var r in fonte)
            Registros.Add(r);
    }

    [RelayCommand]
    private async Task ToggleAssinatura(RelatorioRenovacao? reg)
    {
        if (reg == null) return;
        reg.AssinaturaFeita = !reg.AssinaturaFeita;
        try
        {
            await _service.SalvarStatusAdministrativoAsync(reg);
            AtualizarCards();
            AtualizarResumoProdutor();
        }
        catch (Exception ex)
        {
            reg.AssinaturaFeita = !reg.AssinaturaFeita;
            MessageBox.Show($"Erro ao salvar:\n{ex.Message}", "Erro",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    [RelayCommand]
    private async Task ToggleSeguroEmitido(RelatorioRenovacao? reg)
    {
        if (reg == null) return;
        reg.SeguroEmitido = !reg.SeguroEmitido;
        try
        {
            await _service.SalvarStatusAdministrativoAsync(reg);
            AtualizarCards();
            AtualizarResumoProdutor();
        }
        catch (Exception ex)
        {
            reg.SeguroEmitido = !reg.SeguroEmitido;
            MessageBox.Show($"Erro ao salvar:\n{ex.Message}", "Erro",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    [RelayCommand]
    private async Task Recarregar() => await CarregarAsync();
}
