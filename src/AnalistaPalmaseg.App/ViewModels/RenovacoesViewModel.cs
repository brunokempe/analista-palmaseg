using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using AnalistaPalmaseg.Core.Services;

namespace AnalistaPalmaseg.App.ViewModels;

public partial class RenovacoesViewModel : ObservableObject
{
    private readonly RelatorioService _relatorioService;

    [ObservableProperty] private ObservableCollection<string> _nomesDisponiveis = [];
    [ObservableProperty] private string? _nomeSelecionado;
    [ObservableProperty] private ObservableCollection<PeriodoRenovacoesVm> _periodos = [];
    [ObservableProperty] private string _filtroTexto = string.Empty;
    [ObservableProperty] private string _filtroStatus = "Todos";

    public string[] StatusOpcoes { get; } = ["Todos", "Ren.Palma", "Procurado", "Pendente", "Agendado", "Não renovado"];

    public RenovacoesViewModel(RelatorioService relatorioService)
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
            var timeline = await _relatorioService.GetTimelineRenovacoesAsync(produtor);
            Periodos = new ObservableCollection<PeriodoRenovacoesVm>(
                timeline.Select(t => new PeriodoRenovacoesVm(t.Resumo, t.Renovacoes))
            );
            AplicarFiltros();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[Timeline Renovações] ERRO: {ex}");
            Periodos = [];
        }
    }

    private void AplicarFiltros()
    {
        foreach (var periodo in Periodos)
            periodo.AplicarFiltro(FiltroTexto, FiltroStatus);
    }
}
