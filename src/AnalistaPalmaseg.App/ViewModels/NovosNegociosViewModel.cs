using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using AnalistaPalmaseg.Core.Services;

namespace AnalistaPalmaseg.App.ViewModels;

public partial class NovosNegociosViewModel : ObservableObject
{
    private readonly RelatorioService _relatorioService;

    [ObservableProperty] private ObservableCollection<string> _nomesDisponiveis = [];
    [ObservableProperty] private string? _nomeSelecionado;
    [ObservableProperty] private ObservableCollection<PeriodoNegociosVm> _periodos = [];
    [ObservableProperty] private string _filtroTexto = string.Empty;
    [ObservableProperty] private string _filtroStatus = "Todos";

    public string[] StatusOpcoes { get; } = ["Todos", "Novo", "Renovação", "Prospecção", "Mercado"];

    public NovosNegociosViewModel(RelatorioService relatorioService)
    {
        _relatorioService = relatorioService;
    }

    public async Task CarregarAsync()
    {
        var produtores = await _relatorioService.GetProdutoresAsync();
        NomesDisponiveis = new ObservableCollection<string>(produtores);

        if (NomeSelecionado != null && NomesDisponiveis.Contains(NomeSelecionado))
            await CarregarPeriodosAsync(NomeSelecionado);
        else
            NomeSelecionado = NomesDisponiveis.FirstOrDefault();
    }

    partial void OnNomeSelecionadoChanged(string? value)
    {
        if (!string.IsNullOrEmpty(value))
            _ = CarregarPeriodosAsync(value);
        else
            Periodos = [];
    }

    partial void OnFiltroTextoChanged(string value) => AplicarFiltros();
    partial void OnFiltroStatusChanged(string value) => AplicarFiltros();

    private async Task CarregarPeriodosAsync(string produtor)
    {
        try
        {
            var timeline = await _relatorioService.GetTimelineNegociosAsync(produtor);
            Periodos = new ObservableCollection<PeriodoNegociosVm>(
                timeline.Select(t => new PeriodoNegociosVm(t.Resumo, t.Negocios))
            );
            AplicarFiltros();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[Timeline Novos Negócios] ERRO: {ex}");
            Periodos = [];
        }
    }

    private void AplicarFiltros()
    {
        foreach (var periodo in Periodos)
            periodo.AplicarFiltro(FiltroTexto, FiltroStatus);
    }
}
